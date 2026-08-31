using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class ExternalThreatFeedSubscriberTest
{
    [TestMethod]
    public async Task RefreshFeedsAsync_IngestsValidPublicIpsAndFiltersBogonAndWhitelist()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableExternalThreatFeeds = true;
        config.ThreatFeedMinLevel = 3;
        config.UseSafeNetworkList = true;
        config.SafeNetworks =
        [
            new IddsConfig.CSafeNetwork("140.112.50.50", "255.255.255.255") // Whitelisted admin IP
        ];

        string mockFeedContent = @"
# IPsum level 3 mock feed
10.0.0.99	5   # Bogon RFC 1918
140.112.50.50	5 # In SafeNetwork Whitelist
140.112.99.99	4 # Valid Malicious Public IP
1.1.1.1	2         # Level 2 < 3 (Ignored)
";

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(mockFeedContent)
            };
            return Task.FromResult(resp);
        });

        using HttpClient httpClient = new(mockHandler);
        List<ThreatIntelligenceItem> discoveredThreats = [];

        using ExternalThreatFeedSubscriberService subscriber = new(
            config,
            threat => discoveredThreats.Add(threat),
            httpClient: httpClient);

        await subscriber.RefreshFeedsAsync().ConfigureAwait(false);

        Assert.AreEqual(1, discoveredThreats.Count);
        Assert.AreEqual("140.112.99.99", discoveredThreats[0].SourceIp);
        Assert.AreEqual("EXTERNAL_FEED", discoveredThreats[0].ThreatCategory);
    }

    [TestMethod]
    public async Task RefreshFeedsAsync_UpdatesDynamicBogonsAndFiltersThreats()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableExternalThreatFeeds = true;
        config.EnableDynamicBogonUpdate = true;
        config.DynamicBogonIpv4Url = "http://localhost/cymru-fullbogons.txt";
        config.ThreatFeedMinLevel = 3;

        string mockBogonContent = "140.112.77.0/24\n";
        string mockFeedContent = "140.112.77.10\t5\n140.112.88.10\t5\n";

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            string url = req.RequestUri!.ToString();
            string content = url.Contains("cymru-fullbogons") ? mockBogonContent : mockFeedContent;
            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
            return Task.FromResult(resp);
        });

        using HttpClient httpClient = new(mockHandler);
        List<ThreatIntelligenceItem> discoveredThreats = [];

        using ExternalThreatFeedSubscriberService subscriber = new(
            config,
            threat => discoveredThreats.Add(threat),
            httpClient: httpClient);

        await subscriber.RefreshFeedsAsync().ConfigureAwait(false);

        Assert.AreEqual(1, discoveredThreats.Count);
        Assert.AreEqual("140.112.88.10", discoveredThreats[0].SourceIp);
        // 140.112.77.10 應被動態 Bogon (140.112.77.0/24) 成功過濾
    }

    [TestMethod]
    public async Task RefreshFeedsAsync_WhenDisabled_DoesNotIngestAnyThreats()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableExternalThreatFeeds = false;

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            Assert.Fail("HttpClient should not be invoked when feature is disabled.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using HttpClient httpClient = new(mockHandler);
        List<ThreatIntelligenceItem> discoveredThreats = [];

        using ExternalThreatFeedSubscriberService subscriber = new(
            config,
            threat => discoveredThreats.Add(threat),
            httpClient: httpClient);

        await subscriber.RefreshFeedsAsync().ConfigureAwait(false);

        Assert.AreEqual(0, discoveredThreats.Count);
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
