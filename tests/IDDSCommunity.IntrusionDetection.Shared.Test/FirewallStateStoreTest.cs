using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class FirewallStateStoreTest
{
    private string directory = null!;
    private Database database = null!;
    private FirewallStateStore store = null!;

    /// <summary>
    /// 建立隔離的加密測試資料庫。
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        directory = Path.Combine(Path.GetTempPath(), "IDDSCommunity.FirewallStateStoreTests", Guid.NewGuid().ToString("N"));
        database = new Database();
        database.Configure(directory);
        store = new FirewallStateStore(database);
    }

    /// <summary>
    /// 關閉資料庫並移除測試資料。
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    /// <summary>
    /// 驗證來源擁有權、版本防倒退、複合 keyset 游標與精確移除。
    /// </summary>
    [TestMethod]
    public void DesiredAddresses_PreserveSourceOwnershipAndRevision()
    {
        string changed = DateTimeOffset.UtcNow.ToString("O");
        FirewallDesiredAddress first = new("192.0.2.1/32", 2, [192, 0, 2, 1], 32, 1, "feed-a", 2, 10, "b010-0", changed);
        FirewallDesiredAddress secondSource = first with { Source = "feed-b", Revision = 1 };
        Assert.AreEqual(2, store.UpsertDesiredAddresses([first, secondSource]));
        Assert.AreEqual(1, store.UpsertDesiredAddresses([first with { DesiredState = 0, Revision = 1 }]));

        var firstPage = store.ReadDesiredAddressPage(string.Empty, string.Empty, 1);
        Assert.AreEqual("feed-a", firstPage[0].Source);
        Assert.AreEqual(1, firstPage[0].DesiredState);
        var secondPage = store.ReadDesiredAddressPage(firstPage[0].AddressKey, firstPage[0].Source, 1);
        Assert.AreEqual("feed-b", secondPage[0].Source);
        var effective = store.ReadEffectiveDesiredAddressPage(string.Empty, 10).Single();
        Assert.AreEqual(2, effective.SourceCount);
        Assert.AreEqual(2L, effective.Revision);

        Assert.AreEqual(1, store.DeleteDesiredAddresses([new FirewallDesiredAddressKey(first.AddressKey, "feed-a")]));
        Assert.AreEqual("feed-b", store.ReadDesiredAddressPage(string.Empty, string.Empty, 10).Single().Source);
        Assert.AreEqual(1, store.ReadEffectiveDesiredAddressPage(string.Empty, 10).Single().SourceCount);
    }

    /// <summary>
    /// 驗證異動日誌序號分頁、部分完成與失敗摘要。
    /// </summary>
    [TestMethod]
    public void ChangeJournal_SupportsKeysetAndPartialOutcomes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var sequences = store.AppendChanges([
            new FirewallChangeRequest(10, "192.0.2.10/32", FirewallChangeOperation.Upsert, 1, now),
            new FirewallChangeRequest(11, "192.0.2.11/32", FirewallChangeOperation.Remove, 2, now)]);

        var firstPage = store.ReadPendingChangesPage(0, 1);
        Assert.AreEqual(sequences[0], firstPage[0].Sequence);
        Assert.AreEqual(sequences[1], store.ReadPendingChangesPage(firstPage[0].Sequence, 1)[0].Sequence);
        Assert.AreEqual(1, store.MarkChanges([sequences[0]], FirewallChangeProcessingState.Completed, null, now));
        Assert.AreEqual(1, store.MarkChanges([sequences[1]], FirewallChangeProcessingState.Failed, "COM failure", now));
        Assert.AreEqual(0, store.ReadPendingChangesPage(0, 10).Count);

        Assert.AreEqual(1L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM FirewallChangeJournal WHERE ProcessingState=@p0 AND FailureDetails=@p1", (int)FirewallChangeProcessingState.Failed, "COM failure")));
    }

    /// <summary>
    /// 驗證同位址待處理異動會合併、領取具備租約，且失敗重試受最早執行時間控制。
    /// </summary>
    [TestMethod]
    public void ChangeJournal_CoalescesClaimsAndRetriesWithLease()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var oldSequence = store.AppendChanges([new FirewallChangeRequest(1, "192.0.2.20/32", FirewallChangeOperation.Upsert, 1, now)]).Single();
        var latestSequence = store.AppendChanges([new FirewallChangeRequest(1, "192.0.2.20/32", FirewallChangeOperation.Remove, 2, now)]).Single();
        Assert.AreEqual(1, store.ReadPendingChangesPage(0, 10).Count);
        Assert.AreEqual(latestSequence, store.ReadPendingChangesPage(0, 10)[0].Sequence);
        Assert.AreEqual((int)FirewallChangeProcessingState.Completed, Convert.ToInt32(database.ExecuteScalar("SELECT ProcessingState FROM FirewallChangeJournal WHERE Sequence=@p0", oldSequence)));

        var claimed = store.ClaimChanges("worker-a", 10, now, TimeSpan.FromMinutes(1));
        Assert.AreEqual(latestSequence, claimed.Single().Sequence);
        Assert.AreEqual(1, claimed[0].AttemptCount);
        Assert.AreEqual(0, store.ClaimChanges("worker-b", 10, now.AddSeconds(30), TimeSpan.FromMinutes(1)).Count);
        Assert.AreEqual(1, store.ScheduleRetry([latestSequence], "transient", now.AddMinutes(2), now));
        Assert.AreEqual(0, store.ClaimChanges("worker-b", 10, now.AddMinutes(1), TimeSpan.FromMinutes(1)).Count);
        Assert.AreEqual(latestSequence, store.ClaimChanges("worker-b", 10, now.AddMinutes(2), TimeSpan.FromMinutes(1)).Single().Sequence);
    }

    /// <summary>
    /// 驗證寫入批次超過固定上限時整筆回滾，避免無界交易與記憶體成長。
    /// </summary>
    [TestMethod]
    public void DesiredAddresses_RejectUnboundedBatchAtomically()
    {
        FirewallDesiredAddress template = new("192.0.2.1/32", 2, [192, 0, 2, 1], 32, 1, "feed", 1, 1, null, DateTimeOffset.UtcNow.ToString("O"));
        var oversized = Enumerable.Range(0, FirewallStateStore.MaximumBatchSize + 1)
            .Select(index => template with { AddressKey = $"198.51.{index / 256}.{index % 256}/32" });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => store.UpsertDesiredAddresses(oversized));
        Assert.AreEqual(0L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM FirewallDesiredAddress")));
    }

    /// <summary>
    /// 驗證分片 upsert 不接受舊版本覆寫，並支援複合游標與精確刪除。
    /// </summary>
    [TestMethod]
    public void AppliedShards_RejectRevisionRegressionAndSupportPaging()
    {
        FirewallAppliedShard inbound = new(1, "b001-0", "new-hash", 10, 120, 5, "rule-in", null);
        FirewallAppliedShard outbound = new(2, "b001-0", "out-hash", 10, 120, 5, "rule-out", null);
        Assert.AreEqual(2, store.UpsertAppliedShards([inbound, outbound]));
        Assert.AreEqual(1, store.UpsertAppliedShards([inbound with { ContentHash = "old-hash", AppliedRevision = 4 }]));

        var firstPage = store.ReadAppliedShardPage(int.MinValue, string.Empty, 1);
        Assert.AreEqual("new-hash", firstPage[0].ContentHash);
        var secondPage = store.ReadAppliedShardPage(firstPage[0].Direction, firstPage[0].ShardId, 1);
        Assert.AreEqual(2, secondPage[0].Direction);
        Assert.AreEqual(1, store.DeleteAppliedShards([inbound]));
        Assert.AreEqual(2, store.ReadAppliedShardPage(int.MinValue, string.Empty, 10).Single().Direction);
    }
}
