using System.Net;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 GeoIpLookupService 之國家查詢與國家封鎖比對。
/// </summary>
[TestClass]
public sealed class GeoIpLookupServiceTest
{
    [TestInitialize]
    public void Setup()
    {
        GeoIpLookupService.Clear();
    }

    [TestCleanup]
    public void Cleanup()
    {
        GeoIpLookupService.Clear();
    }

    /// <summary>
    /// 驗證從 CSV 載入 IPv4 與 IPv6 國家資料並正確查詢。
    /// </summary>
    [TestMethod]
    public void LoadFromCsv_ParsesAndMatchesIpToCountry()
    {
        string csv = """
            # Sample GeoIP Database
            1.1.1.0/24,AU,Australia
            140.112.0.0/16,TW,Taiwan
            2001:db8:cafe::/48,JP,Japan
            """;

        int loaded = GeoIpLookupService.LoadFromCsv(csv);
        Assert.AreEqual(3, loaded);

        // Test Taiwan IP
        bool foundTw = GeoIpLookupService.TryLookup(IPAddress.Parse("140.112.1.1"), out string twCode, out string twName);
        Assert.IsTrue(foundTw);
        Assert.AreEqual("TW", twCode);
        Assert.AreEqual("Taiwan", twName);

        // Test Australia IP
        bool foundAu = GeoIpLookupService.TryLookup(IPAddress.Parse("1.1.1.1"), out string auCode, out string auName);
        Assert.IsTrue(foundAu);
        Assert.AreEqual("AU", auCode);
        Assert.AreEqual("Australia", auName);

        // Test Japan IPv6
        bool foundJp = GeoIpLookupService.TryLookup(IPAddress.Parse("2001:db8:cafe::1234"), out string jpCode, out string jpName);
        Assert.IsTrue(foundJp);
        Assert.AreEqual("JP", jpCode);
        Assert.AreEqual("Japan", jpName);

        // Test Unknown IP
        bool foundUnk = GeoIpLookupService.TryLookup(IPAddress.Parse("8.8.8.8"), out string unkCode, out string unkName);
        Assert.IsFalse(foundUnk);
        Assert.AreEqual("ZZ", unkCode);
        Assert.AreEqual("Unknown", unkName);
    }

    /// <summary>
    /// 驗證 IsCountryBlocked 正確比對國家封鎖清單。
    /// </summary>
    [TestMethod]
    public void IsCountryBlocked_MatchesBlockedList()
    {
        string csv = """
            198.51.100.0/24,RU,Russian Federation
            203.0.113.0/24,CN,China
            """;

        GeoIpLookupService.LoadFromCsv(csv);

        string[] blockedCountries = ["RU", "KP"];

        Assert.IsTrue(GeoIpLookupService.IsCountryBlocked(IPAddress.Parse("198.51.100.55"), blockedCountries));
        Assert.IsFalse(GeoIpLookupService.IsCountryBlocked(IPAddress.Parse("203.0.113.88"), blockedCountries)); // CN is not in list
        Assert.IsFalse(GeoIpLookupService.IsCountryBlocked(IPAddress.Parse("127.0.0.1"), blockedCountries));
    }
}
