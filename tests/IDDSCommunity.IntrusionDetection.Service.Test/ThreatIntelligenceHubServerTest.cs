using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.ManagementApi;
using IDDSCommunity.IntrusionDetection.Service.Observability;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 ThreatIntelligenceHubServer、ManagementApiHttpServer 與 MetricsHttpServer 之 HTTP 端點行為。
/// 包含健康探針（200 OK）、方法不符（405 Method Not Allowed 含 Allow 標頭）與 OWASP 安全無洩漏認證。
/// </summary>
[TestClass]
public sealed class ThreatIntelligenceHubServerTest
{
    private static int GetAvailablePort()
    {
        using TcpListener tcp = new(IPAddress.Loopback, 0);
        tcp.Start();
        int port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }

    /// <summary>
    /// 驗證對 ThreatHubServer 根路徑 GET 請求回傳 200 OK 與在線狀態。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_GetRoot_Returns200WithOnlineStatus()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var response = await client.GetAsync($"http://localhost:{port}/").ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(body.Contains("\"online\"", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證對 ThreatHubServer 之健康檢查端點 GET 請求回傳 200 OK。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_GetHealthz_Returns200()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var response = await client.GetAsync($"http://localhost:{port}/healthz").ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(body.Contains("\"online\"", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證對 ThreatHubServer 根路徑以非 GET/HEAD 方法發送請求時回傳 405 與 Allow 標頭。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_PostRoot_Returns405WithAllowHeader()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"http://localhost:{port}/", content).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.IsNotNull(response.Content.Headers.Allow);
        string allow = string.Join(",", response.Content.Headers.Allow);
        Assert.IsTrue(allow.Contains("GET", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證未提供 API Key 存取同步端點時回傳 401 且不洩漏金鑰細節。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_GetSyncWithoutKey_Returns401WithoutDetails()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var response = await client.GetAsync($"http://localhost:{port}/api/threat-hub/sync").ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsFalse(body.Contains("Invalid API Key", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(body.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證帶有正確 API Key 但使用 GET 存取同步端點時回傳 405 與 Allow: POST 標頭。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_GetSyncWithKey_Returns405WithAllowPost()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{port}/api/threat-hub/sync");
        request.Headers.Add("X-IDDS-ThreatHub-ApiKey", "hub_test_key");
        using var response = await client.SendAsync(request).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.IsNotNull(response.Content.Headers.Allow);
        string allow = string.Join(",", response.Content.Headers.Allow);
        Assert.IsTrue(allow.Contains("POST", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證帶有正確 API Key 且使用 POST 存取同步端點時回傳 200 OK。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ThreatHubServer_PostSyncWithKey_Returns200()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.ThreatHubRole = ThreatHubRole.ThreatHub;
        config.ThreatHubPort = port;
        config.ThreatHubApiKey = "hub_test_key";

        using var server = new ThreatIntelligenceHubServer(config, _ => { });
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{port}/api/threat-hub/sync");
        request.Headers.Add("X-IDDS-ThreatHub-ApiKey", "hub_test_key");
        request.Content = new StringContent("{\"reporterNodeId\":\"node-01\",\"reporterNodeName\":\"TestNode\",\"localThreats\":[]}", Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(body.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證 ManagementApiHttpServer 根路徑支援 GET 探針並於 POST 時回傳 405 與 Allow 標頭。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ManagementApi_RootAndStatus_Returns200And405OnUnsupportedMethod()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableManagementApi = true;
        config.ManagementApiPort = port;
        config.ManagementApiKey = string.Empty;

        using var server = new ManagementApiHttpServer(config, new Database());
        server.Start();
        if (!server.IsRunning)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();

        // 1. GET / 回傳 200 OK
        using var getResponse = await client.GetAsync($"http://localhost:{port}/").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        string getBody = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(getBody.Contains("\"healthy\"", StringComparison.OrdinalIgnoreCase));

        // 2. POST / 回傳 405 Method Not Allowed 且含有 Allow: GET, HEAD
        using var postContent = new StringContent("{}", Encoding.UTF8, "application/json");
        using var postResponse = await client.PostAsync($"http://localhost:{port}/", postContent).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
        Assert.IsNotNull(postResponse.Content.Headers.Allow);
        string allow = string.Join(",", postResponse.Content.Headers.Allow);
        Assert.IsTrue(allow.Contains("GET", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 驗證 MetricsHttpServer 根路徑支援健康探針，且非支援方法回傳 405 與 Allow 標頭。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task MetricsHttpServer_RootAndHealth_Returns200And405OnPost()
    {
        int port = GetAvailablePort();
        var config = new IddsConfig(new Database());
        var settings = new NotificationSettings(config)
        {
            EnableMetricsEndpoint = true,
            MetricsListenIp = "localhost",
            MetricsPort = port
        };

        using var server = new MetricsHttpServer(settings, new Database());
        server.Start();
        if (!server.IsListening)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();

        // 1. GET / 回傳 200 OK
        using var getResponse = await client.GetAsync($"http://localhost:{port}/").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        string getBody = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(getBody.Contains("\"healthy\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(getBody.Contains("/metrics", StringComparison.OrdinalIgnoreCase));

        // 2. POST /healthz 回傳 405 Method Not Allowed 且含有 Allow: GET, HEAD
        using var postContent = new StringContent("{}", Encoding.UTF8, "application/json");
        using var postResponse = await client.PostAsync($"http://localhost:{port}/healthz", postContent).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
        Assert.IsNotNull(postResponse.Content.Headers.Allow);
        string allowPost = string.Join(",", postResponse.Content.Headers.Allow);
        Assert.IsTrue(allowPost.Contains("GET", StringComparison.OrdinalIgnoreCase));
    }
}
