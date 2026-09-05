using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 使用具狀態的雲端契約模擬驗證多 IP 規則保留與精準刪除。
/// </summary>
[TestClass]
public sealed class CloudPerimeterStatefulTest
{
    /// <summary>
    /// 兩個 IP 取得不同優先順序；解除其中一個不影響另一個或使用者規則。
    /// </summary>
    /// <param name="azure">是否使用 Azure 契約。</param>
    /// <param name="conflict">是否模擬第一個更新遭遇版本衝突。</param>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, false)]
    [DataRow(false, true)]
    public async Task TwoAddressesDoNotOverwriteEachOther(bool azure, bool conflict)
    {
        using var handler = new StatefulHandler(azure) { ConflictOnce = conflict };
        using var client = new HttpClient(handler);
        ICloudPerimeterProvider provider = azure
            ? new AzureNsgPerimeterProvider(client) { BearerToken = "test", SubscriptionId = "sub", ResourceGroupName = "group", NetworkSecurityGroupName = "nsg" }
            : new GcpCloudArmorPerimeterProvider(client) { BearerToken = "test", ProjectId = "project", SecurityPolicyName = "policy" };
        using var lifetime = (IDisposable)provider;
        bool[] results = await Task.WhenAll(provider.BlockIpAsync("8.8.8.8", "test"), provider.BlockIpAsync("1.1.1.1", "test"));
        Assert.IsTrue(results.All(result => result));
        Assert.AreEqual(3, handler.Rules.Count);
        Assert.AreEqual(3, handler.Rules.Select(rule => handler.Properties(rule)["priority"]!.GetValue<int>()).Distinct().Count());
        Assert.IsTrue(await provider.UnblockIpAsync("8.8.8.8"));
        Assert.AreEqual(2, handler.Rules.Count);
        Assert.IsTrue(handler.Rules.Any(rule => handler.Properties(rule)["description"]!.GetValue<string>() == "user-owned"));
        Assert.IsTrue(handler.Rules.Any(rule => handler.Properties(rule)["description"]!.GetValue<string>() == "IDDSCommunity:v2:1.1.1.1/32"));
    }

    private sealed class StatefulHandler : HttpMessageHandler
    {
        private readonly bool azure;
        private int revision;
        internal bool ConflictOnce { get; set; }
        internal List<JsonNode> Rules { get; } = [];
        internal StatefulHandler(bool azure)
        {
            this.azure = azure;
            Rules.Add(JsonNode.Parse(azure
                ? """{"name":"user-rule","properties":{"priority":4096,"direction":"Inbound","description":"user-owned","access":"Allow"}}"""
                : """{"priority":2147483647,"description":"user-owned","action":"allow","match":{"config":{"srcIpRanges":["*"]}}}""")!);
        }
        internal JsonNode Properties(JsonNode rule) => azure ? rule["properties"]! : rule;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            string path = request.RequestUri!.AbsolutePath;
            if (azure)
            {
                if (!path.Contains("/securityRules/", StringComparison.Ordinal))
                    return Reply(new JsonObject { ["properties"] = new JsonObject { ["securityRules"] = new JsonArray(Rules.Select(r => r.DeepClone()).ToArray()) } });
                string name = path[(path.LastIndexOf('/') + 1)..];
                JsonNode? existing = Rules.SingleOrDefault(rule => rule["name"]!.GetValue<string>() == name);
                if (request.Method == HttpMethod.Get)
                    return existing is null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Reply(existing.DeepClone());
                if (request.Method == HttpMethod.Delete)
                {
                    if (existing is not null) Rules.Remove(existing);
                    return Reply(new JsonObject());
                }
                Assert.AreEqual(HttpMethod.Put, request.Method);
                JsonNode rule = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
                int priority = rule["properties"]!["priority"]!.GetValue<int>();
                Assert.IsFalse(Rules.Any(item => Properties(item)["priority"]!.GetValue<int>() == priority));
                rule["name"] = name;
                rule["properties"]!["provisioningState"] = "Succeeded";
                Rules.Add(rule);
                return Reply(rule.DeepClone());
            }
            if (request.Method == HttpMethod.Get)
                return Reply(new JsonObject { ["fingerprint"] = revision.ToString(), ["rules"] = new JsonArray(Rules.Select(r => r.DeepClone()).ToArray()) });
            Assert.AreEqual(HttpMethod.Patch, request.Method);
            JsonNode patch = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            Assert.AreEqual(revision.ToString(), patch["fingerprint"]!.GetValue<string>());
            if (ConflictOnce)
            {
                ConflictOnce = false;
                revision++;
                return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
            }
            Rules.Clear();
            Rules.AddRange(patch["rules"]!.AsArray().Select(rule => rule!.DeepClone()));
            revision++;
            return Reply(JsonNode.Parse("""{"status":"DONE"}""")!);
        }
        private static HttpResponseMessage Reply(JsonNode body) => new(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) };
    }
}