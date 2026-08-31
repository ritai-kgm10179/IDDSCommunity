using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Cloudflare WAF IP Access Rules 邊界防禦整合。
/// </summary>
public sealed class CloudflareWafPerimeterProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.Cloudflare;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "Cloudflare (WAF IP Access Rules)";

    /// <summary>
    /// 取得或設定 Cloudflare Zone ID 或 Account ID。
    /// </summary>
    public string ZoneId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Cloudflare API Token。
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="CloudflareWafPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用 boyhood 端。</param>
    public CloudflareWafPerimeterProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 Cloudflare WAF 邊界阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(ApiToken)) return false;

        try
        {
            string url = $"https://api.cloudflare.com/client/v4/zones/{ZoneId}/firewall/access_rules/rules";
            string targetType = ipAddress.Contains(':') ? "ip6" : "ip";

            string payload = $$"""
            {
              "mode": "block",
              "configuration": {
                "target": "{{targetType}}",
                "value": "{{ipAddress}}"
              },
              "notes": "IDDS Community Auto Block: {{reason}}"
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {ApiToken}");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步將指定 IP 位址自 Cloudflare WAF 邊界阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(ApiToken)) return false;

        try
        {
            // 查詢既有 Rule ID
            string searchUrl = $"https://api.cloudflare.com/client/v4/zones/{ZoneId}/firewall/access_rules/rules?configuration.value={ipAddress}&mode=block";
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchRequest.Headers.Add("Authorization", $"Bearer {ApiToken}");

            using var searchResponse = await httpClient.SendAsync(searchRequest, cancellationToken);
            if (!searchResponse.IsSuccessStatusCode) return false;

            string searchJson = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
            // 簡易擷取 rule id
            int idIdx = searchJson.IndexOf("\"id\":\"", StringComparison.OrdinalIgnoreCase);
            if (idIdx < 0) return true; // 已不存在

            int start = idIdx + 6;
            int end = searchJson.IndexOf('"', start);
            if (end < 0) return false;
            string ruleId = searchJson[start..end];

            string deleteUrl = $"https://api.cloudflare.com/client/v4/zones/{ZoneId}/firewall/access_rules/rules/{ruleId}";
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
            deleteRequest.Headers.Add("Authorization", $"Bearer {ApiToken}");

            using var deleteResponse = await httpClient.SendAsync(deleteRequest, cancellationToken);
            return deleteResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步測試與 Cloudflare API 之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiToken))
            return (false, "Cloudflare API Token is required.");

        try
        {
            string url = "https://api.cloudflare.com/client/v4/user/tokens/verify";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {ApiToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "Cloudflare API Token verified successfully.");

            return (false, $"Cloudflare API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Cloudflare connection error: {ex.Message}");
        }
    }
}
