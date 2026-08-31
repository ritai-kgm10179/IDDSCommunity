using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class ThreatHubTest
{
    [TestMethod]
    public void DynamicDnsCache_StoresAndMatchesResolvedIps()
    {
        DynamicDnsCache.Clear();
        string fqdn = "office.ddns.test";
        IPAddress ip1 = IPAddress.Parse("203.0.113.50");
        IPAddress ip2 = IPAddress.Parse("203.0.113.51");

        DynamicDnsCache.Update(fqdn, [ip1, ip2]);

        Assert.IsTrue(DynamicDnsCache.TryGetResolvedIps(fqdn, out HashSet<IPAddress> addresses));
        Assert.AreEqual(2, addresses.Count);
        Assert.IsTrue(DynamicDnsCache.IsIpInDdns(ip1, fqdn));
        Assert.IsTrue(DynamicDnsCache.IsIpInDdns(ip2, fqdn));
        Assert.IsFalse(DynamicDnsCache.IsIpInDdns(IPAddress.Parse("198.51.100.1"), fqdn));

        DynamicDnsCache.Clear();
        Assert.IsFalse(DynamicDnsCache.IsIpInDdns(ip1, fqdn));
    }

    [TestMethod]
    public async Task ThreatHubClient_SynchronizeAsync_SendsPayloadAndReturnsResponse()
    {
        ThreatHubSyncResponse expectedResponse = new()
        {
            Success = true,
            ServerTimeUtc = DateTime.UtcNow,
            ActiveThreats =
            [
                new ThreatIntelligenceItem
                {
                    SourceIp = "198.51.100.99",
                    ThreatCategory = "RDP_BRUTE_FORCE",
                    ConfidenceScore = 1.0,
                    ReporterNodeName = "SRV-TEST"
                }
            ]
        };

        HttpMessageHandler mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            Assert.AreEqual("POST", req.Method.Method);
            Assert.IsTrue(req.RequestUri!.ToString().EndsWith("/api/threat-hub/sync"));
            Assert.IsTrue(req.Headers.Contains("X-IDDS-ThreatHub-ApiKey"));

            HttpResponseMessage resp = new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        });

        using HttpClient client = new(mockHandler);
        using ThreatHubClient hubClient = new(client);

        ThreatHubSyncPayload payload = new()
        {
            NodeId = "node_1",
            NodeName = "SRV-LOCAL",
            NewThreats =
            [
                new ThreatIntelligenceItem
                {
                    SourceIp = "203.0.113.10",
                    ThreatCategory = "SSH_SPRAY"
                }
            ]
        };

        ThreatHubSyncResponse result = await hubClient.SynchronizeAsync(
            "http://localhost:8443",
            "test_api_key",
            payload,
            CancellationToken.None);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.ActiveThreats.Count);
        Assert.AreEqual("198.51.100.99", result.ActiveThreats[0].SourceIp);
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
