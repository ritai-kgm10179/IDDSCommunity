using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 GeoIpUpdateService 之自動下載、本地檔案載入、快取與熱更新機制。
/// </summary>
[TestClass]
public sealed class GeoIpUpdateServiceTest
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
    /// 驗證遠端下載 IPv4 與 IPv6 之 GeoIP CSV 數據並完成記憶體熱更新。
    /// </summary>
    [TestMethod]
    public async Task RefreshDatabaseAsync_DownloadsAndLoadsGeoIpFeeds()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableGeoIpAutoUpdate = true;
        config.GeoIpDatabaseIpv4Url = "http://localhost/geoip-v4.csv";
        config.GeoIpDatabaseIpv6Url = "http://localhost/geoip-v6.csv";
        config.GeoIpLocalFilePath = string.Empty;

        string mockV4Csv = "140.112.0.0/16,TW,Taiwan\n1.1.1.0/24,AU,Australia\n";
        string mockV6Csv = "2001:db8:cafe::/48,JP,Japan\n";

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            string url = req.RequestUri!.ToString();
            string content = url.Contains("geoip-v4") ? mockV4Csv : mockV6Csv;
            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
            return Task.FromResult(resp);
        });

        using HttpClient httpClient = new(mockHandler);
        using GeoIpUpdateService service = new(config, httpClient: httpClient);

        var result = await service.RefreshDatabaseAsync(isManual: false).ConfigureAwait(false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.TotalRecords);
        Assert.AreEqual(3, result.TotalCountries);

        Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("140.112.1.1"), out string twCode, out _));
        Assert.AreEqual("TW", twCode);

        Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("2001:db8:cafe::1"), out string jpCode, out _));
        Assert.AreEqual("JP", jpCode);
    }

    /// <summary>
    /// 驗證配置本機自訂檔案時優先自本機檔案載入。
    /// </summary>
    [TestMethod]
    public async Task RefreshDatabaseAsync_LoadsFromLocalFileWhenConfigured()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            string localCsv = "203.0.113.0/24,CN,China\n198.51.100.0/24,RU,Russian Federation\n";
            await File.WriteAllTextAsync(tempFile, localCsv).ConfigureAwait(false);

            IddsConfig config = IddsConfig.GetDefaultConfiguration();
            config.GeoIpLocalFilePath = tempFile;

            using GeoIpUpdateService service = new(config);
            var result = await service.RefreshDatabaseAsync(isManual: false).ConfigureAwait(false);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.TotalRecords);
            Assert.AreEqual(2, result.TotalCountries);

            Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("203.0.113.5"), out string cnCode, out _));
            Assert.AreEqual("CN", cnCode);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// 驗證停用自動更新且非手動觸發時不發送下載請求。
    /// </summary>
    [TestMethod]
    public async Task RefreshDatabaseAsync_WhenDisabledAndNotManual_DoesNotDownload()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableGeoIpAutoUpdate = false;
        config.GeoIpLocalFilePath = string.Empty;

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            Assert.Fail("HttpClient should not be invoked when auto-update is disabled.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using HttpClient httpClient = new(mockHandler);
        using GeoIpUpdateService service = new(config, httpClient: httpClient);

        var result = await service.RefreshDatabaseAsync(isManual: false).ConfigureAwait(false);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.TotalRecords);
    }

    /// <summary>
    /// 驗證手動觸發時即使停用自動更新仍可執行下載與更新。
    /// </summary>
    [TestMethod]
    public async Task RefreshDatabaseAsync_WhenManual_DownloadsEvenIfAutoUpdateDisabled()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableGeoIpAutoUpdate = false;
        config.GeoIpDatabaseIpv4Url = "http://localhost/geoip-v4.csv";
        config.GeoIpDatabaseIpv6Url = "http://localhost/geoip-v6.csv";
        config.GeoIpLocalFilePath = string.Empty;

        string mockV4Csv = "140.112.0.0/16,TW,Taiwan\n";

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            string url = req.RequestUri!.ToString();
            string content = url.Contains("geoip-v4") ? mockV4Csv : string.Empty;
            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
            return Task.FromResult(resp);
        });

        using HttpClient httpClient = new(mockHandler);
        using GeoIpUpdateService service = new(config, httpClient: httpClient);

        var result = await service.RefreshDatabaseAsync(isManual: true).ConfigureAwait(false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalRecords);
        Assert.IsTrue(GeoIpLookupService.TryLookup(IPAddress.Parse("140.112.1.1"), out string twCode, out _));
        Assert.AreEqual("TW", twCode);
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
