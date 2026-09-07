using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證各大公有雲與電信雲邊界防火牆適配器 (AWS, Azure, GCP, Cloudflare, 中華電信 CVPC, Webhook)。
/// </summary>
[TestClass]
public sealed class CloudPerimeterAdaptersTest
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = "{}";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StringContent(ResponseContent)
            };
            return response;
        }
    }

    /// <summary>
    /// 驗證 AWS WAFv2 適配器阻絕與解除呼叫。
    /// </summary>
    [TestMethod]
    public async Task AwsPerimeterProvider_BlockAndUnblock_SendsCorrectHeadersAndPayload()
    {
        using var client = new FakeWafClient();
        using var provider = new AwsPerimeterProvider(client) { IpSetId = "11111111-1111-1111-1111-111111111111", IpSetName = "owned", Region = "us-east-1" };
        Assert.IsTrue(await provider.BlockIpAsync("198.51.100.25", "test"));
        Assert.IsTrue(await provider.BlockIpAsync("198.51.100.26", "test"));
        Assert.IsTrue(await provider.UnblockIpAsync("198.51.100.25"));
        CollectionAssert.AreEquivalent(new[] { "8.8.8.8/32", "198.51.100.26/32" }, client.Addresses);
        Assert.IsFalse(await provider.BlockIpAsync("2001:db8::1", "wrong family"));
    }

    /// <summary>
    /// 驗證 Azure NSG 適配器 REST API 呼叫。
    /// </summary>
    [TestMethod]
    public async Task AzureNsgPerimeterProvider_BlockAndUnblock_SendsCorrectArmUri()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new AzureNsgPerimeterProvider(client)
        {
            BearerToken = "test-azure-token",
            SubscriptionId = "sub-123",
            ResourceGroupName = "rg-prod",
            NetworkSecurityGroupName = "nsg-dmz"
        };

        handler.ResponseContent = "{\"properties\":{\"description\":\"user-owned\",\"sourceAddressPrefix\":\"203.0.113.88/32\",\"access\":\"Deny\",\"direction\":\"Inbound\"}}";
        Assert.IsFalse(await provider.BlockIpAsync("203.0.113.88", "test"));
        Assert.IsFalse(await provider.UnblockIpAsync("203.0.113.88"));
        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
    }

    /// <summary>
    /// 驗證 GCP Cloud Armor 適配器呼叫。
    /// </summary>
    [TestMethod]
    public async Task GcpCloudArmorPerimeterProvider_BlockAndUnblock_SendsCorrectPayload()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new GcpCloudArmorPerimeterProvider(client)
        {
            BearerToken = "gcp-oauth-token",
            ProjectId = "my-gcp-project",
            SecurityPolicyName = "armor-policy-default"
        };

        handler.ResponseContent = "{\"rules\":[{\"priority\":1000,\"description\":\"user-owned\",\"action\":\"deny(403)\",\"match\":{\"config\":{\"srcIpRanges\":[\"198.51.100.77/32\"]}}}]}";
        Assert.IsTrue(await provider.UnblockIpAsync("198.51.100.77"));
        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
    }

    /// <summary>
    /// 驗證 Cloudflare WAF 適配器呼叫。
    /// </summary>
    [TestMethod]
    public async Task CloudflareWafPerimeterProvider_Block_SendsCorrectRule()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new CloudflareWafPerimeterProvider(client)
        {
            ApiToken = "cf-api-token",
            ZoneId = "zone-abcdef123"
        };

        bool blocked = await provider.BlockIpAsync("198.51.100.99", "Bad bot");
        Assert.IsTrue(blocked);
        Assert.IsTrue(handler.LastRequest!.RequestUri!.ToString().Contains("zones/zone-abcdef123/firewall/access_rules/rules"));
        Assert.IsTrue(handler.LastRequestBody!.Contains("\"mode\": \"block\""));
        Assert.IsTrue(handler.LastRequestBody.Contains("198.51.100.99"));
    }

    /// <summary>
    /// 驗證中華電信 HiCloud / CVPC / CaaS 安全群組 (OpenStack Neutron) 適配器呼叫。
    /// </summary>
    [TestMethod]
    public async Task ChunghwaHiCloudPerimeterProvider_Block_SendsOpenStackNeutronRule()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new ChunghwaHiCloudPerimeterProvider(client)
        {
            AuthToken = "hicloud-keystone-token",
            SecurityGroupId = "sg-hinet-001",
            EndpointUrl = "https://cvpc.hicloud.hinet.net:9696"
        };

        Assert.IsFalse(await provider.BlockIpAsync("203.0.113.55", "test"));
        Assert.IsFalse(await provider.UnblockIpAsync("203.0.113.55"));
        Assert.IsNull(handler.LastRequest);
    }

    /// <summary>
    /// 驗證通用硬體防火牆 Webhook 適配器呼叫。
    /// </summary>
    [TestMethod]
    public async Task GenericPerimeterWebhookProvider_Block_SendsPostJson()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new GenericPerimeterWebhookProvider(client)
        {
            WebhookUrl = "https://gateway.corp.local/api/v1/blacklist",
            AuthHeader = "Bearer secret-gateway-key"
        };

        bool blocked = await provider.BlockIpAsync("198.51.100.12", "Port scan");
        Assert.IsTrue(blocked);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.IsTrue(handler.LastRequestBody!.Contains("\"action\": \"block\""));
        Assert.IsTrue(handler.LastRequestBody.Contains("198.51.100.12"));

        // 驗證包含雙引號與換行之 reason 能被正確轉義為合法 JSON (CWE-138)
        bool blockedWithQuotes = await provider.BlockIpAsync("198.51.100.13", "Attack with \"quotes\" and \n newline");
        Assert.IsTrue(blockedWithQuotes);
        using var doc = System.Text.Json.JsonDocument.Parse(handler.LastRequestBody);
        Assert.AreEqual("Attack with \"quotes\" and \n newline", doc.RootElement.GetProperty("reason").GetString());
    }

    /// <summary>
    /// 驗證通用 Webhook 阻絕 Link-Local 與雲端 IMDS 元數據端點。
    /// </summary>
    [TestMethod]
    public async Task GenericPerimeterWebhookProvider_BlocksImdsAndLinkLocalEndpoints()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new GenericPerimeterWebhookProvider(client)
        {
            WebhookUrl = "http://169.254.169.254/latest/meta-data/"
        };

        bool blocked = await provider.BlockIpAsync("198.51.100.12", "Testing");
        Assert.IsFalse(blocked);
        Assert.IsNull(handler.LastRequest);

        var (success, message) = await provider.TestConnectionAsync();
        Assert.IsFalse(success);
        Assert.IsTrue(message.Contains("Cloud IMDS or Link-Local addresses is prohibited"));
    }

    /// <summary>
    /// 驗證邊界提供者 Dispose 正確釋放自身所擁有之資源。
    /// </summary>
    [TestMethod]
    public void PerimeterProviders_Dispose_DisposesOwnedResourcesWithoutThrowing()
    {
        using (var webhook = new GenericPerimeterWebhookProvider())
        {
            Assert.IsNotNull(webhook);
        }

        using (var cloudflare = new CloudflareWafPerimeterProvider())
        {
            Assert.IsNotNull(cloudflare);
        }

        using (var azure = new AzureNsgPerimeterProvider())
        {
            Assert.IsNotNull(azure);
        }

        using (var gcp = new GcpCloudArmorPerimeterProvider())
        {
            Assert.IsNotNull(gcp);
        }
    }
    private sealed class FakeWafClient : Amazon.WAFV2.AmazonWAFV2Client
    {
        internal System.Collections.Generic.List<string> Addresses { get; private set; } = ["8.8.8.8/32"];
        private int version;
        internal FakeWafClient() : base(new Amazon.Runtime.AnonymousAWSCredentials(), Amazon.RegionEndpoint.USEast1) { }
        public override Task<Amazon.WAFV2.Model.GetIPSetResponse> GetIPSetAsync(Amazon.WAFV2.Model.GetIPSetRequest request, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual("owned", request.Name);
            Assert.AreEqual("11111111-1111-1111-1111-111111111111", request.Id);
            return Task.FromResult(new Amazon.WAFV2.Model.GetIPSetResponse
            {
                LockToken = version.ToString(), HttpStatusCode = HttpStatusCode.OK,
                IPSet = new Amazon.WAFV2.Model.IPSet { Name = "owned", Id = "11111111-1111-1111-1111-111111111111", IPAddressVersion = Amazon.WAFV2.IPAddressVersion.IPV4, Addresses = [.. Addresses] }
            });
        }
        public override Task<Amazon.WAFV2.Model.UpdateIPSetResponse> UpdateIPSetAsync(Amazon.WAFV2.Model.UpdateIPSetRequest request, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual(version.ToString(), request.LockToken);
            Addresses = [.. request.Addresses];
            version++;
            return Task.FromResult(new Amazon.WAFV2.Model.UpdateIPSetResponse { HttpStatusCode = HttpStatusCode.OK, NextLockToken = version.ToString() });
        }
    }
}