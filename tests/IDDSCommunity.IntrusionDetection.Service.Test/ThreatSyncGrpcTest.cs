using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Grpc.Core;
using IDDSCommunity.IntrusionDetection.Service;
using IDDSCommunity.IntrusionDetection.Service.Protos;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 Threat Hub 之 gRPC 服務端點、串流微批次傳輸與客戶端容錯移轉機制。
/// </summary>
[TestClass]
public sealed class ThreatSyncGrpcTest
{
    [TestMethod]
    public async Task CustomGrpcPort_UsesHttp2BeforeRestFallback()
    {
        static int FreePort()
        {
            using TcpListener socket = new(IPAddress.Loopback, 0);
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }
        int restPort = FreePort();
        int grpcPort = FreePort();
        IddsConfig hubConfig = IddsConfig.GetDefaultConfiguration();
        hubConfig.ThreatHubPort = restPort;
        hubConfig.ThreatHubGrpcPort = grpcPort;
        hubConfig.ThreatHubApiKey = "port-test-key";
        hubConfig.ThreatHubUseReverseProxy = true;
        hubConfig.ThreatHubReverseProxyLoopbackOnly = true;
        hubConfig.EnableThreatHubGrpc = true;
        using ThreatIntelligenceHubServer hub = new(hubConfig, _ => { }, allowLoopbackHttp: true);
        hub.Start();
        IddsConfig edgeConfig = IddsConfig.GetDefaultConfiguration();
        edgeConfig.EnableThreatHubGrpc = true;
        edgeConfig.ThreatHubGrpcPort = grpcPort;
        List<string> messages = [];
        using ThreatSyncClientHandler client = new(edgeConfig, logInformation: messages.Add);
        ThreatHubSyncResponse reply = await client.SynchronizeAsync($"http://localhost:{restPort}",
            "port-test-key", new ThreatHubSyncPayload { NodeId = "edge-port-test", NewThreats = [] });
        Assert.IsTrue(reply.Success);
        Assert.IsFalse(messages.Any(message => message.Contains("falling back", StringComparison.OrdinalIgnoreCase)));
    }
    private sealed class TestServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> WrittenItems { get; } = [];
        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            WrittenItems.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata requestHeaders = [];
        private readonly CancellationToken cancellationToken;

        public TestServerCallContext(Metadata? headers = null, CancellationToken cancellationToken = default)
        {
            if (headers != null) requestHeaders = headers;
            this.cancellationToken = cancellationToken;
        }

        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "127.0.0.1";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => requestHeaders;
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => null!;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null!;
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }

    /// <summary>
    /// 驗證 gRPC 伺服器服務實作正確驗證金鑰、批次入庫威脅並傳回增量批次。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_Sync_HandlesAuthAndMicroBatching()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "grpc-secret-key-123";
        var store = new ThreatHubStore();
        List<ThreatIntelligenceItem> received = [];
        var service = new ThreatSyncServiceImpl(config, store, item => received.Add(item));

        // 1. 驗證金鑰錯誤被拒絕
        var invalidReq = new SyncRequest
        {
            ApiKey = "wrong-key",
            NodeId = "node-1",
            NodeName = "edge-alpha"
        };
        var unauthResp = await service.Sync(invalidReq, new TestServerCallContext());
        Assert.IsFalse(unauthResp.Success);
        Assert.AreEqual("Unauthorized", unauthResp.ErrorMessage);

        // 2. 驗證合法同步與情資微批次入庫
        var validReq = new SyncRequest
        {
            ApiKey = "grpc-secret-key-123",
            NodeId = "node-1",
            NodeName = "edge-alpha",
            Cursor = 0
        };
        validReq.NewThreats.Add(new ThreatRecord
        {
            SourceIp = "93.184.216.34",
            ThreatCategory = "SCANNER",
            ConfidenceScore = 0.95,
            ReportedTicks = DateTime.UtcNow.Ticks,
            ExpiresTicks = DateTime.UtcNow.AddHours(2).Ticks,
            ReporterNodeId = "node-1",
            ReporterNodeName = "edge-alpha"
        });

        var resp = await service.Sync(validReq, new TestServerCallContext());
        Assert.IsTrue(resp.Success);
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("93.184.216.34", received[0].SourceIp);
        Assert.IsNotNull(resp.DeltaBatch);
        Assert.AreEqual(1, resp.DeltaBatch.Events.Count);
        Assert.AreEqual(ThreatJournalEventType.EventTypeBlocked, resp.DeltaBatch.Events[0].EventType);
        Assert.AreEqual("93.184.216.34", resp.DeltaBatch.Events[0].SourceIp);
    }

    /// <summary>
    /// 驗證 PullDeltas 伺服器串流能依序推送異動批次。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_PullDeltas_StreamsBatches()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "pull-key-456";
        var store = new ThreatHubStore();
        store.Upsert(new ThreatIntelligenceItem { SourceIp = "203.0.113.1", ExpiresUtc = DateTime.UtcNow.AddHours(1) });
        store.RecordProbation("203.0.113.2", "Probation event");
        store.Revoke("203.0.113.1", "Tombstone revoked");

        var service = new ThreatSyncServiceImpl(config, store, _ => { });
        var streamWriter = new TestServerStreamWriter<ThreatDeltaBatch>();
        var request = new StreamDeltasRequest
        {
            ApiKey = "pull-key-456",
            Cursor = 0
        };

        await service.PullDeltas(request, streamWriter, new TestServerCallContext());
        Assert.AreEqual(1, streamWriter.WrittenItems.Count);
        var batch = streamWriter.WrittenItems[0];
        Assert.AreEqual(3, batch.Events.Count);
        Assert.AreEqual(ThreatJournalEventType.EventTypeBlocked, batch.Events[0].EventType);
        Assert.AreEqual(ThreatJournalEventType.EventTypeProbation, batch.Events[1].EventType);
        Assert.AreEqual(ThreatJournalEventType.EventTypeRevoked, batch.Events[2].EventType);
    }

    /// <summary>
    /// 驗證 StreamSnapshot 伺服器串流能分塊推送當前生效威脅項目。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_StreamSnapshot_StreamsChunks()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "snap-key-789";
        var store = new ThreatHubStore();
        for (int i = 1; i <= 10; i++)
        {
            store.Upsert(new ThreatIntelligenceItem { SourceIp = $"198.51.100.{i}", ExpiresUtc = DateTime.UtcNow.AddHours(1) });
        }

        var service = new ThreatSyncServiceImpl(config, store, _ => { });
        var streamWriter = new TestServerStreamWriter<ThreatSnapshotChunk>();
        var request = new StreamDeltasRequest
        {
            ApiKey = "snap-key-789",
            Cursor = 0
        };

        await service.StreamSnapshot(request, streamWriter, new TestServerCallContext());
        Assert.AreEqual(1, streamWriter.WrittenItems.Count);
        var chunk = streamWriter.WrittenItems[0];
        Assert.AreEqual(10, chunk.Threats.Count);
        Assert.IsTrue(chunk.IsLastChunk);
    }

    /// <summary>
    /// 驗證當 gRPC 通道不可達時，ThreatSyncClientHandler 自動容錯降級使用 REST API。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncClientHandler_FallsBackToRest_WhenGrpcUnavailable()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.EnableThreatHubGrpc = true;
        config.ThreatHubGrpcPort = 65530; // 不存在的 Port

        // 啟動僅支援 REST 的本機 ThreatIntelligenceHubServer
        config.ThreatHubApiKey = "fallback-test-key";
        config.EnableThreatHubGrpc = false; // Hub 本身不啟動 gRPC 以模擬舊版 Hub
        using var hub = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
        hub.Start();

        var clientConfig = IddsConfig.GetDefaultConfiguration();
        clientConfig.EnableThreatHubGrpc = true;
        clientConfig.ThreatHubGrpcPort = 65530; // Client 會先嘗試連線不存在的 gRPC port
        using var handler = new ThreatSyncClientHandler(clientConfig);

        var payload = new ThreatHubSyncPayload
        {
            NodeId = "edge-test-fallback",
            NodeName = "edge-box",
            Cursor = 0,
            NewThreats = []
        };

        string endpoint = $"http://localhost:{config.ThreatHubPort}";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await handler.SynchronizeAsync(endpoint, "fallback-test-key", payload, cts.Token);

        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.ActiveThreats);
    }

    /// <summary>
    /// 驗證 ThreatIntelligenceHubServer 支援可設定之節點容量擴展與背景逾時節點清理。
    /// </summary>
    [TestMethod]
    public void ThreatIntelligenceHubServer_NodeCapacity_And_StaleCleanup()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubMaxRegisteredNodes = 25000;
        Assert.AreEqual(25000, config.ThreatHubMaxRegisteredNodes);

        using var hub = new ThreatIntelligenceHubServer(config, _ => { });
        // 初始已註冊節點為 0
        Assert.AreEqual(0, hub.RegisteredNodes.Count);

        // 驗證 CleanStaleNodes 不會在空集合或正常集合引發例外狀況
        hub.CleanStaleNodes();
        Assert.AreEqual(0, hub.RegisteredNodes.Count);
    }

    /// <summary>
    /// 驗證 gRPC 同步服務能正確註冊邊緣節點並在達到最大節點容量上限時回傳錯誤。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_NodeCapacity_RegistersAndEnforcesLimit()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "cap-key";
        config.ThreatHubMaxRegisteredNodes = 1;
        using var hub = new ThreatIntelligenceHubServer(config, _ => { });
        var store = new ThreatHubStore();
        var service = new ThreatSyncServiceImpl(config, store, _ => { }, tryRegisterNode: hub.TryRegisterNode);

        var req1 = new SyncRequest
        {
            ApiKey = "cap-key",
            NodeId = "node-1",
            NodeName = "edge-1"
        };
        var resp1 = await service.Sync(req1, new TestServerCallContext());
        Assert.IsTrue(resp1.Success);
        Assert.AreEqual(1, hub.RegisteredNodes.Count);
        Assert.AreEqual("node-1", hub.RegisteredNodes[0].NodeId);

        // 第二個不同節點應觸發容量上限拒絕
        var req2 = new SyncRequest
        {
            ApiKey = "cap-key",
            NodeId = "node-2",
            NodeName = "edge-2"
        };
        var resp2 = await service.Sync(req2, new TestServerCallContext());
        Assert.IsFalse(resp2.Success);
        Assert.AreEqual("Threat Hub node capacity reached", resp2.ErrorMessage);
        Assert.AreEqual(1, hub.RegisteredNodes.Count);
    }

    /// <summary>
    /// 驗證 gRPC 服務整合 FailedAttemptsRateLimiter，在連續金鑰錯誤達到門檻後觸發冷卻阻絕。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_AuthRateLimiting_AppliesLockout()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "real-secret-key";
        var limiter = new IDDSCommunity.IntrusionDetection.Shared.Security.FailedAttemptsRateLimiter(maxFailedAttempts: 3, windowDuration: TimeSpan.FromMinutes(5), lockDuration: TimeSpan.FromMinutes(10));
        var store = new ThreatHubStore();
        var service = new ThreatSyncServiceImpl(config, store, _ => { }, authRateLimiter: limiter);

        var badReq = new SyncRequest
        {
            ApiKey = "bad-key",
            NodeId = "node-x"
        };

        // 連續 3 次錯誤金鑰
        for (int i = 0; i < 3; i++)
        {
            var r = await service.Sync(badReq, new TestServerCallContext());
            Assert.IsFalse(r.Success);
            Assert.AreEqual("Unauthorized", r.ErrorMessage);
        }

        // 第 4 次嘗試已被阻絕（即使給予正確金鑰）
        var goodReq = new SyncRequest
        {
            ApiKey = "real-secret-key",
            NodeId = "node-x"
        };
        var blockedResp = await service.Sync(goodReq, new TestServerCallContext());
        Assert.IsFalse(blockedResp.Success);
        Assert.IsTrue(blockedResp.ErrorMessage?.Contains("Too many failed attempts") == true);
    }

    /// <summary>
    /// 驗證 gRPC 服務拒絕超過 256 筆微批次、信心度超出邊界及未來時間戳記之異常情資。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatSyncServiceImpl_RejectsInvalidPayloadsAndBoundsConfidence()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "valid-key";
        var store = new ThreatHubStore();
        List<ThreatIntelligenceItem> received = [];
        var service = new ThreatSyncServiceImpl(config, store, item => received.Add(item));

        // 1. 超過 256 筆批次應被拒絕
        var largeReq = new SyncRequest
        {
            ApiKey = "valid-key",
            NodeId = "node-1"
        };
        for (int i = 0; i < 257; i++)
        {
            largeReq.NewThreats.Add(new ThreatRecord { SourceIp = $"198.51.100.{i % 250 + 1}", ConfidenceScore = 0.9 });
        }
        var largeResp = await service.Sync(largeReq, new TestServerCallContext());
        Assert.IsFalse(largeResp.Success);
        Assert.IsTrue(largeResp.ErrorMessage?.Contains("exceeds maximum allowed size") == true);

        // 2. 信心度小於 0.8 或大於 1.0、Bogon IP、未來時間戳記應被忽略
        var invalidThreatsReq = new SyncRequest
        {
            ApiKey = "valid-key",
            NodeId = "node-1"
        };
        invalidThreatsReq.NewThreats.Add(new ThreatRecord { SourceIp = "10.0.0.1", ConfidenceScore = 0.95, Reason = "Bogon IP" }); // Bogon IP (RFC 1918)
        invalidThreatsReq.NewThreats.Add(new ThreatRecord { SourceIp = "93.184.216.30", ConfidenceScore = 0.5 }); // 低信心度
        invalidThreatsReq.NewThreats.Add(new ThreatRecord { SourceIp = "93.184.216.31", ConfidenceScore = 1.5 }); // 超過 1.0
        invalidThreatsReq.NewThreats.Add(new ThreatRecord { SourceIp = "93.184.216.32", ConfidenceScore = 0.95, ReportedTicks = DateTime.UtcNow.AddHours(2).Ticks }); // 未來時間戳記
        invalidThreatsReq.NewThreats.Add(new ThreatRecord { SourceIp = "93.184.216.33", ConfidenceScore = 0.95, Reason = "Valid threat" }); // 合法

        var filterResp = await service.Sync(invalidThreatsReq, new TestServerCallContext());
        Assert.IsTrue(filterResp.Success);
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual("93.184.216.33", received[0].SourceIp);
        Assert.AreEqual("Valid threat", received[0].Notes);
    }

    /// <summary>
    /// 驗證邊緣節點在同步時接收到 DeltaEvents 之撤銷墓碑，能自動解除本地鎖定並更新本地儲存庫。
    /// </summary>
    /// <returns>表示非同步測試作業之工作。</returns>
    [TestMethod]
    public async Task ThreatIntelligenceSyncService_ProcessesDeltaEvents_RevokesAndProbates()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubApiKey = "sync-delta-key";
        config.EnableThreatHubGrpc = false;

        // 建立測試用 Hub，並預先建立一筆威脅、一筆假釋、一筆撤銷
        var hubStore = new ThreatHubStore();
        hubStore.Upsert(new ThreatIntelligenceItem { SourceIp = "203.0.113.88", ExpiresUtc = DateTime.UtcNow.AddHours(1) });
        hubStore.RecordProbation("203.0.113.89", "Probation check");
        hubStore.Revoke("203.0.113.88", "False positive revoked");

        var localStore = new ThreatHubStore();
        localStore.Upsert(new ThreatIntelligenceItem { SourceIp = "203.0.113.88", ExpiresUtc = DateTime.UtcNow.AddHours(1) });
        Assert.IsNotNull(localStore.LookupThreat("203.0.113.88"));

        var response = new ThreatHubSyncResponse
        {
            Success = true,
            Generation = hubStore.Generation,
            NextCursor = 3,
            DeltaEvents = hubStore.ReadJournal(0, 10).ToList()
        };

        // 模擬在 ThreatIntelligenceSyncService 中收到含有 DeltaEvents 之 response
        foreach (var delta in response.DeltaEvents)
        {
            if (delta.EventType == ThreatHubJournalEventType.Revoked)
            {
                if (IpAddressCanonicalizer.TryCanonicalize(delta.SourceIp, out string canonicalIp))
                {
                    localStore.Revoke(canonicalIp, delta.Payload);
                }
            }
            else if (delta.EventType == ThreatHubJournalEventType.Probation)
            {
                if (IpAddressCanonicalizer.TryCanonicalize(delta.SourceIp, out string canonicalIp))
                {
                    localStore.RecordProbation(canonicalIp, delta.Payload);
                }
            }
        }

        // 驗證本地儲存庫中已被撤銷移除
        Assert.IsNull(localStore.LookupThreat("203.0.113.88"));
        await Task.CompletedTask;
    }
}
