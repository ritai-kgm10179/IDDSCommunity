using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using IDDSCommunity.IntrusionDetection.Service.Protos;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Security;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供威脅情資中繼中心（Threat Hub）之 gRPC 高效微批次同步服務實作。
/// </summary>
internal sealed class ThreatSyncServiceImpl : ThreatSyncService.ThreatSyncServiceBase
{
    private const string ApiKeyHeader = "X-IDDS-ThreatHub-ApiKey";
    private readonly IddsConfig config;
    private readonly ThreatHubStore store;
    private readonly Action<ThreatIntelligenceItem> onThreatReceived;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logError;
    private readonly Func<string, string, string, int, string, bool>? tryRegisterNode;
    private readonly FailedAttemptsRateLimiter? authRateLimiter;

    /// <summary>
    /// 初始化 <see cref="ThreatSyncServiceImpl"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域組態設定執行個體。</param>
    /// <param name="store">威脅中繼中心儲存庫。</param>
    /// <param name="onThreatReceived">當接收到合法威脅時引發之回呼委派。</param>
    /// <param name="logInformation">資訊日誌回報委派。</param>
    /// <param name="logError">錯誤日誌回報委派。</param>
    /// <param name="tryRegisterNode">選擇性的節點註冊與容量檢查委派。</param>
    /// <param name="authRateLimiter">選擇性的 API 金鑰驗證失敗頻率限制器。</param>
    internal ThreatSyncServiceImpl(
        IddsConfig config,
        ThreatHubStore store,
        Action<ThreatIntelligenceItem> onThreatReceived,
        Action<string>? logInformation = null,
        Action<string, Exception>? logError = null,
        Func<string, string, string, int, string, bool>? tryRegisterNode = null,
        FailedAttemptsRateLimiter? authRateLimiter = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.onThreatReceived = onThreatReceived ?? throw new ArgumentNullException(nameof(onThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logError = logError ?? ((msg, ex) => System.Diagnostics.Trace.TraceError("{0}: {1}", msg, ex.Message));
        this.tryRegisterNode = tryRegisterNode;
        this.authRateLimiter = authRateLimiter;
    }

    private static string ExtractClientIp(ServerCallContext context)
    {
        try
        {
            var httpContext = context.GetHttpContext();
            if (httpContext?.Connection?.RemoteIpAddress != null)
            {
                return IpAddressCanonicalizer.Canonicalize(httpContext.Connection.RemoteIpAddress).ToString();
            }
        }
        catch (InvalidOperationException) { }

        string peer = context.Peer ?? string.Empty;
        if (peer.StartsWith("ipv4:", StringComparison.OrdinalIgnoreCase))
            peer = peer[5..];
        else if (peer.StartsWith("ipv6:", StringComparison.OrdinalIgnoreCase))
            peer = peer[5..];

        int lastColon = peer.LastIndexOf(':');
        if (lastColon > 0 && !peer.Contains(']'))
            peer = peer[..lastColon];
        else if (peer.StartsWith('[') && peer.Contains("]:"))
            peer = peer.Substring(1, peer.IndexOf("]:", StringComparison.Ordinal) - 1);

        if (IpAddressCanonicalizer.TryCanonicalize(peer, out string canonical))
            return canonical;

        return "127.0.0.1";
    }

    private bool Authenticate(string? requestApiKey, ServerCallContext context, string clientIp, out int failureCount)
    {
        failureCount = 0;
        string? headerApiKey = context.RequestHeaders.GetValue(ApiKeyHeader);
        string? providedKey = !string.IsNullOrWhiteSpace(requestApiKey) ? requestApiKey : headerApiKey;
        if (string.IsNullOrWhiteSpace(config.ThreatHubApiKey) || string.IsNullOrWhiteSpace(providedKey))
        {
            failureCount = RecordAuthFailure(clientIp);
            return false;
        }

        byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(config.ThreatHubApiKey));
        byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));
        bool match = CryptographicOperations.FixedTimeEquals(expected, actual);
        if (!match)
        {
            failureCount = RecordAuthFailure(clientIp);
            return false;
        }
        return true;
    }

    private int RecordAuthFailure(string clientIp)
    {
        if (authRateLimiter == null) return 0;
        int failures = authRateLimiter.RecordFailedAttempt(clientIp);
        if (failures >= 10 && IPAddress.TryParse(clientIp, out IPAddress? parsed) && !IPAddress.IsLoopback(parsed))
        {
            try
            {
                IntrusionLog.AddEntry(DateTime.UtcNow, WellKnownAgentIds.ClusterThreatHub, clientIp, IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);
            }
            catch { }
        }
        return failures;
    }

    /// <summary>
    /// 執行雙向威脅情資增量微批次同步。
    /// </summary>
    /// <param name="request">同步請求物件。</param>
    /// <param name="context">伺服器呼叫上下文。</param>
    /// <returns>表示非同步作業之工作，其結果為同步回應物件。</returns>
    public override async Task<SyncResponse> Sync(SyncRequest request, ServerCallContext context)
    {
        string clientIp = ExtractClientIp(context);
        if (authRateLimiter != null && authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
        {
            return new SyncResponse
            {
                Success = false,
                ErrorMessage = $"Too many failed attempts. Try again in {retryAfter.TotalSeconds:F0} seconds."
            };
        }

        if (!Authenticate(request.ApiKey, context, clientIp, out int failures))
        {
            if (failures >= 3)
            {
                int delayMs = Math.Min(500 * (1 << Math.Min(failures - 3, 3)), 3000);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            return new SyncResponse
            {
                Success = false,
                ErrorMessage = "Unauthorized"
            };
        }

        if (tryRegisterNode != null)
        {
            int threatCount = request.NewThreats?.Count ?? 0;
            if (!tryRegisterNode(request.NodeId, request.NodeName, clientIp, threatCount, request.Generation ?? string.Empty))
            {
                return new SyncResponse
                {
                    Success = false,
                    ErrorMessage = "Threat Hub node capacity reached"
                };
            }
        }

        if (request.NewThreats != null && request.NewThreats.Count > 256)
        {
            return new SyncResponse
            {
                Success = false,
                ErrorMessage = "Micro-batch exceeds maximum allowed size of 256 items."
            };
        }

        if (request.NewThreats != null && request.NewThreats.Count > 0)
        {
            List<ThreatIntelligenceItem> validThreats = [];
            foreach (var threat in request.NewThreats)
            {
                if (string.IsNullOrWhiteSpace(threat.SourceIp)) continue;
                if (!IPAddress.TryParse(threat.SourceIp, out IPAddress? parsedIp)) continue;
                if (BogonIpFilter.IsBogonOrReserved(parsedIp)) continue;

                string canonical = IpAddressCanonicalizer.Canonicalize(parsedIp).ToString();
                if (config.UseSafeNetworkList && config.IsInSafeNetwork(canonical)) continue;

                if (!double.IsFinite(threat.ConfidenceScore) || threat.ConfidenceScore < 0.8 || threat.ConfidenceScore > 1.0) continue;

                if (threat.ThreatCategory?.Length > 128 || threat.ReporterNodeId?.Length > 128 || threat.ReporterNodeName?.Length > 128 || threat.Reason?.Length > 1024) continue;

                DateTime now = DateTime.UtcNow;
                DateTime reported = threat.ReportedTicks > 0 ? new DateTime(threat.ReportedTicks, DateTimeKind.Utc) : now;
                if (reported > now.AddMinutes(5) || reported < now.AddDays(-Math.Clamp(config.ThreatFeedTtlDays, 1, 365))) continue;

                DateTime expires = threat.ExpiresTicks > 0 ? new DateTime(threat.ExpiresTicks, DateTimeKind.Utc) : reported.AddDays(Math.Clamp(config.ThreatFeedTtlDays, 1, 365));
                DateTime maxExpiry = reported.AddDays(Math.Clamp(config.ThreatFeedTtlDays, 1, 365));
                if (expires > maxExpiry) expires = maxExpiry;
                if (expires <= now) continue;

                var item = new ThreatIntelligenceItem
                {
                    SourceIp = canonical,
                    ThreatCategory = !string.IsNullOrWhiteSpace(threat.ThreatCategory) ? threat.ThreatCategory : "BRUTE_FORCE",
                    ConfidenceScore = threat.ConfidenceScore,
                    ReportedUtc = reported,
                    ExpiresUtc = expires,
                    ReporterNodeId = threat.ReporterNodeId ?? string.Empty,
                    ReporterNodeName = threat.ReporterNodeName ?? string.Empty,
                    Notes = threat.Reason ?? string.Empty
                };
                validThreats.Add(item);
                onThreatReceived(item);
            }
            if (validThreats.Count > 0)
            {
                store.UpsertBatch(validThreats);
            }
        }

        var deltaEntries = store.ReadJournal(request.Cursor, 256);
        long nextJournalCursor = deltaEntries.Count > 0 ? deltaEntries[^1].Sequence : request.Cursor;
        var deltaBatch = new ThreatDeltaBatch
        {
            Generation = store.Generation,
            FromCursor = request.Cursor,
            ToCursor = nextJournalCursor,
            HasMore = deltaEntries.Count >= 256
        };
        foreach (var entry in deltaEntries)
        {
            deltaBatch.Events.Add(new ThreatDeltaEvent
            {
                Sequence = entry.Sequence,
                EventType = (ThreatJournalEventType)entry.EventType,
                SourceIp = entry.SourceIp,
                Payload = entry.Payload,
                CreatedTicks = entry.CreatedTicks
            });
        }

        var page = store.ReadPage(request.Cursor, request.Generation ?? string.Empty);
        var response = new SyncResponse
        {
            Success = true,
            Generation = store.Generation,
            NextCursor = nextJournalCursor > 0 ? nextJournalCursor : page.NextCursor,
            HasMore = deltaBatch.HasMore || page.HasMore,
            DeltaBatch = deltaBatch
        };

        foreach (var t in page.ActiveThreats)
        {
            response.ActiveThreats.Add(new ThreatRecord
            {
                SourceIp = t.SourceIp,
                ThreatCategory = t.ThreatCategory,
                ConfidenceScore = t.ConfidenceScore,
                ReportedTicks = t.ReportedUtc.Ticks,
                ExpiresTicks = t.ExpiresUtc.Ticks,
                ReporterNodeId = t.ReporterNodeId,
                ReporterNodeName = t.ReporterNodeName,
                Reason = t.Notes
            });
        }

        return response;
    }

    /// <summary>
    /// 以伺服器串流方式批次拉取威脅情資異動日誌。
    /// </summary>
    /// <param name="request">串流日誌請求物件。</param>
    /// <param name="responseStream">異動批次回應串流寫入器。</param>
    /// <param name="context">伺服器呼叫上下文。</param>
    /// <returns>表示非同步串流作業之工作。</returns>
    public override async Task PullDeltas(StreamDeltasRequest request, IServerStreamWriter<ThreatDeltaBatch> responseStream, ServerCallContext context)
    {
        string clientIp = ExtractClientIp(context);
        if (authRateLimiter != null && authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted, $"Too many failed attempts. Try again in {retryAfter.TotalSeconds:F0} seconds."));
        }

        if (!Authenticate(request.ApiKey, context, clientIp, out int failures))
        {
            if (failures >= 3)
            {
                int delayMs = Math.Min(500 * (1 << Math.Min(failures - 3, 3)), 3000);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        }

        long cursor = request.Cursor;
        const int batchSize = 256;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var entries = store.ReadJournal(cursor, batchSize);
            if (entries.Count == 0) break;

            var batch = new ThreatDeltaBatch
            {
                Generation = store.Generation,
                FromCursor = cursor,
                ToCursor = entries[^1].Sequence,
                HasMore = entries.Count >= batchSize
            };
            foreach (var entry in entries)
            {
                batch.Events.Add(new ThreatDeltaEvent
                {
                    Sequence = entry.Sequence,
                    EventType = (ThreatJournalEventType)entry.EventType,
                    SourceIp = entry.SourceIp,
                    Payload = entry.Payload,
                    CreatedTicks = entry.CreatedTicks
                });
            }

            await responseStream.WriteAsync(batch, context.CancellationToken).ConfigureAwait(false);
            cursor = entries[^1].Sequence;
            if (entries.Count < batchSize) break;
        }
    }

    /// <summary>
    /// 以伺服器串流方式分塊拉取全網生效威脅快照清單。
    /// </summary>
    /// <param name="request">串流快照請求物件。</param>
    /// <param name="responseStream">快照分塊回應串流寫入器。</param>
    /// <param name="context">伺服器呼叫上下文。</param>
    /// <returns>表示非同步串流作業之工作。</returns>
    public override async Task StreamSnapshot(StreamDeltasRequest request, IServerStreamWriter<ThreatSnapshotChunk> responseStream, ServerCallContext context)
    {
        string clientIp = ExtractClientIp(context);
        if (authRateLimiter != null && authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted, $"Too many failed attempts. Try again in {retryAfter.TotalSeconds:F0} seconds."));
        }

        if (!Authenticate(request.ApiKey, context, clientIp, out int failures))
        {
            if (failures >= 3)
            {
                int delayMs = Math.Min(500 * (1 << Math.Min(failures - 3, 3)), 3000);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        }

        long cursor = 0;
        int chunkIndex = 0;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var page = store.ReadPage(cursor, store.Generation);
            chunkIndex++;
            var chunk = new ThreatSnapshotChunk
            {
                Generation = store.Generation,
                ChunkIndex = chunkIndex,
                IsLastChunk = !page.HasMore
            };
            foreach (var t in page.ActiveThreats)
            {
                chunk.Threats.Add(new ThreatRecord
                {
                    SourceIp = t.SourceIp,
                    ThreatCategory = t.ThreatCategory,
                    ConfidenceScore = t.ConfidenceScore,
                    ReportedTicks = t.ReportedUtc.Ticks,
                    ExpiresTicks = t.ExpiresUtc.Ticks,
                    ReporterNodeId = t.ReporterNodeId,
                    ReporterNodeName = t.ReporterNodeName,
                    Reason = t.Notes
                });
            }

            await responseStream.WriteAsync(chunk, context.CancellationToken).ConfigureAwait(false);
            if (!page.HasMore) break;
            cursor = page.NextCursor;
        }
    }
}
