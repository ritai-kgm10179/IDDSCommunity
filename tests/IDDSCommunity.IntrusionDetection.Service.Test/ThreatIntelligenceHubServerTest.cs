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
    /// 驗證儀表板語言依查詢參數、Accept-Language 與英文回退規則選擇。
    /// </summary>
    [TestMethod]
    public void DashboardLanguage_UsesQueryThenAcceptLanguageThenEnglishFallback()
    {
        Assert.AreEqual("zh-Hant-TW", ThreatIntelligenceHubServer.ResolveDashboardLanguage("zh-TW", "en-US"));
        Assert.AreEqual("en-US", ThreatIntelligenceHubServer.ResolveDashboardLanguage("en", "zh-TW"));
        Assert.AreEqual("zh-Hant-TW", ThreatIntelligenceHubServer.ResolveDashboardLanguage(null, "fr-FR, zh-Hant;q=0.9, en;q=0.8"));
        Assert.AreEqual("zh-Hant-TW", ThreatIntelligenceHubServer.ResolveDashboardLanguage(null, "en-US;q=0.2, zh-TW;q=0.9"));
        Assert.AreEqual("en-US", ThreatIntelligenceHubServer.ResolveDashboardLanguage(null, "ja-JP"));
    }

    /// <summary>
    /// 驗證儀表板產生正確的 HTML 語言標籤與完整中英文字串。
    /// </summary>
    [TestMethod]
    public void DashboardHtml_UsesCanonicalLanguageTagsAndLocalizedText()
    {
        string chinese = ThreatIntelligenceHubServer.BuildDashboardHtml("zh-Hant-TW");
        StringAssert.Contains(chinese, "<html lang=\"zh-Hant-TW\">");
        StringAssert.Contains(chinese, "邊緣節點清單");
        Assert.IsFalse(chinese.Contains("{{", StringComparison.Ordinal));

        string english = ThreatIntelligenceHubServer.BuildDashboardHtml("en-US");
        StringAssert.Contains(english, "<html lang=\"en-US\">");
        StringAssert.Contains(english, "Edge nodes");
        Assert.IsFalse(english.Contains("{{", StringComparison.Ordinal));
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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

        using var server = new ThreatIntelligenceHubServer(config, _ => { }, allowLoopbackHttp: true);
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
        config.ManagementApiKey = "management-test-key";

        using var server = new ManagementApiHttpServer(config, new Database(), true);
        server.Start();
        if (!server.IsRunning)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        using var unauthorized = await client.GetAsync($"http://localhost:{port}/").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-Api-Key", "management-test-key");

        foreach (string invalidBody in new[] { "[]", "{\"ipAddress\":123}", "{" })
        {
            using var invalid = new StringContent(invalidBody, Encoding.UTF8, "application/json");
            using var invalidResponse = await client.PostAsync($"http://localhost:{port}/api/v1/locks", invalid).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        }
        using var oversized = new StringContent(new string('x', 65537), Encoding.UTF8, "application/json");
        using var oversizedResponse = await client.PostAsync($"http://localhost:{port}/api/v1/locks", oversized).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, oversizedResponse.StatusCode);

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
    /// 驗證 ManagementApiHttpServer 之 ChatOps 端點無需 X-Api-Key，僅需有效之 ActionToken 即可授權執行（相容 Slack/Teams/瀏覽器點擊）。
    /// </summary>
    /// <returns>代表非同步測試作業的 Task。</returns>
    [TestMethod]
    public async Task ManagementApi_ChatOpsActions_AuthenticateViaActionTokenWithoutApiKey()
    {
        int port = GetAvailablePort();
        IddsConfig config = IddsConfig.GetDefaultConfiguration();
        config.EnableManagementApi = true;
        config.ManagementApiPort = port;
        config.ManagementApiKey = "chatops-test-secret";

        using var server = new ManagementApiHttpServer(config, new Database(), true);
        server.Start();
        if (!server.IsRunning)
        {
            Assert.Inconclusive("無法於目前環境監聽本機通訊埠。");
        }

        using var client = new HttpClient();
        // 1. 未攜帶 Token 存取 actions/unblock 回傳 403 Forbidden
        using var noTokenResponse = await client.GetAsync($"http://localhost:{port}/api/v1/actions/unblock").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Forbidden, noTokenResponse.StatusCode);

        // 2. 攜帶無效/偽造 Token 回傳 403 Forbidden
        using var invalidTokenResponse = await client.GetAsync($"http://localhost:{port}/api/v1/actions/unblock?token=invalid_token").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Forbidden, invalidTokenResponse.StatusCode);

        // 3. 生成有效 ActionToken（不帶 X-Api-Key 標頭）以 GET 存取 actions/unblock 回傳 200 OK 預覽且不銷毀權杖（符合 RFC 9110 Safe Methods）
        string validToken = IDDSCommunity.IntrusionDetection.Shared.Security.ActionTokenService.GenerateToken(
            "unblock", "198.51.100.88", 15, "chatops-test-secret");
        using var previewResponse = await client.GetAsync($"http://localhost:{port}/api/v1/actions/unblock?token={Uri.EscapeDataString(validToken)}").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.OK, previewResponse.StatusCode);
        string previewBody = await previewResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(previewBody.Contains("\"preview\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(previewBody.Contains("\"requiresConfirmation\"", StringComparison.OrdinalIgnoreCase));

        // 4. 重複 GET 存取依然成功（證明 GET 絕未銷毀 Token）
        using var previewRepeat = await client.GetAsync($"http://localhost:{port}/api/v1/actions/unblock?token={Uri.EscapeDataString(validToken)}").ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.OK, previewRepeat.StatusCode);

        // 5. 以 POST 存取執行解鎖處置，回傳 202 Accepted 並單次銷毀 Token
        using var postResponse = await client.PostAsync($"http://localhost:{port}/api/v1/actions/unblock?token={Uri.EscapeDataString(validToken)}", null).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Accepted, postResponse.StatusCode);
        string postBody = await postResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.IsTrue(postBody.Contains("unblockRequested", StringComparison.OrdinalIgnoreCase));

        // 6. 再次以 POST 存取同一 Token 回傳 403 Forbidden（證明已於前次 POST 單次銷毀）
        using var postRepeat = await client.PostAsync($"http://localhost:{port}/api/v1/actions/unblock?token={Uri.EscapeDataString(validToken)}", null).ConfigureAwait(false);
        Assert.AreEqual(HttpStatusCode.Forbidden, postRepeat.StatusCode);
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
