namespace IDDSCommunity.IntrusionDetection.Shared.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 驗證 Phase 0 跨 Agent 安全性觀察事件正規化、確定性冪等去重、滑動時間窗、密碼噴灑偵測與安全網路排除邏輯之單元測試類別。
/// </summary>
[TestClass]
[DoNotParallelize]
public class CrossAgentCorrelationTest
{
    /// <summary>
    /// 驗證網域反斜線、UPN 與明確網域欄位可產生一致的帳號身分鍵值。
    /// </summary>
    [TestMethod]
    public void AccountIdentityNormalizer_CommonWindowsFormats_ProduceStableIdentity()
    {
        string downLevel = AccountIdentityNormalizer.BuildKey("CONTOSO\\Alice", null, null);
        string upn = AccountIdentityNormalizer.BuildKey("alice@contoso", null, null);
        string separated = AccountIdentityNormalizer.BuildKey("alice", "CONTOSO", null);

        Assert.AreEqual(downLevel, upn);
        Assert.AreEqual(downLevel, separated);
        Assert.AreEqual(
            AccountIdentityNormalizer.BuildKey("renamed-user", "OTHER", "S-1-5-21-1-2-3-1001"),
            AccountIdentityNormalizer.BuildKey("alice", "CONTOSO", "s-1-5-21-1-2-3-1001"));
    }

    /// <summary>
    /// 驗證不同 Agent 對同一次驗證失敗的回報只會有第一筆進入計數時間窗。
    /// </summary>
    [TestMethod]
    public void Ingest_SameAuthenticationFromDifferentSources_MarksCrossSourceDuplicate()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        SecurityObservationEvent first = new()
        {
            SourceAgentName = "WindowsNetworkLogon",
            ProviderOrChannel = "Security",
            ComputerName = "SERVER01",
            SourceEventRecordId = 4625001,
            NormalizedIpAddress = "::ffff:192.0.2.70",
            NormalizedAccount = "Alice",
            EventTimeUtc = occurredAt
        };
        SecurityObservationEvent second = new()
        {
            SourceAgentName = "NpsRadius",
            ProviderOrChannel = "Security",
            ComputerName = "NPS01",
            SourceEventRecordId = 6273001,
            NormalizedIpAddress = "192.0.2.70",
            NormalizedAccount = "alice",
            EventTimeUtc = occurredAt.AddMilliseconds(500)
        };

        CorrelationEvaluationResult firstResult = engine.Ingest(first, config);
        CorrelationEvaluationResult secondResult = engine.Ingest(second, config);

        Assert.IsFalse(firstResult.IsCrossSourceDuplicate);
        Assert.IsTrue(secondResult.IsCrossSourceDuplicate);
        Assert.AreEqual(CorrelationAction.None, secondResult.Action);
    }

    /// <summary>
    /// 驗證語意去重時間差可由設定控制，超出容許範圍的獨立事件不得被合併。
    /// </summary>
    [TestMethod]
    public void Ingest_EventsOutsideConfiguredSemanticTolerance_RemainIndependent()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        config.CrossAgentSemanticDeduplicationSeconds = 1;
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        SecurityObservationEvent first = new()
        {
            SourceAgentName = "WindowsNetworkLogon",
            ProviderOrChannel = "Security",
            ComputerName = "SERVER01",
            SourceEventRecordId = 7001,
            NormalizedIpAddress = "192.0.2.71",
            NormalizedAccount = "CONTOSO\\alice",
            EventTimeUtc = occurredAt
        };
        SecurityObservationEvent second = new()
        {
            SourceAgentName = "NpsRadius",
            ProviderOrChannel = "Security",
            ComputerName = "NPS01",
            SourceEventRecordId = 7002,
            NormalizedIpAddress = "192.0.2.71",
            NormalizedAccount = "alice@contoso",
            EventTimeUtc = occurredAt.AddSeconds(2)
        };

        engine.Ingest(first, config);
        CorrelationEvaluationResult result = engine.Ingest(second, config);

        Assert.IsFalse(result.IsCrossSourceDuplicate);
    }

    /// <summary>
    /// 驗證不同來源但具有相同 ActivityID 的事件會取得相同的穩定關聯群組。
    /// </summary>
    [TestMethod]
    public void Ingest_SameActivityIdAcrossAgents_AssignsSameCorrelationGroup()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        SecurityObservationEvent winRm = new()
        {
            SourceAgentName = "WinRmSecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-WinRM/Operational",
            ComputerName = "SERVER01",
            SourceEventRecordId = 101,
            NormalizedIpAddress = "192.0.2.88",
            NormalizedAccount = "alice",
            EventTimeUtc = DateTimeOffset.UtcNow,
            ActivityId = "{11111111-2222-3333-4444-555555555555}"
        };
        SecurityObservationEvent gateway = new()
        {
            SourceAgentName = "RdGatewaySecurityAgent",
            ProviderOrChannel = "Microsoft-Windows-TerminalServices-Gateway/Operational",
            ComputerName = "RDGW01",
            SourceEventRecordId = 202,
            NormalizedIpAddress = "192.0.2.88",
            NormalizedAccount = "alice",
            EventTimeUtc = winRm.EventTimeUtc.AddSeconds(30),
            ActivityId = winRm.ActivityId
        };

        CorrelationEvaluationResult first = engine.Ingest(winRm, config);
        CorrelationEvaluationResult second = engine.Ingest(gateway, config);

        Assert.IsNotNull(first.CorrelationGroupId);
        Assert.AreEqual(first.CorrelationGroupId, second.CorrelationGroupId);
        Assert.IsTrue(second.IsCrossSourceDuplicate, "明確 Activity ID 應在完整關聯時間窗內去重，不受五秒推論視窗限制");
    }

    /// <summary>
    /// 驗證相同來源記錄識別 (EventRecordID) 之重複重播事件能被冪等篩選器正確阻絕，且不產生任何防護動作。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenDuplicateSourceRecordIdReplayed_RejectsAsDuplicate()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);

        SecurityObservationEvent evt1 = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 123456L,
            NormalizedIpAddress = "192.168.1.50",
            NormalizedAccount = "admin",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        SecurityObservationEvent evt2 = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 123456L,
            NormalizedIpAddress = "192.168.1.50",
            NormalizedAccount = "admin",
            EventTimeUtc = evt1.EventTimeUtc
        };

        CorrelationEvaluationResult result1 = engine.Ingest(evt1, config);
        CorrelationEvaluationResult result2 = engine.Ingest(evt2, config);

        Assert.IsFalse(result1.IsDuplicateReplay);
        Assert.IsTrue(result2.IsDuplicateReplay);
        Assert.AreEqual(CorrelationAction.None, result2.Action);
    }

    /// <summary>
    /// 驗證相異事件記錄識別之不同事件不會被錯誤判定為重複，且皆能正常被引擎接受。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenDistinctEventsIngested_AcceptsBothIndependently()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);

        SecurityObservationEvent evt1 = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 1001L,
            NormalizedIpAddress = "192.168.1.50",
            NormalizedAccount = "user1",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        SecurityObservationEvent evt2 = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 1002L,
            NormalizedIpAddress = "192.168.1.50",
            NormalizedAccount = "user2",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        CorrelationEvaluationResult result1 = engine.Ingest(evt1, config);
        CorrelationEvaluationResult result2 = engine.Ingest(evt2, config);

        Assert.IsFalse(result1.IsDuplicateReplay);
        Assert.IsFalse(result2.IsDuplicateReplay);
        Assert.AreNotEqual(evt1.ComputeIdempotencyKey(), evt2.ComputeIdempotencyKey());
    }

    /// <summary>
    /// 驗證多來源相似事件（如 4625 與 4771）僅建立關聯群組，而不刪除或合併原始事件。
    /// </summary>
    [TestMethod]
    public void Ingest_MultiSourceEvents_AssignsCorrelationGroupWithoutDeletingOriginals()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        Guid sharedGroupId = Guid.NewGuid();

        SecurityObservationEvent winAuthEvent = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 2001L,
            NormalizedIpAddress = "10.0.0.15",
            NormalizedAccount = "victim_user",
            CorrelationGroupId = sharedGroupId,
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        SecurityObservationEvent kerberosEvent = new()
        {
            SourceAgentName = "KerberosAgent",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 2002L,
            NormalizedIpAddress = "10.0.0.15",
            NormalizedAccount = "victim_user",
            CorrelationGroupId = sharedGroupId,
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        CorrelationEvaluationResult result1 = engine.Ingest(winAuthEvent, config);
        CorrelationEvaluationResult result2 = engine.Ingest(kerberosEvent, config);

        Assert.IsFalse(result1.IsDuplicateReplay);
        Assert.IsFalse(result2.IsDuplicateReplay);
        Assert.AreEqual(sharedGroupId, result1.CorrelationGroupId);
        Assert.AreEqual(sharedGroupId, result2.CorrelationGroupId);
    }

    /// <summary>
    /// 驗證當功能旗標 <c>EnableCrossAgentCorrelation</c> 為關閉時，引擎傳回無動作且不進行噴灑分析，確保既有行為零差異。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenFeatureFlagDisabled_ReturnsNoneAndZeroRegression()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: false);

        SecurityObservationEvent evt = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 3001L,
            NormalizedIpAddress = "192.168.1.100",
            NormalizedAccount = "target",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        CorrelationEvaluationResult result = engine.Ingest(evt, config);

        Assert.AreEqual(CorrelationAction.None, result.Action);
        Assert.AreEqual(SprayAttackType.None, result.SprayType);
    }

    /// <summary>
    /// 驗證安全網路全域白名單（含 IPv4、IPv6 及 CIDR）之來源 IP 能被正確豁免而不觸發警示。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenSourceIpInSafeNetworks_MarksAsExempted()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        List<IddsConfig.CSafeNetwork> safeList =
        [
            new IddsConfig.CSafeNetwork("10.0.0.0", "255.0.0.0"),
            new IddsConfig.CSafeNetwork("192.168.100.1", "255.255.255.255"),
            new IddsConfig.CSafeNetwork("2001:db8::", "32")
        ];
        IddsConfig config = CreateTestConfig(enableCorrelation: true, safeNetworks: safeList);

        SecurityObservationEvent ipv4CidrEvent = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 4001L,
            NormalizedIpAddress = "10.50.1.20",
            NormalizedAccount = "userA",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        SecurityObservationEvent ipv6CidrEvent = new()
        {
            SourceAgentName = "WindowsAuthentication",
            ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
            ComputerName = "DC01.corp.local",
            SourceEventRecordId = 4002L,
            NormalizedIpAddress = "2001:db8::1234",
            NormalizedAccount = "userB",
            EventTimeUtc = DateTimeOffset.UtcNow
        };

        CorrelationEvaluationResult result1 = engine.Ingest(ipv4CidrEvent, config);
        CorrelationEvaluationResult result2 = engine.Ingest(ipv6CidrEvent, config);

        Assert.IsTrue(result1.IsSafeNetworkExempted);
        Assert.AreEqual(CorrelationAction.None, result1.Action);
        Assert.IsTrue(result2.IsSafeNetworkExempted);
        Assert.AreEqual(CorrelationAction.None, result2.Action);
    }

    /// <summary>
    /// 驗證單一來源 IP 對多個帳號之傳統密碼噴灑 (1-to-N) 達到門檻時發出警示，且嚴格遵守 Phase 0 規範僅評分警示而不自動封鎖。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenOneIpAttacksMultipleAccounts_EmitsAlertAndScoreOnly()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        config.CrossAgentSprayAccountThreshold = 3;

        string attackerIp = "203.0.113.50";
        CorrelationEvaluationResult lastResult = null!;

        for (int i = 1; i <= 3; i++)
        {
            SecurityObservationEvent evt = new()
            {
                SourceAgentName = "WindowsAuthentication",
                ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
                ComputerName = "DC01.corp.local",
                SourceEventRecordId = 5000L + i,
                NormalizedIpAddress = attackerIp,
                NormalizedAccount = $"victim_account_{i}",
                EventTimeUtc = DateTimeOffset.UtcNow
            };
            lastResult = engine.Ingest(evt, config);
        }

        Assert.IsNotNull(lastResult);
        Assert.AreEqual(SprayAttackType.OneIpToMultipleAccounts, lastResult.SprayType);
        Assert.AreEqual(CorrelationAction.AlertAndScoreOnly, lastResult.Action);
        Assert.AreEqual(3, lastResult.AssociatedAccounts.Count);
    }

    [TestMethod]
    public void Ingest_EquivalentIpv4RepresentationsShareOneSprayBucket()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        config.CrossAgentSprayAccountThreshold = 2;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CorrelationEvaluationResult first = engine.Ingest(new SecurityObservationEvent
        {
            SourceAgentName = "AgentA",
            ProviderOrChannel = "Security",
            SourceEventRecordId = 1,
            NormalizedIpAddress = "::ffff:192.0.2.40",
            NormalizedAccount = "account-a",
            EventTimeUtc = now
        }, config);
        CorrelationEvaluationResult second = engine.Ingest(new SecurityObservationEvent
        {
            SourceAgentName = "AgentA",
            ProviderOrChannel = "Security",
            SourceEventRecordId = 2,
            NormalizedIpAddress = "192.0.2.40",
            NormalizedAccount = "account-b",
            EventTimeUtc = now.AddSeconds(1)
        }, config);

        Assert.AreEqual(CorrelationAction.None, first.Action);
        Assert.AreEqual(CorrelationAction.AlertAndScoreOnly, second.Action);
        Assert.AreEqual(SprayAttackType.OneIpToMultipleAccounts, second.SprayType);
        CollectionAssert.AreEquivalent(new[] { "ACCOUNT-A", "ACCOUNT-B" }, second.AssociatedAccounts.ToArray());
    }

    /// <summary>
    /// 驗證多個分散來源 IP 針對單一目標帳號之分散式密碼噴灑 (N-to-1) 達到門檻時發出警示，且絕不自動封鎖所有涉及的 IP。
    /// </summary>
    [TestMethod]
    public void Ingest_WhenMultipleIpsAttackSingleAccount_EmitsAlertAndScoreOnlyWithoutMassBlocking()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        IddsConfig config = CreateTestConfig(enableCorrelation: true);
        config.CrossAgentSprayIpThreshold = 3;

        string targetAccount = "ceo_account";
        CorrelationEvaluationResult lastResult = null!;

        for (int i = 1; i <= 3; i++)
        {
            SecurityObservationEvent evt = new()
            {
                SourceAgentName = "WindowsAuthentication",
                ProviderOrChannel = "Microsoft-Windows-Security-Auditing",
                ComputerName = "DC01.corp.local",
                SourceEventRecordId = 6000L + i,
                NormalizedIpAddress = $"198.51.100.{i}",
                NormalizedAccount = targetAccount,
                EventTimeUtc = DateTimeOffset.UtcNow
            };
            lastResult = engine.Ingest(evt, config);
        }

        Assert.IsNotNull(lastResult);
        Assert.AreEqual(SprayAttackType.MultipleIpsToOneAccount, lastResult.SprayType);
        Assert.AreEqual(CorrelationAction.AlertAndScoreOnly, lastResult.Action);
        Assert.AreEqual(3, lastResult.AssociatedIps.Count);
    }

    /// <summary>
    /// 驗證 <see cref="ConcurrentSlidingBucket{TKey, TItem}"/> 在多執行緒高並行存取下具備執行緒安全性且無資料競爭。
    /// </summary>
    [TestMethod]
    public async Task ConcurrentSlidingBucket_HighConcurrency_ThreadSafe()
    {
        ConcurrentSlidingBucket<string, int> bucket = new(maxKeyCapacity: 1000, maxItemsPerBucket: 500, windowDuration: TimeSpan.FromMinutes(5));
        const int threadCount = 8;
        const int itemsPerThread = 100;

        Task[] tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < itemsPerThread; i++)
            {
                bucket.Add($"key_{threadId % 4}", threadId * 1000 + i);
            }
        })).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        Assert.AreEqual(threadCount * itemsPerThread, bucket.TotalItemsAdded);
        Assert.AreEqual(4, bucket.ActiveKeyCount);
    }

    /// <summary>
    /// 驗證滑動時間窗過期機制能正確修剪超過時間窗之歷史項目。
    /// </summary>
    [TestMethod]
    public void ConcurrentSlidingBucket_SlidingWindowExpiry_PrunesOldItems()
    {
        TimeSpan window = TimeSpan.FromMinutes(5);
        ConcurrentSlidingBucket<string, string> bucket = new(windowDuration: window);
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        bucket.Add("test_key", "old_item", baseTime.Subtract(TimeSpan.FromMinutes(10)));
        bucket.Add("test_key", "new_item", baseTime);

        IReadOnlyList<string> activeItems = bucket.GetActiveItems("test_key");
        Assert.AreEqual(1, activeItems.Count);
        Assert.AreEqual("new_item", activeItems[0]);
    }

    /// <summary>
    /// 驗證延遲抵達的舊事件即使排在新事件之後，仍會被滑動時間窗正確移除。
    /// </summary>
    [TestMethod]
    public void ConcurrentSlidingBucket_OutOfOrderArrival_PrunesExpiredItemAnywhere()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ConcurrentSlidingBucket<string, string> bucket = new(windowDuration: TimeSpan.FromMinutes(5));

        bucket.Add("account", "new", now);
        bucket.Add("account", "late-old", now.AddMinutes(-10));

        CollectionAssert.AreEqual(new[] { "new" }, bucket.GetActiveItems("account").ToArray());
    }

    /// <summary>
    /// 驗證來源鍵洪水超過上限時會驅逐最久未使用的鍵，不得突破容量硬上限。
    /// </summary>
    [TestMethod]
    public void ConcurrentSlidingBucket_KeyFlood_EnforcesHardCapacity()
    {
        ConcurrentSlidingBucket<string, int> bucket = new(maxKeyCapacity: 3, windowDuration: TimeSpan.FromMinutes(5));

        for (int index = 0; index < 20; index++)
        {
            bucket.Add($"key-{index}", index);
            Assert.IsTrue(bucket.ActiveKeyCount <= 3, "活動聚合鍵數不得在任何時點突破硬上限");
        }

        Assert.AreEqual(3, bucket.ActiveKeyCount);
        Assert.IsTrue(bucket.TotalEvictions >= 17);
    }

    /// <summary>
    /// 驗證高併發寫入與 LRU 驅逐共存時，每筆加入事件皆可由活動項目或驅逐計數完整對帳。
    /// </summary>
    [TestMethod]
    public async Task ConcurrentSlidingBucket_ConcurrentFlood_DoesNotLoseOrphanedWrites()
    {
        ConcurrentSlidingBucket<string, int> bucket = new(maxKeyCapacity: 8, maxItemsPerBucket: 2000, windowDuration: TimeSpan.FromHours(1));

        Task[] tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (int index = 0; index < 250; index++)
            {
                bucket.Add($"key-{(worker * 250 + index) % 32}", index);
            }
        })).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);

        long activeItems = bucket.SnapshotCounts().Values.Sum(static count => (long)count);
        Assert.AreEqual(bucket.TotalItemsAdded, activeItems + bucket.TotalEvictions);
        Assert.IsTrue(bucket.ActiveKeyCount <= 8);
    }

    /// <summary>
    /// 驗證快照與過期清理和既有鍵寫入同時執行時，內部非執行緒安全清單仍受相同貯列鎖完整保護。
    /// </summary>
    [TestMethod]
    public async Task ConcurrentSlidingBucket_SnapshotAndPruneDuringWrites_RemainsConsistent()
    {
        ConcurrentSlidingBucket<string, int> bucket = new(maxKeyCapacity: 16, maxItemsPerBucket: 10000, windowDuration: TimeSpan.FromHours(1));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Task[] writers = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (int index = 0; index < 1000; index++)
            {
                bucket.Add($"key-{index % 8}", (worker * 1000) + index, now.AddTicks(index));
            }
        })).ToArray();
        Task[] observers = Enumerable.Range(0, 4).Select(observer => Task.Run(() =>
        {
            for (int index = 0; index < 500; index++)
            {
                _ = bucket.SnapshotCounts();
                bucket.PruneExpiredKeys(now.AddHours(-1));
            }
        })).ToArray();

        await Task.WhenAll(writers.Concat(observers)).ConfigureAwait(false);

        long activeItems = bucket.SnapshotCounts().Values.Sum(static count => (long)count);
        Assert.AreEqual(bucket.TotalItemsAdded, activeItems + bucket.TotalEvictions);
        Assert.AreEqual(8, bucket.ActiveKeyCount);
    }

    /// <summary>
    /// 驗證水位點復原機制可正確恢復各 Agent 與通道之處理狀態。
    /// </summary>
    [TestMethod]
    public void RestoreWatermark_RestoresStateCorrectly()
    {
        CrossAgentCorrelationEngine engine = new(TimeSpan.FromMinutes(10));
        ObservationWatermark watermark = new("WindowsAuthentication", "Security", 99999L, DateTimeOffset.UtcNow);

        engine.RestoreWatermark(watermark);

        Assert.IsTrue(engine.Watermarks.ContainsKey("WindowsAuthentication|Security"));
        Assert.AreEqual(99999L, engine.Watermarks["WindowsAuthentication|Security"].LastEventRecordId);
    }

    private static IddsConfig CreateTestConfig(bool enableCorrelation, List<IddsConfig.CSafeNetwork>? safeNetworks = null)
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Database db = new();
        db.Configure(directory);
        IddsConfig config = new(db);
        config.EnableCrossAgentCorrelation = enableCorrelation;
        config.UseSafeNetworkList = safeNetworks != null && safeNetworks.Count > 0;
        config.SafeNetworks = new IddsConfig.CSafeNetworks();
        if (safeNetworks != null)
        {
            config.SafeNetworks.AddRange(safeNetworks);
        }
        return config;
    }
}
