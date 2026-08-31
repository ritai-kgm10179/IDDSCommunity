using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Microsoft Azure 網路安全性群組 (NSG) REST API 邊界防禦整合。
/// </summary>
public sealed class AzureNsgPerimeterProvider : ICloudPerimeterProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// 取得提供者類型。
    /// </summary>
    public CloudPerimeterType ProviderType => CloudPerimeterType.Azure;

    /// <summary>
    /// 取得提供者名稱。
    /// </summary>
    public string Name => "Microsoft Azure (Network Security Group NSG)";

    /// <summary>
    /// 取得或設定 Azure 訂用帳戶識別碼 (Subscription ID)。
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Azure 資源群組名稱 (Resource Group)。
    /// </summary>
    public string ResourceGroupName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Azure NSG 名稱。
    /// </summary>
    public string NetworkSecurityGroupName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 Azure ARM REST API 授權 Token。
    /// </summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="AzureNsgPerimeterProvider"/> 類別的新執行個體。
    /// </summary>
    /// <param name="httpClient">選用的自訂 HTTP 用戶端。</param>
    public AzureNsgPerimeterProvider(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 Azure NSG 邊界阻絕清單。
    /// </summary>
    public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(BearerToken)) return false;

        try
        {
            string sanitizedIp = ipAddress.Replace('.', '_').Replace(':', '_');
            string ruleName = $"IDDS_Block_{sanitizedIp}";
            string url = GetRuleUrl(ruleName);
            string cidr = ipAddress.Contains(':') ? $"{ipAddress}/128" : $"{ipAddress}/32";

            string payload = $$"""
            {
              "properties": {
                "protocol": "*",
                "sourceAddressPrefix": "{{cidr}}",
                "destinationAddressPrefix": "*",
                "access": "Deny",
                "direction": "Inbound",
                "priority": 110,
                "sourcePortRange": "*",
                "destinationPortRange": "*"
              }
            }
            """;

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
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
    /// 非同步將指定 IP 位址自 Azure NSG 邊界阻絕清單移除。
    /// </summary>
    public async Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(BearerToken)) return false;

        try
        {
            string sanitizedIp = ipAddress.Replace('.', '_').Replace(':', '_');
            string ruleName = $"IDDS_Block_{sanitizedIp}";
            string url = GetRuleUrl(ruleName);

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
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
    /// 非同步測試與 Azure NSG ARM API 之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            return (false, "Azure ARM Bearer Token is required.");

        try
        {
            string url = $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Network/networkSecurityGroups/{NetworkSecurityGroupName}?api-version=2023-09-01";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {BearerToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, "Azure NSG connection verified successfully.");

            return (false, $"Azure ARM API returned status {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Azure connection error: {ex.Message}");
        }
    }

    private string GetRuleUrl(string ruleName)
    {
        return $"https://management.azure.com/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Network/networkSecurityGroups/{NetworkSecurityGroupName}/securityRules/{ruleName}?api-version=2023-09-01";
    }
}
