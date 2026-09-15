using System;
using System.IO;
using System.Linq;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證中繼資料持久化、到期及游標頁面邊界。
/// </summary>
[TestClass]
public sealed class ThreatHubStoreTest
{
    /// <summary>
    /// 重啟後保留世代與序號；重送不產生重複序號，過期項目不回傳。
    /// </summary>
    [TestMethod]
    public void DurablePagesSurviveRestartAndExcludeExpiredEntries()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDS-Hub-Test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var database = new Database();
        try
        {
            database.Configure(directory, "hub.db");
            var store = new ThreatHubStore(database);
            for (int i = 0; i < 300; i++)
                Assert.IsTrue(store.Upsert(new ThreatIntelligenceItem { SourceIp = $"8.8.{i / 256}.{i % 256}", ExpiresUtc = DateTime.UtcNow.AddHours(1) }));
            Assert.AreEqual(300, store.ActiveThreatCount);
            var first = store.ReadPage(0, string.Empty);
            Assert.AreEqual(256, first.ActiveThreats.Count);
            Assert.IsTrue(first.HasMore);
            var restarted = new ThreatHubStore(database);
            Assert.AreEqual(300, restarted.ActiveThreatCount);
            var second = restarted.ReadPage(first.NextCursor, first.Generation);
            Assert.AreEqual(44, second.ActiveThreats.Count);
            Assert.IsFalse(second.HasMore);
            Assert.AreEqual(first.Generation, second.Generation);
            Assert.IsTrue(restarted.Upsert(second.ActiveThreats.Last()));
            Assert.AreEqual(0, restarted.ReadPage(second.NextCursor, second.Generation).ActiveThreats.Count);
            Assert.IsTrue(restarted.Upsert(new ThreatIntelligenceItem { SourceIp = "1.1.1.1", ExpiresUtc = DateTime.UtcNow.AddSeconds(-1) }));
            Assert.AreEqual(0, restarted.ReadPage(second.NextCursor, second.Generation).ActiveThreats.Count);
            Assert.AreEqual(256, restarted.ReadPage(long.MaxValue, "different-database").ActiveThreats.Count);
        }
        finally
        {
            database.Close();
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Hub 入口拒絕低信心、過期、Bogon 與安全網路；限制永久情資的交換期限。
    /// </summary>
    [TestMethod]
    public void HubRejectsUnsafeSourcesAndBoundsPermanentExpiry()
    {
        var config = IddsConfig.GetDefaultConfiguration();
        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        Assert.ThrowsExactly<InvalidOperationException>(() => server.IngestLocalThreat(new ThreatIntelligenceItem { SourceIp = "127.0.0.1" }));
        Assert.ThrowsExactly<InvalidOperationException>(() => server.IngestLocalThreat(new ThreatIntelligenceItem { SourceIp = "8.8.8.8", ConfidenceScore = double.NaN }));
        Assert.ThrowsExactly<InvalidOperationException>(() => server.IngestLocalThreat(new ThreatIntelligenceItem { SourceIp = "8.8.8.8", ExpiresUtc = DateTime.UtcNow.AddDays(-1) }));
        server.IngestLocalThreat(new ThreatIntelligenceItem { SourceIp = "8.8.8.8" });
        Assert.AreEqual(1, server.ActiveThreats.Count);
        Assert.IsTrue(server.ActiveThreats[0].ExpiresUtc <= DateTime.UtcNow.AddDays(Math.Clamp(config.ThreatFeedTtlDays, 1, 365)));
    }

    /// <summary>
    /// 驗證 UpsertBatch 能正確批次處理新增、更新與重複資料之情資項目。
    /// </summary>
    [TestMethod]
    public void UpsertBatch_HandlesInsertAndUpdate_Efficiently()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDS-Hub-BatchTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var database = new Database();
        try
        {
            database.Configure(directory, "hub_batch.db");
            var store = new ThreatHubStore(database);

            var batch1 = Enumerable.Range(1, 100).Select(i => new ThreatIntelligenceItem
            {
                SourceIp = $"198.51.100.{i}",
                ThreatCategory = "SCANNER",
                ConfidenceScore = 0.9,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            }).ToList();

            int inserted = store.UpsertBatch(batch1);
            Assert.AreEqual(100, inserted);
            Assert.AreEqual(100, store.ActiveThreatCount);
            ThreatHubSyncResponse initialPage = store.ReadPage(0, string.Empty);

            // 重複送出相同資料應被跳過（不觸發無效寫入）
            int skipped = store.UpsertBatch(batch1);
            Assert.AreEqual(0, skipped);
            Assert.AreEqual(100, store.ActiveThreatCount);

            // 包含 50 筆更新 + 50 筆全新項目
            var batch2 = Enumerable.Range(51, 100).Select(i => new ThreatIntelligenceItem
            {
                SourceIp = $"198.51.100.{i}",
                ThreatCategory = "BRUTE_FORCE",
                ConfidenceScore = 1.0,
                ExpiresUtc = DateTime.UtcNow.AddHours(4)
            }).ToList();

            int updatedAndInserted = store.UpsertBatch(batch2);
            Assert.AreEqual(100, updatedAndInserted);
            Assert.AreEqual(150, store.ActiveThreatCount);

            var updatedItem = store.LookupThreat("198.51.100.60");
            Assert.IsNotNull(updatedItem);
            Assert.AreEqual("BRUTE_FORCE", updatedItem.ThreatCategory);

            ThreatHubSyncResponse changedPage = store.ReadPage(initialPage.NextCursor, initialPage.Generation);
            Assert.AreEqual(100, changedPage.ActiveThreats.Count);
            Assert.AreEqual(100, changedPage.ActiveThreats.Count(item => item.ThreatCategory == "BRUTE_FORCE"));

            long settledCursor = changedPage.NextCursor;
            Assert.AreEqual(0, store.UpsertBatch(batch2));
            Assert.AreEqual(0, store.ReadPage(settledCursor, changedPage.Generation).ActiveThreats.Count);
        }
        finally
        {
            database.Close();
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// 驗證異動日誌正確記錄封鎖、假釋與撤銷墓碑事件，並支援增量查詢與狀態收斂。
    /// </summary>
    [TestMethod]
    public void Journal_RecordsEvents_And_SupportsDeltaQueryAndRevocation()
    {
        // 1. 驗證記憶體模式
        var memoryStore = new ThreatHubStore();
        Assert.IsTrue(memoryStore.Upsert(new ThreatIntelligenceItem { SourceIp = "203.0.113.10", ExpiresUtc = DateTime.UtcNow.AddHours(1) }));
        Assert.IsTrue(memoryStore.RecordProbation("203.0.113.20", "Probation check"));
        Assert.IsTrue(memoryStore.Revoke("203.0.113.10", "False positive unblocked"));

        var memoryDeltas = memoryStore.ReadJournal(0, 10);
        Assert.AreEqual(3, memoryDeltas.Count);
        Assert.AreEqual(ThreatHubJournalEventType.Blocked, memoryDeltas[0].EventType);
        Assert.AreEqual("203.0.113.10", memoryDeltas[0].SourceIp);
        Assert.AreEqual(ThreatHubJournalEventType.Probation, memoryDeltas[1].EventType);
        Assert.AreEqual("203.0.113.20", memoryDeltas[1].SourceIp);
        Assert.AreEqual(ThreatHubJournalEventType.Revoked, memoryDeltas[2].EventType);
        Assert.AreEqual("203.0.113.10", memoryDeltas[2].SourceIp);

        // 撤銷後活躍威脅庫中不應存在該 IP
        Assert.IsNull(memoryStore.LookupThreat("203.0.113.10"));

        // 2. 驗證 SQLite 持久化模式
        string directory = Path.Combine(Path.GetTempPath(), "IDDS-Hub-JournalTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var database = new Database();
        try
        {
            database.Configure(directory, "hub_journal.db");
            var durableStore = new ThreatHubStore(database);

            Assert.IsTrue(durableStore.Upsert(new ThreatIntelligenceItem { SourceIp = "192.0.2.1", ExpiresUtc = DateTime.UtcNow.AddHours(2) }));
            Assert.IsTrue(durableStore.RecordProbation("192.0.2.2", "Probation active"));
            Assert.IsTrue(durableStore.Revoke("192.0.2.1", "Admin revoked"));

            var deltas = durableStore.ReadJournal(0, 10);
            Assert.AreEqual(3, deltas.Count);
            Assert.AreEqual(ThreatHubJournalEventType.Blocked, deltas[0].EventType);
            Assert.AreEqual("192.0.2.1", deltas[0].SourceIp);
            Assert.AreEqual(ThreatHubJournalEventType.Probation, deltas[1].EventType);
            Assert.AreEqual("192.0.2.2", deltas[1].SourceIp);
            Assert.AreEqual(ThreatHubJournalEventType.Revoked, deltas[2].EventType);
            Assert.AreEqual("192.0.2.1", deltas[2].SourceIp);

            // 游標分頁測試
            long cursor1 = deltas[1].Sequence;
            var nextDeltas = durableStore.ReadJournal(cursor1, 10);
            Assert.AreEqual(1, nextDeltas.Count);
            Assert.AreEqual(deltas[2].Sequence, nextDeltas[0].Sequence);

            // 撤銷後從活躍資料表中完全移除
            Assert.IsNull(durableStore.LookupThreat("192.0.2.1"));
        }
        finally
        {
            database.Close();
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// 驗證 Revoke 與 RecordProbation 輸入無效 IP 時能優雅傳回 false，不拋出 FormatException。
    /// </summary>
    [TestMethod]
    public void Revoke_And_RecordProbation_InvalidIpReturnsFalse()
    {
        var store = new ThreatHubStore();
        Assert.IsFalse(store.Revoke(""));
        Assert.IsFalse(store.Revoke("   "));
        Assert.IsFalse(store.Revoke("not-an-ip"));
        Assert.IsFalse(store.RecordProbation(""));
        Assert.IsFalse(store.RecordProbation("invalid-ip-format"));
    }
}
