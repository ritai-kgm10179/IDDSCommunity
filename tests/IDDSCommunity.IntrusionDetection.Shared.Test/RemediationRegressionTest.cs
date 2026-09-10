using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using IDDSCommunity.IntrusionDetection.Shared.Security;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證審查修正的邊界、安全及容量行為。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RemediationRegressionTest
{
    /// <summary>
    /// IPv6 權杖可往返且拒絕竄改、錯誤動作及空白密鑰。
    /// </summary>
    [TestMethod]
    public void ActionTokensRequireKeyAndPreserveIpv6()
    {
        const string secret = "unit-test-only-random-secret";
        string token = ActionTokenService.GenerateToken("unblock", "2001:db8::1234", secretKey: secret);
        Assert.IsTrue(ActionTokenService.ValidateToken(token, "unblock", out string ip, secret));
        Assert.AreEqual("2001:db8::1234", ip);
        Assert.IsFalse(ActionTokenService.ValidateToken(token, "block", out _, secret));
        Assert.IsFalse(ActionTokenService.ValidateToken(token.Replace("1234", "1235"), "unblock", out _, secret));
        Assert.IsFalse(ActionTokenService.ValidateToken(token, "unblock", out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => ActionTokenService.GenerateToken("block", "8.8.8.8"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ActionTokenService.GenerateToken("block", "8.8.8.8", 16, secret));
    }

    /// <summary>
    /// 驗證 ActionToken 經 ValidateAndBurnToken 驗證成功後立即銷毀，無法被重複執行（Burn-on-use）。
    /// </summary>
    [TestMethod]
    public void ActionToken_ValidateAndBurn_CannotBeReplayed()
    {
        ActionTokenService.ResetConsumedTokensForTesting();
        const string secret = "unit-test-burn-secret-key-12345";
        string token = ActionTokenService.GenerateToken("unblock", "140.112.1.1", ttlMinutes: 10, secretKey: secret);

        // 第一次驗證並消耗應成功
        Assert.IsTrue(ActionTokenService.ValidateAndBurnToken(token, "unblock", out string ip, secret));
        Assert.AreEqual("140.112.1.1", ip);

        // 第二次重放調用應被拒絕（已消耗）
        Assert.IsFalse(ActionTokenService.ValidateAndBurnToken(token, "unblock", out string replayedIp, secret));
        Assert.IsTrue(string.IsNullOrEmpty(replayedIp));
    }

    /// <summary>
    /// 大量不同帳號不會飽和於二十餘個，且時間窗結束後重設多樣性。
    /// </summary>
    [TestMethod]
    public void CardinalityAndWindowAreCorrect()
    {
        var detector = new SlowAndLowAttackDetector(anomalyThreshold: 0);
        int estimate = 0;
        detector.SlowAndLowAttackDetected += (_, _, accounts, _) => estimate = accounts;
        DateTime now = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++) detector.RecordEvent("8.8.8.8", $"account-{i}", "test", now);
        Assert.IsTrue(estimate > 500 && estimate < 2000, $"Observed estimate: {estimate}");
        detector.RecordEvent("8.8.8.8", "single", "test", now.AddHours(73));
        Assert.AreEqual(1, estimate);
    }

    /// <summary>
    /// 並行新增來源受硬容量限制，較新來源仍可正常追蹤。
    /// </summary>
    [TestMethod]
    public void ConcurrentTrackingRemainsBounded()
    {
        const int capacity = 32;
        var detector = new SlowAndLowAttackDetector(maxCapacity: capacity);
        DateTime now = DateTime.UtcNow;
        Parallel.For(0, 4000, i => detector.RecordEvent($"source-{i}", "user", "test", now));
        int tracked = 0;
        for (int i = 0; i < 4000; i++) if (detector.GetCurrentScore($"source-{i}", now) > 0) tracked++;
        Assert.AreEqual(capacity, tracked);
        detector.RecordEvent("new", "user", "test", now);
        Assert.IsTrue(detector.GetCurrentScore("new", now) > 0);
    }

    /// <summary>
    /// 索引保留輸入優先順序、完整 IPv6 與最大端點。
    /// </summary>
    [TestMethod]
    public void GeoIpIndexPreservesOverlapAndEndpoints()
    {
        try
        {
            GeoIpLookupService.LoadFromCsv("1.1.1.64,1.1.1.127,TW,Taiwan\n1.1.1.0/24,US,United States\n::/0,ZZ,Unknown\n255.255.255.255,255.255.255.255,AU,Australia");
            Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("1.1.1.100"), out string overlap, out _));
            Assert.AreEqual("TW", overlap);
            GeoIpLookupService.TryLookup(IPAddress.Parse("1.1.1.128"), out string after, out _);
            Assert.AreEqual("US", after);
            GeoIpLookupService.TryLookup(IPAddress.Broadcast, out string last, out _);
            Assert.AreEqual("AU", last);
            Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.IPv6Any, out _, out _));
            Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"), out _, out _));
            Assert.AreEqual(4, GeoIpLookupService.TotalLoadedRecords);
        }
        finally { GeoIpLookupService.Clear(); }
    }

    /// <summary>
    /// 動態前綴快照支援零長度、主機前綴與原子清除。
    /// </summary>
    [TestMethod]
    public void BogonPrefixIndexMatchesBoundaries()
    {
        try
        {
            BogonIpFilter.UpdateDynamicBogons([IPNetwork.Parse("8.8.8.0/24"), IPNetwork.Parse("2001:4860::/32")]);
            Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("8.8.8.255"));
            Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("8.8.9.0"));
            Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("2001:4860::8888"));
            BogonIpFilter.UpdateDynamicBogons([IPNetwork.Parse("0.0.0.0/0")]);
            Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("1.1.1.1"));
            BogonIpFilter.ClearDynamicBogons();
            Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("1.1.1.1"));
        }
        finally { BogonIpFilter.ClearDynamicBogons(); }
    }

    /// <summary>
    /// 已知與未知長度內容皆遵循位元組上限。
    /// </summary>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    public async Task DownloadLimitRejectsOversizedContent()
    {
        using var exact = new ByteArrayContent(new byte[32]);
        Assert.AreEqual(32, (await BoundedHttpContent.ReadAsync(exact, 32)).Length);
        using var large = new StreamContent(new MemoryStream(new byte[33]));
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => BoundedHttpContent.ReadAsync(large, 32));
    }
}