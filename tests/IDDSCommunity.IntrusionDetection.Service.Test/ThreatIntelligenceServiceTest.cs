using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class ThreatIntelligenceServiceTest
{
    [TestMethod]
    public async Task DynamicDnsResolverService_RefreshAsync_ResolvesLocalhostToDdnsCache()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.UseSafeNetworkList = true;
        config.SafeNetworks =
        [
            new IddsConfig.CSafeNetwork("localhost", string.Empty)
        ];

        using DynamicDnsResolverService resolver = new(config);
        await resolver.RefreshAsync().ConfigureAwait(false);

        Assert.IsTrue(DynamicDnsCache.TryGetResolvedIps("localhost", out HashSet<IPAddress> ips));
        Assert.IsTrue(ips.Count > 0);
        Assert.IsTrue(ips.Contains(IPAddress.Loopback) || ips.Contains(IPAddress.IPv6Loopback));
    }

    [TestMethod]
    public async Task ThreatIntelligenceSyncService_SynchronizeNowAsync_ProcessesClusterThreats()
    {
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.EdgeNode;
        config.ThreatHubEndpoint = "http://localhost:8443";
        config.ThreatHubApiKey = "sync_test_key";

        List<ThreatIntelligenceItem> receivedThreats = [];

        ThreatHubSyncResponse mockResponse = new()
        {
            Success = true,
            ServerTimeUtc = DateTime.UtcNow,
            ActiveThreats =
            [
                new ThreatIntelligenceItem
                {
                    SourceIp = "192.0.2.88",
                    ThreatCategory = "CROSS_AGENT_SPRAY",
                    ConfidenceScore = 1.0,
                    ReporterNodeName = "NODE-ALPHA"
                }
            ]
        };

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(mockResponse), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        });

        using HttpClient httpClient = new(mockHandler);
        using ThreatHubClient hubClient = new(httpClient);
        using ThreatIntelligenceSyncService syncService = new(
            config,
            threat => receivedThreats.Add(threat),
            client: hubClient);

        syncService.EnqueueLocalThreat(new ThreatIntelligenceItem { SourceIp = "198.51.100.12" });
        await syncService.SynchronizeNowAsync().ConfigureAwait(false);

        Assert.AreEqual(1, receivedThreats.Count);
        Assert.AreEqual("192.0.2.88", receivedThreats[0].SourceIp);
        Assert.AreEqual("NODE-ALPHA", receivedThreats[0].ReporterNodeName);
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
