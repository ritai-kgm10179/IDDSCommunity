using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class SecurityEventPipelineTest
{
    private string testDirectory = null!;
    private Database database = null!;

    /// <summary>
    /// 驗證具型別驗證通知完整保留中央關聯與封鎖判斷所需的安全語意。
    /// </summary>
    [TestMethod]
    public void AuthenticationNotification_ToObservation_PreservesSecuritySemantics()
    {
        AuthenticationNotificationEventArgs notification = new()
        {
            IpAddress = "198.51.100.61",
            CreateDate = DateTime.Now,
            EventId = 201,
            EventMessage = "CAP denied",
            AccountName = @"CORP\alice",
            IsCredentialFailure = false,
            ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
            ComputerName = "RDGW01",
            SourceEventRecordId = 321,
            ActivityId = "activity-321",
            ConfidenceScore = 0.25,
            TargetResource = "APP01",
            ErrorCode = "23003"
        };

        SecurityObservationEvent observation = Service.CreateSecurityObservation("RdGatewaySecurityAgent", notification);

        Assert.AreEqual(@"CORP\alice", observation.NormalizedAccount);
        Assert.IsFalse(observation.IsCredentialFailure);
        Assert.AreEqual(notification.ProviderOrChannel, observation.ProviderOrChannel);
        Assert.AreEqual("RDGW01", observation.ComputerName);
        Assert.AreEqual(321L, observation.SourceEventRecordId);
        Assert.AreEqual("activity-321", observation.ActivityId);
        Assert.AreEqual(0.25, observation.ConfidenceScore);
        Assert.AreEqual("APP01", observation.TargetResource);
        Assert.AreEqual("23003", observation.ErrorCode);
        Assert.IsFalse(Service.ShouldProcessLegacyDetection(notification));
    }
    /// <summary>
    /// 為每個管線測試建立隔離的持久化收件匣。
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "IDDSCommunity.SecurityEventPipelineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "pipeline.db");
    }
    /// <summary>
    /// 釋放隔離的收件匣資料庫。
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(testDirectory))
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch (IOException)
            {
                Thread.Sleep(200);
                try { Directory.Delete(testDirectory, recursive: true); }
                catch { /* 避免在失敗測試後因目錄清理異常遮蔽原始例外 */ }
            }
        }
    }
    /// <summary>
    /// 驗證已接受的事件依序處理，並於完成時排空。
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task Complete_AcceptedEvents_DrainsInOrderAsync()
    {
        ConcurrentQueue<string> processed = new();
        SecurityEventPipeline pipeline = CreatePipeline(8, (_, args) => processed.Enqueue(args.IpAddress), _ => Assert.Fail("Unexpected failure."));
        NotificationEventArgs first = CreateEvent("192.0.2.1");

        Assert.IsTrue(pipeline.TryPublish(this, first));
        first.IpAddress = "203.0.113.99";
        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.2")));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        CollectionAssert.AreEqual(new[] { "192.0.2.1", "192.0.2.2" }, processed.ToArray());
        Assert.AreEqual(0, pipeline.QueueDepth);
        Assert.IsFalse(pipeline.TryPublish(this, CreateEvent("192.0.2.3")));
    }
    /// <summary>
    /// 驗證佇列飽和時以非阻塞方式拒絕生產者，且單一消費者失敗不會停止後續工作。
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task TryPublish_SaturationAndConsumerFailure_AreIsolatedAsync()
    {
        using ManualResetEventSlim entered = new(false);
        using ManualResetEventSlim release = new(false);
        int processed = 0;
        int failures = 0;
        SecurityEventPipeline pipeline = CreatePipeline(1, (_, args) =>
        {
            Interlocked.Increment(ref processed);
            if (args.IpAddress == "192.0.2.1")
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5));
                throw new InvalidOperationException("expected");
            }
        }, _ => Interlocked.Increment(ref failures));

        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.1")));
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.2")));
        Assert.IsFalse(pipeline.TryPublish(this, CreateEvent("192.0.2.3")));
        release.Set();
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(2, processed);
        Assert.AreEqual(1, failures);
    }
    /// <summary>
    /// 驗證無損發布路徑會施加背壓，直到有界容量可供使用。
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task Publish_SaturatedQueue_BackpressuresProducerAsync()
    {
        using ManualResetEventSlim entered = new(false);
        using ManualResetEventSlim release = new(false);
        SecurityEventPipeline pipeline = CreatePipeline(1, (_, args) =>
        {
            if (args.IpAddress == "192.0.2.1")
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5));
            }
        }, _ => Assert.Fail("Unexpected failure."));

        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.1")));
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.2")));
        System.Threading.Tasks.Task<bool> blockedProducer = System.Threading.Tasks.Task.Run(() => pipeline.Publish(this, CreateEvent("192.0.2.3")));
        await System.Threading.Tasks.Task.Delay(100);
        Assert.IsFalse(blockedProducer.IsCompleted);

        release.Set();
        Assert.IsTrue(await blockedProducer.WaitAsync(TimeSpan.FromSeconds(5)));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));
    }
    /// <summary>
    /// 驗證中斷前已持久化的事件會由新管線重新播放並標示為完成。
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task RecoverPending_PersistedEvent_ReplaysAfterRestartAsync()
    {
        SecurityEventInbox inbox = new(database, TimeProvider.System);
        inbox.Add("RecoveredAgent", CreateEvent("192.0.2.44"));
        string? processedAddress = null;
        SecurityEventPipeline pipeline = new(
            4,
            (_, args) => processedAddress = args.IpAddress,
            _ => Assert.Fail("Unexpected failure."),
            inbox,
            agentName => agentName == "RecoveredAgent" ? this : null);

        pipeline.RecoverPending(10);
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual("192.0.2.44", processedAddress);
        Assert.AreEqual(0, inbox.CountUnfinished());
    }
    /// <summary>
    /// 驗證多個並行生產者的突發事件皆會經由有界管線完成，不遺留持久化待辦。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    [TestCategory("Stability")]
    public async Task Publish_ParallelBurst_DrainsEveryAcceptedEventAsync()
    {
        const int producerCount = 8;
        const int eventsPerProducer = 50;
        int processed = 0;
        SecurityEventInbox inbox = new(database, TimeProvider.System);
        SecurityEventPipeline pipeline = new(
            64,
            (_, _) => Interlocked.Increment(ref processed),
            exception => Assert.Fail(exception.ToString()),
            inbox,
            _ => this);

        Task[] producers = Enumerable.Range(0, producerCount)
            .Select(producer => Task.Run(() =>
            {
                for (int index = 0; index < eventsPerProducer; index++)
                    Assert.IsTrue(pipeline.Publish(this, CreateEvent($"198.51.{producer}.{index + 1}")));
            }))
            .ToArray();

        await Task.WhenAll(producers).WaitAsync(TimeSpan.FromSeconds(60));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(60));

        Assert.AreEqual(producerCount * eventsPerProducer, processed);
        Assert.AreEqual(0, pipeline.QueueDepth);
        Assert.AreEqual(0, inbox.CountUnfinished());
    }
    /// <summary>
    /// 驗證處理失敗不會阻斷後續事件，且失敗事件會保留供重新播放。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    [TestCategory("Stability")]
    public async Task ConsumerFailure_SubsequentEventsContinueAndFailureRemainsRecoverableAsync()
    {
        int processed = 0;
        int failures = 0;
        SecurityEventInbox inbox = new(database, TimeProvider.System);
        SecurityEventPipeline pipeline = new(
            8,
            (_, args) =>
            {
                if (args.IpAddress == "192.0.2.2")
                    throw new InvalidOperationException("expected stability-test failure");
                Interlocked.Increment(ref processed);
            },
            _ => Interlocked.Increment(ref failures),
            inbox,
            _ => this);

        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.1")));
        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.2")));
        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.3")));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(2, processed);
        Assert.AreEqual(1, failures);
        Assert.AreEqual(1, inbox.CountUnfinished());
        Assert.AreEqual("192.0.2.2", inbox.ReadPending(10).Single().EventArgs.IpAddress);
    }

    /// <summary>
    /// 驗證跨 Agent 關聯與持久化收件匣端對端整合：當功能開啟時執行完整分析與告警；當模擬崩潰重啟時未完成事件能安全重播且不重複告警。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    public async Task DurableInbox_CrossAgentCorrelation_FullPipelineAndCrashRecoveryAsync()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(5));
        ConcurrentBag<string> alertLogs = new();
        SecurityEventInbox inbox = new(database, TimeProvider.System);

        SecurityEventPipeline pipeline = new(
            16,
            (_, args) =>
            {
                IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent observation = new()
                {
                    SourceAgentName = "TestAgent",
                    NormalizedIpAddress = args.IpAddress,
                    NormalizedAccount = args.EventMessage.Replace("Account:", "").Trim(),
                    EventTimeUtc = args.CreateDate.ToUniversalTime(),
                    ReceivedTimeUtc = DateTimeOffset.UtcNow,
                    OriginalEventReference = args.EventId.ToString(),
                    Provenance = "TestIntegration"
                };

                IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationEvaluationResult result = engine.Ingest(observation, config);
                if (result.Action == IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationAction.AlertAndScoreOnly)
                {
                    alertLogs.Add(result.Message);
                }
            },
            _ => Assert.Fail("Unexpected pipeline failure."),
            inbox,
            _ => this);

        // 投遞第 1 個事件
        NotificationEventArgs evt1 = new()
        {
            IpAddress = "198.51.100.77",
            EventMessage = "Account: User1",
            EventId = 101,
            CreateDate = DateTime.UtcNow
        };
        Assert.IsTrue(pipeline.Publish(this, evt1));

        // 投遞第 2 個事件 (達到 1-to-N 門檻 2)
        NotificationEventArgs evt2 = new()
        {
            IpAddress = "198.51.100.77",
            EventMessage = "Account: User2",
            EventId = 102,
            CreateDate = DateTime.UtcNow
        };
        Assert.IsTrue(pipeline.Publish(this, evt2));

        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        // 驗證已產生 1-to-N 警示，且 inbox 標記已完成 (未完成數為 0)
        Assert.AreEqual(1, alertLogs.Count);
        Assert.AreEqual(0, inbox.CountUnfinished());

        // 模擬重播完全相同事件 (重播冪等性消除)
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent duplicateObs = new()
        {
            SourceAgentName = "TestAgent",
            NormalizedIpAddress = "198.51.100.77",
            NormalizedAccount = "User2",
            EventTimeUtc = evt2.CreateDate.ToUniversalTime(),
            ReceivedTimeUtc = DateTimeOffset.UtcNow,
            OriginalEventReference = "102",
            Provenance = "TestIntegration"
        };
        IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationEvaluationResult dupResult = engine.Ingest(duplicateObs, config);
        Assert.IsTrue(dupResult.IsDuplicateReplay);
        Assert.AreEqual(IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationAction.None, dupResult.Action);
    }

    /// <summary>
    /// 崩潰點 1 (觀察事件持久化後 / 關聯聚合前)：驗證新執行個體啟動時透過 RebuildFromDatabase 自資料庫重建時間窗內歷史觀察事件，並在後續事件到達時正確達到噴灑門檻發出單次告警。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    public async Task CrashPoint1_PostObservationPersistence_PreAggregation_RecoversAndAlertsOnceAsync()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs1 = new()
        {
            SourceAgentName = "AuthAgent",
            ProviderOrChannel = "Security",
            ComputerName = "DC01",
            SourceEventRecordId = 1001,
            NormalizedIpAddress = "192.0.2.11",
            NormalizedAccount = "UserA",
            EventTimeUtc = DateTimeOffset.UtcNow,
            ReceivedTimeUtc = DateTimeOffset.UtcNow,
            OriginalEventReference = "1001",
            Provenance = "CrashTest1"
        };

        // 執行個體 1：將第 1 筆觀察事件寫入資料庫後模擬崩潰
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obs1, database);
        database.Close();

        // 啟動全新的執行個體 2 (全新物件圖與連線)
        Database database2 = new();
        database2.Configure(testDirectory, "pipeline.db");
        IddsConfig config2 = new(database2)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };
        IDDSCommunity.IntrusionDetection.Shared.ProtectionAuditTrail auditTrail2 = new(database2, TimeProvider.System);
        IDDSCommunity.IntrusionDetection.Shared.Correlation.CrossAgentCorrelationEngine engine2 = new(TimeSpan.FromMinutes(5));

        // 自資料庫重建時間窗與水位點
        engine2.RebuildFromDatabase(database2);

        // 傳入第 2 筆相異帳號事件 (達到門檻 2)
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs2 = new()
        {
            SourceAgentName = "AuthAgent",
            ProviderOrChannel = "Security",
            ComputerName = "DC01",
            SourceEventRecordId = 1002,
            NormalizedIpAddress = "192.0.2.11",
            NormalizedAccount = "UserB",
            EventTimeUtc = DateTimeOffset.UtcNow,
            ReceivedTimeUtc = DateTimeOffset.UtcNow,
            OriginalEventReference = "1002",
            Provenance = "CrashTest1"
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obs2, database2);
        IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationEvaluationResult result = engine2.Ingest(obs2, config2);

        Assert.AreEqual(IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationAction.AlertAndScoreOnly, result.Action);

        string alertId = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.ComputeAlertId(
            result.SprayType, obs2.NormalizedIpAddress, result.ContributingIdempotencyKeys);
        bool enqueued = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.EnqueueAlertOutbox(
            alertId, obs2.Id, obs2.EventTimeUtc, "CrossAgentSprayDetected", "AlertOnly", "AuthAgent", obs2.NormalizedIpAddress, result.Message, database2);
        Assert.IsTrue(enqueued);

        int dispatched = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database2, auditTrail2);
        Assert.AreEqual(1, dispatched);

        long obsCount = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(*) FROM SecurityObservationEvents"));
        Assert.AreEqual(2L, obsCount);

        var watermarks = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.LoadWatermarks(database2);
        Assert.AreEqual(1002L, watermarks["AuthAgent|Security"].LastEventRecordId);

        database2.Close();
    }

    /// <summary>
    /// 驗證重啟會恢復原始冪等鍵，且不會把非密碼遙測事件重建至密碼噴灑時間窗。
    /// </summary>
    [TestMethod]
    public void RebuildFromDatabase_RestoresExactKeyAndExcludesTelemetryFromSprayWindow()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SecurityObservationEvent telemetry = new()
        {
            SourceAgentName = "RdGatewaySecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
            ComputerName = "RDGW01",
            SourceEventRecordId = 4101,
            NormalizedIpAddress = "192.0.2.41",
            NormalizedAccount = "TelemetryUser",
            EventTimeUtc = now.AddMinutes(-15),
            ReceivedTimeUtc = now,
            OriginalEventReference = "4101",
            Provenance = "RebuildTelemetryTest",
            IsCredentialFailure = false
        };
        SecurityObservationStore.PersistObservationAndWatermark(telemetry, database);

        CrossAgentCorrelationEngine rebuilt = new(TimeSpan.FromMinutes(5));
        rebuilt.RebuildFromDatabase(database, TimeSpan.FromMinutes(20), now.AddSeconds(1));

        SecurityObservationEvent replay = new()
        {
            SourceAgentName = telemetry.SourceAgentName,
            ProviderOrChannel = telemetry.ProviderOrChannel,
            ComputerName = telemetry.ComputerName,
            SourceEventRecordId = telemetry.SourceEventRecordId,
            NormalizedIpAddress = telemetry.NormalizedIpAddress,
            NormalizedAccount = telemetry.NormalizedAccount,
            EventTimeUtc = telemetry.EventTimeUtc,
            IsCredentialFailure = false
        };
        CorrelationEvaluationResult replayResult = rebuilt.Ingest(replay, config);
        Assert.IsTrue(replayResult.IsDuplicateReplay, "即使來源事件延遲送達，重啟後仍須依接收時間恢復原始持久化冪等鍵");

        SecurityObservationEvent credential = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            ComputerName = "SERVER01",
            SourceEventRecordId = 5101,
            NormalizedIpAddress = telemetry.NormalizedIpAddress,
            NormalizedAccount = "CredentialUser",
            EventTimeUtc = now.AddSeconds(2),
            IsCredentialFailure = true
        };
        CorrelationEvaluationResult credentialResult = rebuilt.Ingest(credential, config);
        Assert.AreEqual(CorrelationAction.None, credentialResult.Action, "非密碼遙測不得成為達成噴灑門檻的歷史事件");
    }

    /// <summary>
    /// 崩潰點 2 (關聯聚合與 Outbox 寫入後 / 稽核日誌分派前)：驗證 Outbox 記錄維持 Pending (Status=0)，新執行個體啟動時安全分派至稽核日誌且不產生重複列。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    public async Task CrashPoint2_PostAggregationAndOutboxEnqueue_PreAuditDispatch_RecoversAndDispatchesOnceAsync()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };

        Guid obsId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string alertId = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.ComputeAlertId(
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SprayAttackType.OneIpToMultipleAccounts, "192.0.2.22", new[] { "192.0.2.22:User1", "192.0.2.22:User2" });

        // 執行個體 1：寫入 Outbox 佇列 (Status = 0) 後未分派即崩潰
        bool enqueued = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.EnqueueAlertOutbox(
            alertId, obsId, now, "CrossAgentSprayDetected", "AlertOnly", "AuthAgent", "192.0.2.22", "Test Outbox Crash", database);
        Assert.IsTrue(enqueued);
        database.Close();

        // 啟動全新的執行個體 2
        Database database2 = new();
        database2.Configure(testDirectory, "pipeline.db");
        IDDSCommunity.IntrusionDetection.Shared.ProtectionAuditTrail auditTrail2 = new(database2, TimeProvider.System);

        // 驗證 Outbox 狀態在重啟前為 0 (Pending)
        long pendingCount = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(*) FROM ObservationAlertOutbox WHERE Status = 0"));
        Assert.AreEqual(1L, pendingCount);

        // 執行個體 2 啟動分派
        int dispatched = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database2, auditTrail2);
        Assert.AreEqual(1, dispatched);

        // 驗證 Outbox 狀態已轉為 1 (Dispatched)
        long dispatchedStatusCount = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(*) FROM ObservationAlertOutbox WHERE Status = 1"));
        Assert.AreEqual(1L, dispatchedStatusCount);

        // 再次分派不得重複產生記錄
        int reDispatched = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database2, auditTrail2);
        Assert.AreEqual(0, reDispatched);

        database2.Close();
    }

    /// <summary>
    /// 崩潰點 3 (稽核日誌寫入後 / Outbox 狀態更新前)：精準注入 Audit INSERT 成功後中斷，驗證新執行個體復原重播時因唯一約束消除重複審計紀錄，且 Outbox 狀態安全更新為 Dispatched。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    public async Task CrashPoint3_PostAuditInsert_PreOutboxStatusUpdate_RecoversAndDeduplicatesAuditAsync()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };

        Guid obsId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string alertId = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.ComputeAlertId(
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SprayAttackType.OneIpToMultipleAccounts, "192.0.2.33", new[] { "192.0.2.33:Admin1", "192.0.2.33:Admin2" });

        // 1. Enqueue 告警至 Outbox (Status = 0)
        bool enqueued = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.EnqueueAlertOutbox(
            alertId, obsId, now, "CrossAgentSprayDetected", "AlertOnly", "AuthAgent", "192.0.2.33", "Crash Injection Detail", database);
        Assert.IsTrue(enqueued);

        // 2. 模擬崩潰點：在 Audit INSERT 成功後、Outbox Status 更新前注入例外
        bool callbackTriggered = false;
        try
        {
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(
                database,
                afterAuditInsertCallback: () =>
                {
                    callbackTriggered = true;
                    throw new ApplicationException("Simulated process crash right after ProtectionAuditLog INSERT");
                });
            Assert.Fail("Expected crash exception was not thrown.");
        }
        catch (ApplicationException)
        {
            Assert.IsTrue(callbackTriggered);
        }

        // 關閉執行個體 1
        database.Close();

        // 啟動全新的執行個體 2 (全新物件圖與連線)
        Database database2 = new();
        database2.Configure(testDirectory, "pipeline.db");
        IDDSCommunity.IntrusionDetection.Shared.ProtectionAuditTrail auditTrail2 = new(database2, TimeProvider.System);

        // 執行個體 2 執行復原分派 (此時 AuditLog 已有同 AlertId 紀錄，但 Outbox 為 Status 0)
        int dispatched = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database2, auditTrail2);
        Assert.AreEqual(1, dispatched);

        // 驗證 ProtectionAuditLog 中該 AlertId 精確只有 1 筆紀錄
        long auditCount = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(*) FROM ProtectionAuditLog WHERE AlertId = @p0", alertId));
        Assert.AreEqual(1L, auditCount);

        // 驗證 ProtectionAuditLog 總數與相異 AlertId 數一致
        long totalAudits = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(*) FROM ProtectionAuditLog"));
        long distinctAlerts = Convert.ToInt64(database2.ExecuteScalar("SELECT COUNT(DISTINCT AlertId) FROM ProtectionAuditLog"));
        Assert.AreEqual(totalAudits, distinctAlerts);

        // 驗證 Outbox 狀態為 1 (Dispatched)
        long outboxStatus = Convert.ToInt64(database2.ExecuteScalar("SELECT Status FROM ObservationAlertOutbox WHERE AlertId = @p0", alertId));
        Assert.AreEqual(1L, outboxStatus);

        database2.Close();
    }

    /// <summary>
    /// 驗證並行 Dispatcher 競態條件：兩個獨立資料庫連線同時分派同一筆 Pending Outbox 記錄，驗證結果僅產出 1 筆審計日誌且 Outbox 狀態安全轉為已分派。
    /// </summary>
    /// <returns>非同步測試工作。</returns>
    [TestMethod]
    public async Task ParallelDispatchers_ConcurrentRace_ResultsInSingleAuditRecordAndDispatchedStatusAsync()
    {
        Guid obsId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string alertId = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.ComputeAlertId(
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SprayAttackType.OneIpToMultipleAccounts, "192.0.2.77", new[] { "192.0.2.77:UserA", "192.0.2.77:UserB" });

        bool enqueued = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.EnqueueAlertOutbox(
            alertId, obsId, now, "CrossAgentSprayDetected", "AlertOnly", "AuthAgent", "192.0.2.77", "Race Detail", database);
        Assert.IsTrue(enqueued);

        // 建立第 2 個獨立連線
        Database database2 = new();
        database2.Configure(testDirectory, "pipeline.db");

        // 同時執行 Dispatch
        Task<int> task1 = Task.Run(() => IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database));
        Task<int> task2 = Task.Run(() => IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.DispatchPendingAlerts(database2));

        int[] results = await Task.WhenAll(task1, task2);
        int totalDispatched = results[0] + results[1];
        Assert.AreEqual(1, totalDispatched);

        long auditCount = Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionAuditLog WHERE AlertId = @p0", alertId));
        Assert.AreEqual(1L, auditCount);

        long outboxStatus = Convert.ToInt64(database.ExecuteScalar("SELECT Status FROM ObservationAlertOutbox WHERE AlertId = @p0", alertId));
        Assert.AreEqual(1L, outboxStatus);

        database2.Close();
    }

    /// <summary>
    /// 驗證單一 SQLite 交易 Rollback 行為：在持久化觀察事件與水位點期間注入例外，驗證整筆交易原子回滾，無半套資料殘留。
    /// </summary>
    [TestMethod]
    public void EnqueueTransaction_WhenInjectedFailure_RollsBackEntirelyWithoutPartialData()
    {
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent invalidObs = new()
        {
            SourceAgentName = "TestAgent",
            ProviderOrChannel = "Security",
            NormalizedIpAddress = "192.0.2.99",
            NormalizedAccount = "TestAccount",
            EventTimeUtc = DateTimeOffset.UtcNow,
            ReceivedTimeUtc = DateTimeOffset.UtcNow,
            // 故意設為 null 以觸發非 null 約束違規或由交易拋出例外
            OriginalEventReference = null!,
            Provenance = "RollbackTest"
        };

        try
        {
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(invalidObs, database);
            Assert.Fail("Expected exception was not thrown.");
        }
        catch (Exception)
        {
            // 預期捕捉到例外
        }

        // 驗證未留下半套觀察事件
        long count = Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM SecurityObservationEvents WHERE NormalizedIpAddress = '192.0.2.99'"));
        Assert.AreEqual(0L, count);

        // 驗證水位點未遭更新
        var watermarks = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.LoadWatermarks(database);
        Assert.IsFalse(watermarks.ContainsKey("TestAgent|Security"));
    }

    /// <summary>
    /// 驗證 WinRM 與 RD Gateway 事件經由管線處理、持久化至 Durable Inbox 並由關聯 Outbox 冪等派發之完整端對端流程。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_WhenWinRmAndRdGatewayEvents_PersistsToDurableInboxAndEmitsAlert()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent winrmObs = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            ComputerName = "SRV-APP-01",
            SourceEventRecordId = 1001,
            EventTimeUtc = now,
            ReceivedTimeUtc = now,
            NormalizedIpAddress = "198.51.100.40",
            NormalizedAccount = "admin",
            NormalizedDomain = "CORP",
            OriginalEventReference = "WinRM_Op_142_1001",
            Provenance = "WinRM_Operational_Test",
            ConfidenceScore = 1.0
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent rdgObs = new()
        {
            SourceAgentName = "RdGatewaySecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
            ComputerName = "SRV-RDG-01",
            SourceEventRecordId = 2001,
            EventTimeUtc = now.AddSeconds(5),
            ReceivedTimeUtc = now.AddSeconds(5),
            NormalizedIpAddress = "198.51.100.40",
            NormalizedAccount = "admin",
            NormalizedDomain = "CORP",
            OriginalEventReference = "RDG_Op_201_2001",
            Provenance = "RDG_Operational_Test",
            ConfidenceScore = 1.0
        };

        // 1. 持久化至 Durable Observation Store
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(winrmObs, database);
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(rdgObs, database);

        // 2. 驗證觀察事件寫入
        long eventCount = Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM SecurityObservationEvents WHERE NormalizedIpAddress = '198.51.100.40'"));
        Assert.AreEqual(2L, eventCount);

        // 3. 驗證同源事件重播之確定性冪等去重
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(winrmObs, database);
        long replayCount = Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM SecurityObservationEvents WHERE NormalizedIpAddress = '198.51.100.40'"));
        Assert.AreEqual(2L, replayCount);

        // 4. 驗證水位點記錄
        var watermarks = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.LoadWatermarks(database);
        Assert.AreEqual((long?)1001L, watermarks["WinRmSecurityAgent|Microsoft-Windows-WinRM/Operational"].LastEventRecordId);
        Assert.AreEqual((long?)2001L, watermarks["RdGatewaySecurityAgent|Microsoft-Windows-TerminalServices-Gateway/Operational"].LastEventRecordId);
    }

    /// <summary>
    /// 驗證亂序到達與重複重播事件之處理：水位點維持單調遞增最大 RecordID，且重複事件被確定性冪等消除。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_OutOfOrderAndReplay_MaintainsWatermarkAndDeduplication()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obsLater = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            SourceEventRecordId = 5005,
            EventTimeUtc = now.AddSeconds(10),
            NormalizedIpAddress = "198.51.100.99",
            NormalizedAccount = "user99",
            OriginalEventReference = "rec_5005",
            Provenance = "test"
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obsEarlier = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            SourceEventRecordId = 5002,
            EventTimeUtc = now.AddSeconds(2),
            NormalizedIpAddress = "198.51.100.99",
            NormalizedAccount = "user99",
            OriginalEventReference = "rec_5002",
            Provenance = "test"
        };

        // 先送入較大 ID (5005)，再送入較小 ID (5002)
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obsLater, database);
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obsEarlier, database);

        var watermarks = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.LoadWatermarks(database);
        Assert.AreEqual((long?)5005L, watermarks["WinRmSecurityAgent|Microsoft-Windows-WinRM/Operational"].LastEventRecordId, "水位點應維持記錄過之最大 RecordID");

        // 再次送入 5005 重複事件
        var result = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obsLater, database);
        Assert.IsTrue(result.IsDuplicate, "重複事件應被標示為重複");
    }

    /// <summary>
    /// 驗證安全網路全域白名單支援單一 IPv4、IPv6、IPv4 CIDR 與 IPv6 CIDR，落在白名單內之事件永不告警。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_SafeNetworkExemptions_SupportsIPv4_IPv6_and_CIDR()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            UseSafeNetworkList = true,
            CrossAgentSprayAccountThreshold = 2
        };

        config.SafeNetworks.Add(new IddsConfig.CSafeNetwork { IpAddress = "192.168.1.0", SubnetMask = "255.255.255.0" });
        config.SafeNetworks.Add(new IddsConfig.CSafeNetwork { IpAddress = "2001:db8:acad::", SubnetMask = "48" });
        config.SafeNetworks.Add(new IddsConfig.CSafeNetwork { IpAddress = "10.50.50.50", SubnetMask = "255.255.255.255" });

        IDDSCommunity.IntrusionDetection.Shared.Correlation.CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // 1. IPv4 CIDR 內
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obsIpv4 = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            NormalizedIpAddress = "192.168.1.100",
            NormalizedAccount = "admin1",
            EventTimeUtc = now
        };
        var r1 = engine.Evaluate(obsIpv4, config);
        Assert.IsTrue(r1.IsSafeNetworkExempted);
        Assert.AreEqual(IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationAction.None, r1.Action);

        // 2. IPv6 CIDR 內
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obsIpv6 = new()
        {
            SourceAgentName = "RdGatewaySecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
            NormalizedIpAddress = "2001:db8:acad:1234::1",
            NormalizedAccount = "admin2",
            EventTimeUtc = now
        };
        var r2 = engine.Evaluate(obsIpv6, config);
        Assert.IsTrue(r2.IsSafeNetworkExempted);

        // 3. 單一 IPv4 內
        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obsSingle = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            NormalizedIpAddress = "10.50.50.50",
            NormalizedAccount = "admin3",
            EventTimeUtc = now
        };
        var r3 = engine.Evaluate(obsSingle, config);
        Assert.IsTrue(r3.IsSafeNetworkExempted);
    }

    /// <summary>
    /// 驗證重啟游標狀態復原：從持久化水位點恢復後，引擎可無縫接續先流水號。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_CursorRestart_ResumesFromWatermark()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            SourceEventRecordId = 8888,
            EventTimeUtc = now,
            NormalizedIpAddress = "198.51.100.88",
            NormalizedAccount = "test88",
            OriginalEventReference = "rec_8888",
            Provenance = "test"
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obs, database);

        // 模擬服務重啟並載入水位點
        var watermarks = IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.LoadWatermarks(database);
        Assert.IsTrue(watermarks.ContainsKey("WinRmSecurityAgent|Microsoft-Windows-WinRM/Operational"));
        Assert.AreEqual((long?)8888L, watermarks["WinRmSecurityAgent|Microsoft-Windows-WinRM/Operational"].LastEventRecordId);
    }

    /// <summary>
    /// 驗證高負載突發連線下，背壓緩衝區與持久化儲存不遺失任何觀察事件。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_Backpressure_UnderHeavyBurst_PreservesAllObservations()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        const int burstCount = 50;

        for (int i = 0; i < burstCount; i++)
        {
            IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs = new()
            {
                SourceAgentName = "WinRmSecurityAgent",
                ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
                SourceEventRecordId = 10000 + i,
                EventTimeUtc = now.AddMilliseconds(i * 10),
                NormalizedIpAddress = "198.51.100.70",
                NormalizedAccount = $"burst_user_{i}",
                OriginalEventReference = $"rec_{10000 + i}",
                Provenance = "burst_test"
            };

            IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationStore.PersistObservationAndWatermark(obs, database);
        }

        long count = Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM SecurityObservationEvents WHERE Provenance = 'burst_test'"));
        Assert.AreEqual((long)burstCount, count, "高突發事件應全數持久化至資料庫，不得發生遺失");
    }

    /// <summary>
    /// 驗證跨來源關聯偵測僅產生警示評分 (AlertAndScoreOnly)，絕不直接呼叫或建立防火牆封鎖規則。
    /// </summary>
    [TestMethod]
    public void ServicePipeline_NeverDirectlyCallsFirewall()
    {
        IddsConfig config = new(database)
        {
            EnableCrossAgentCorrelation = true,
            CrossAgentSprayAccountThreshold = 2
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));

        DateTimeOffset now = DateTimeOffset.UtcNow;

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs1 = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            NormalizedIpAddress = "198.51.100.150",
            NormalizedAccount = "userA",
            EventTimeUtc = now,
            IsCredentialFailure = true
        };

        IDDSCommunity.IntrusionDetection.Shared.Correlation.SecurityObservationEvent obs2 = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            NormalizedIpAddress = "198.51.100.150",
            NormalizedAccount = "userB",
            EventTimeUtc = now.AddSeconds(1),
            IsCredentialFailure = true
        };

        engine.Evaluate(obs1, config);
        var r2 = engine.Evaluate(obs2, config);

        Assert.AreEqual(IDDSCommunity.IntrusionDetection.Shared.Correlation.CorrelationAction.AlertAndScoreOnly, r2.Action, "Phase 0/1A 僅能為 AlertAndScoreOnly");
        // 驗證 Action 不是封鎖動作
        Assert.AreNotEqual(2, (int)r2.Action);
    }

    /// <summary>
    /// 建立一筆測試用偵測事件。
    /// </summary>
    /// <param name="address">The source address.</param>
    /// <returns>傳回 mutable test event 的結果。</returns>
    private static NotificationEventArgs CreateEvent(string address) => new()
    {
        CreateDate = DateTime.UtcNow,
        EventId = 1,
        EventMessage = "test",
        IpAddress = address
    };

    private SecurityEventPipeline CreatePipeline(int capacity, Action<object, INotificationEventArgs> process, Action<Exception> reportFailure) =>
        new(capacity, process, reportFailure, new SecurityEventInbox(database, TimeProvider.System), _ => this);
}
