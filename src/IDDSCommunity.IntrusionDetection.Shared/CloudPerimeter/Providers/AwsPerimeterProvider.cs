using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Amazon Web Services (AWS) WAFv2 IPSet 與 Security Group 邊界防禦整合。
/// </summary>
public sealed class AwsPerimeterProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.Aws;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "Amazon Web Services (AWS WAFv2 / Security Group)";

    /// <summary>
    /// 取得或設定 AWS 區域 (例如 us-east-1, ap-northeast-1)。
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// 取得或設定 AWS WAFv2 IPSet 識別碼或名稱。
    /// </summary>
    public string IpSetId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 AWS API 存取端點 URL (若留空則依據 Region 自動產生)。
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 AWS API 金鑰或 Bearer Token。
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="AwsPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public AwsPerimeterProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 AWS 雲端邊界阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(ApiKey)) return false;

        try
        {
            string url = GetEffectiveEndpoint();
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";
            string payload = $"{{\"Action\":\"Block\",\"IpCidr\":\"{cidr}\",\"Reason\":\"{reason}\",\"IpSetId\":\"{IpSetId}\"}}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            request.Headers.Add("X-Amz-Target", "AWSWAF_20190729.UpdateIPSet");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步將指定 IP 位址自 AWS 雲端邊界阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(ApiKey)) return false;

        try
        {
            string url = GetEffectiveEndpoint();
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";
            string payload = $"{{\"Action\":\"Unblock\",\"IpCidr\":\"{cidr}\",\"IpSetId\":\"{IpSetId}\"}}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            request.Headers.Add("X-Amz-Target", "AWSWAF_20190729.UpdateIPSet");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步測試與 AWS 雲端邊界端點之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return (false, "AWS API Key / Bearer Token is required.");

        try
        {
            string url = GetEffectiveEndpoint();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            request.Headers.Add("X-Amz-Target", "AWSWAF_20190729.GetIPSet");
            request.Content = new StringContent($"{{\"IpSetId\":\"{IpSetId}\"}}", Encoding.UTF8, "application/x-amz-json-1.1");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "AWS WAF connection verified successfully.");

            return (false, $"AWS API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"AWS connection error: {ex.Message}");
        }
    }

    private string GetEffectiveEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(EndpointUrl)) return EndpointUrl;
        return $"https://wafv2.{Region}.amazonaws.com/";
    }
}
