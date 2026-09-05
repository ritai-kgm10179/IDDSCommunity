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
            var first = store.ReadPage(0, string.Empty);
            Assert.AreEqual(256, first.ActiveThreats.Count);
            Assert.IsTrue(first.HasMore);
            var restarted = new ThreatHubStore(database);
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
}