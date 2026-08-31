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
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new AwsPerimeterProvider(client)
        {
            ApiKey = "test-aws-key",
            IpSetId = "ipset-12345",
            Region = "ap-northeast-1"
        };

        bool blocked = await provider.BlockIpAsync("198.51.100.25", "SSH brute force");
        Assert.IsTrue(blocked);
        Assert.IsNotNull(handler.LastRequest);
        Assert.IsTrue(handler.LastRequestBody!.Contains("198.51.100.25/32"));
        Assert.IsTrue(handler.LastRequestBody.Contains("ipset-12345"));

        bool unblocked = await provider.UnblockIpAsync("198.51.100.25");
        Assert.IsTrue(unblocked);
        Assert.IsTrue(handler.LastRequestBody.Contains("Unblock"));
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

        bool blocked = await provider.BlockIpAsync("203.0.113.88", "RDP Attack");
        Assert.IsTrue(blocked);
        Assert.IsNotNull(handler.LastRequest);
        Assert.AreEqual(HttpMethod.Put, handler.LastRequest.Method);
        Assert.IsTrue(handler.LastRequest.RequestUri!.ToString().Contains("subscriptions/sub-123/resourceGroups/rg-prod"));
        Assert.IsTrue(handler.LastRequestBody!.Contains("203.0.113.88/32"));

        bool unblocked = await provider.UnblockIpAsync("203.0.113.88");
        Assert.IsTrue(unblocked);
        Assert.AreEqual(HttpMethod.Delete, handler.LastRequest.Method);
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

        bool blocked = await provider.BlockIpAsync("198.51.100.77", "Web vulnerability scan");
        Assert.IsTrue(blocked);
        Assert.IsNotNull(handler.LastRequest);
        Assert.IsTrue(handler.LastRequest.RequestUri!.ToString().Contains("projects/my-gcp-project/global/securityPolicies/armor-policy-default/addRule"));
        Assert.IsTrue(handler.LastRequestBody!.Contains("198.51.100.77/32"));
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

        bool blocked = await provider.BlockIpAsync("203.0.113.55", "SQL brute force");
        Assert.IsTrue(blocked);
        Assert.IsTrue(handler.LastRequest!.RequestUri!.ToString().Contains("v2.0/security-group-rules"));
        Assert.IsTrue(handler.LastRequestBody!.Contains("sg-hinet-001"));
        Assert.IsTrue(handler.LastRequestBody.Contains("203.0.113.55/32"));
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
    }
}
