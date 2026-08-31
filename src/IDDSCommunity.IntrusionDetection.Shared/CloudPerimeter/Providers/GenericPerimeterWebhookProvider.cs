using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供通用邊界硬體防火牆 / 自建閘道控制器 Webhook 整合 (FortiGate, Palo Alto, OPNsense 等)。
/// </summary>
public sealed class GenericPerimeterWebhookProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.GenericWebhook;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "通用邊界防火牆 Webhook (Generic Edge Firewall API)";

    /// <summary>
    /// 取得或設定 Webhook 端點 URL。
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定自訂授權標頭 (例如 Bearer Token, API Key 等)。
    /// </summary>
    public string AuthHeader { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="GenericPerimeterWebhookProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public GenericPerimeterWebhookProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址透過 Webhook 推播至邊界防火牆阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(WebhookUrl)) return false;

        try
        {
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";
            string payload = $$"""
            {
              "action": "block",
              "ip": "{{ipAddress}}",
              "cidr": "{{cidr}}",
              "reason": "{{reason}}",
              "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);
            if (!string.IsNullOrWhiteSpace(AuthHeader))
                request.Headers.TryAddWithoutValidation("Authorization", AuthHeader);
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
    /// 非同步將指定 IP 位址透過 Webhook 自邊界防火牆阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(WebhookUrl)) return false;

        try
        {
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";
            string payload = $$"""
            {
              "action": "unblock",
              "ip": "{{ipAddress}}",
              "cidr": "{{cidr}}",
              "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);
            if (!string.IsNullOrWhiteSpace(AuthHeader))
                request.Headers.TryAddWithoutValidation("Authorization", AuthHeader);
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
    /// 非同步測試與通用 Webhook 端點之連通性。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(WebhookUrl))
            return (false, "Webhook URL is required.");

        try
        {
            string payload = $$"""
            {
              "action": "test",
              "message": "IDDS Community perimeter webhook connectivity test",
              "timestamp": "{{DateTime.UtcNow:O}}"
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);
            if (!string.IsNullOrWhiteSpace(AuthHeader))
                request.Headers.TryAddWithoutValidation("Authorization", AuthHeader);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "Generic perimeter webhook verified successfully.");

            return (false, $"Webhook returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Webhook connection error: {ex.Message}");
        }
    }
}
