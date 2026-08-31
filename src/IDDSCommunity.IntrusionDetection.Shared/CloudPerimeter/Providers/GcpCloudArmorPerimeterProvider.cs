using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Google Cloud Platform (GCP) Cloud Armor 與 Compute Engine Firewall 邊界防禦整合。
/// </summary>
public sealed class GcpCloudArmorPerimeterProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.Gcp;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "Google Cloud Platform (GCP Cloud Armor / VPC Firewall)";

    /// <summary>
    /// 取得或設定 GCP 專案識別碼 (Project ID)。
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Cloud Armor 安全政策名稱 (Security Policy Name)。
    /// </summary>
    public string SecurityPolicyName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 GCP OAuth2 / 服務帳戶 Bearer Token。
    /// </summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="GcpCloudArmorPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public GcpCloudArmorPerimeterProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 GCP Cloud Armor 邊界阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(BearerToken)) return false;

        try
        {
            string url = $"https://compute.googleapis.com/compute/v1/projects/{ProjectId}/global/securityPolicies/{SecurityPolicyName}/addRule";
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";

            string payload = $$"""
            {
              "action": "deny(403)",
              "priority": 1000,
              "match": {
                "versionedExpr": "SRC_IPS_V1",
                "config": {
                  "srcIpRanges": ["{{cidr}}"]
                }
              },
              "description": "IDDS Community Auto Block: {{reason}}"
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {BearerToken}");
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
    /// 非同步將指定 IP 位址自 GCP Cloud Armor 邊界阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(BearerToken)) return false;

        try
        {
            string url = $"https://compute.googleapis.com/compute/v1/projects/{ProjectId}/global/securityPolicies/{SecurityPolicyName}/removeRule?priority=1000";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {BearerToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步測試與 GCP Cloud Armor API 之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            return (false, "GCP OAuth2 Bearer Token is required.");

        try
        {
            string url = $"https://compute.googleapis.com/compute/v1/projects/{ProjectId}/global/securityPolicies/{SecurityPolicyName}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {BearerToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "GCP Cloud Armor connection verified successfully.");

            return (false, $"GCP API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"GCP connection error: {ex.Message}");
        }
    }
}
