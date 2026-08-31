using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供中華電信 HiCloud / CVPC / CaaS 雲端虛擬私有網路安全群組 (OpenStack Neutron Security Group) 邊界防禦整合。
/// </summary>
public sealed class ChunghwaHiCloudPerimeterProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.ChunghwaTelecomHiCloud;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "中華電信 HiCloud / CVPC / CaaS (Security Group)";

    /// <summary>
    /// 取得或設定中華電信 CVPC / Neutron API 端點 URL (例如 https://cvpc.hicloud.hinet.net:9696)。
    /// </summary>
    public string EndpointUrl { get; set; } = "https://cvpc.hicloud.hinet.net:9696";

    /// <summary>
    /// 取得或設定中華電信 CVPC 安全群組識別碼 (Security Group ID)。
    /// </summary>
    public string SecurityGroupId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定中華電信 Keystone 認證 Token / API Key (X-Auth-Token)。
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="ChunghwaHiCloudPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public ChunghwaHiCloudPerimeterProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入中華電信 CVPC 邊界阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(AuthToken)) return false;

        try
        {
            string url = $"{EndpointUrl.TrimEnd('/')}/v2.0/security-group-rules";
            string ethertype = ipAddress.Contains(':') ? "IPv6" : "IPv4";
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";

            string payload = $$"""
            {
              "security_group_rule": {
                "direction": "ingress",
                "ethertype": "{{ethertype}}",
                "security_group_id": "{{SecurityGroupId}}",
                "remote_ip_prefix": "{{cidr}}",
                "description": "IDDS Block: {{reason}}"
              }
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Auth-Token", AuthToken);
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
    /// 非同步將指定 IP 位址自中華電信 CVPC 邊界阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(AuthToken)) return false;

        try
        {
            string searchUrl = $"{EndpointUrl.TrimEnd('/')}/v2.0/security-group-rules?security_group_id={SecurityGroupId}";
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchRequest.Headers.Add("X-Auth-Token", AuthToken);

            using var searchResponse = await httpClient.SendAsync(searchRequest, cancellationToken);
            if (!searchResponse.IsSuccessStatusCode) return false;

            string json = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";

            int ipIdx = json.IndexOf(cidr, StringComparison.OrdinalIgnoreCase);
            if (ipIdx < 0) return true; // 已經不在清單中

            int idIdx = json.LastIndexOf("\"id\": \"", ipIdx, StringComparison.OrdinalIgnoreCase);
            if (idIdx < 0) return false;

            int start = idIdx + 7;
            int end = json.IndexOf('"', start);
            if (end < 0) return false;
            string ruleId = json[start..end];

            string deleteUrl = $"{EndpointUrl.TrimEnd('/')}/v2.0/security-group-rules/{ruleId}";
            using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
            deleteRequest.Headers.Add("X-Auth-Token", AuthToken);

            using var deleteResponse = await httpClient.SendAsync(deleteRequest, cancellationToken);
            return deleteResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 非同步測試與中華電信 CVPC API 端點之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(AuthToken))
            return (false, "中華電信 CVPC Auth Token (X-Auth-Token) is required.");

        try
        {
            string url = $"{EndpointUrl.TrimEnd('/')}/v2.0/security-groups/{SecurityGroupId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", AuthToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "中華電信 CVPC API connection verified successfully.");

            return (false, $"中華電信 API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"中華電信 CVPC connection error: {ex.Message}");
        }
    }
}
