using System;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Microsoft Azure 網路安全性群組 (NSG) REST API 邊界防禦整合。
/// </summary>
public sealed class AzureNsgPerimeterProvider : ICloudPerimeterProvider, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly bool ownsClient;
    private readonly SemaphoreSlim mutation = new(1, 1);

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
        ownsClient = httpClient is null;
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 Azure NSG 邊界阻絕清單。
    /// </summary>
    public Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default) => MutateAsync(ipAddress, true, cancellationToken);

    /// <summary>
    /// 只移除產品擁有且符合來源 IP 的 NSG 規則。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>規則已移除或原本不存在時傳回成功。</returns>
    public Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default) => MutateAsync(ipAddress, false, cancellationToken);

    private async Task<bool> MutateAsync(string ipAddress, bool block, CancellationToken token)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress? address) || string.IsNullOrWhiteSpace(BearerToken)) return false;
        address = IpAddressCanonicalizer.Canonicalize(address);
        string cidr = address + (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "/32" : "/128");
        string name = "IDDS_Block_" + address.ToString().Replace('.', '_').Replace(':', '_');
        string marker = "IDDSCommunity:v2:" + cidr;
        await mutation.WaitAsync(token).ConfigureAwait(false);
        try
        {
            string url = GetRuleUrl(name);
            using HttpResponseMessage existing = await SendAsync(HttpMethod.Get, url, null, token).ConfigureAwait(false);
            if (existing.IsSuccessStatusCode)
            {
                using JsonDocument document = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(existing.Content, 1024 * 1024, token).ConfigureAwait(false));
                JsonElement properties = document.RootElement.GetProperty("properties");
                if (!properties.TryGetProperty("description", out var description) || description.GetString() != marker
                    || properties.GetProperty("sourceAddressPrefix").GetString() != cidr
                    || properties.GetProperty("access").GetString() != "Deny" || properties.GetProperty("direction").GetString() != "Inbound")
                    return false;
                if (block) return await IsUnshadowedAsync(properties.GetProperty("priority").GetInt32(), token).ConfigureAwait(false) && await WaitForRuleAsync(url, false, token).ConfigureAwait(false);
                using HttpResponseMessage deleted = await SendAsync(HttpMethod.Delete, url, null, token).ConfigureAwait(false);
                return deleted.IsSuccessStatusCode && await WaitForRuleAsync(url, true, token).ConfigureAwait(false);
            }
            if (existing.StatusCode != HttpStatusCode.NotFound) return false;
            if (!block) return true;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                using HttpResponseMessage list = await SendAsync(HttpMethod.Get, GetGroupUrl(), null, token).ConfigureAwait(false);
                if (!list.IsSuccessStatusCode) return false;
                using JsonDocument document = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(list.Content, 1024 * 1024, token).ConfigureAwait(false));
                HashSet<int> used = document.RootElement.GetProperty("properties").GetProperty("securityRules").EnumerateArray()
                    .Select(rule => rule.GetProperty("properties")).Where(rule => rule.GetProperty("direction").GetString() == "Inbound")
                    .Select(rule => rule.GetProperty("priority").GetInt32()).ToHashSet();
                int priority = Enumerable.Range(100, 3997).FirstOrDefault(value => !used.Contains(value));
                if (priority == 0 || !await IsUnshadowedAsync(priority, token).ConfigureAwait(false)) return false;
                string payload = JsonSerializer.Serialize(new { properties = new { protocol = "*", sourceAddressPrefix = cidr,
                    destinationAddressPrefix = "*", access = "Deny", direction = "Inbound", priority,
                    sourcePortRange = "*", destinationPortRange = "*", description = marker } });
                using HttpResponseMessage created = await SendAsync(HttpMethod.Put, url, payload, token).ConfigureAwait(false);
                if (created.StatusCode == HttpStatusCode.Conflict) continue;
                return created.IsSuccessStatusCode && await WaitForRuleAsync(url, false, token).ConfigureAwait(false);
            }
            return false;
        }
        finally { mutation.Release(); }
    }

    private async Task<bool> IsUnshadowedAsync(int priority, CancellationToken token)
    {
        using var response = await SendAsync(HttpMethod.Get, GetGroupUrl(), null, token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;
        using var document = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 1024 * 1024, token).ConfigureAwait(false));
        return !document.RootElement.GetProperty("properties").GetProperty("securityRules").EnumerateArray()
            .Select(rule => rule.GetProperty("properties"))
            .Any(rule => rule.GetProperty("direction").GetString() == "Inbound"
                && rule.GetProperty("priority").GetInt32() < priority && rule.GetProperty("access").GetString() != "Deny");
    }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? payload, CancellationToken token)
    {
        using HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
        if (payload is not null) request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
    }

    private async Task<bool> WaitForRuleAsync(string url, bool deleted, CancellationToken token)
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            using HttpResponseMessage response = await SendAsync(HttpMethod.Get, url, null, token).ConfigureAwait(false);
            if (deleted && response.StatusCode == HttpStatusCode.NotFound) return true;
            if (!response.IsSuccessStatusCode) return false;
            using JsonDocument document = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(response.Content, 1024 * 1024, token).ConfigureAwait(false));
            string? state = document.RootElement.GetProperty("properties").GetProperty("provisioningState").GetString();
            if (!deleted && state == "Succeeded") return true;
            if (state is "Failed" or "Canceled") return false;
            await Task.Delay(1000, token).ConfigureAwait(false);
        }
        return false;
    }

    private string GetGroupUrl() => $"https://management.azure.com/subscriptions/{Uri.EscapeDataString(SubscriptionId)}/resourceGroups/{Uri.EscapeDataString(ResourceGroupName)}/providers/Microsoft.Network/networkSecurityGroups/{Uri.EscapeDataString(NetworkSecurityGroupName)}?api-version=2025-07-01";
    /// <summary>
    /// 非同步測試與 Azure NSG ARM API 之連通性與授權。
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            return (false, "Azure ARM Bearer Token is required.");

        try
        {
            string url = $"https://management.azure.com/subscriptions/{Uri.EscapeDataString(SubscriptionId)}/resourceGroups/{Uri.EscapeDataString(ResourceGroupName)}/providers/Microsoft.Network/networkSecurityGroups/{Uri.EscapeDataString(NetworkSecurityGroupName)}?api-version=2025-07-01";
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
        return $"https://management.azure.com/subscriptions/{Uri.EscapeDataString(SubscriptionId)}/resourceGroups/{Uri.EscapeDataString(ResourceGroupName)}/providers/Microsoft.Network/networkSecurityGroups/{Uri.EscapeDataString(NetworkSecurityGroupName)}/securityRules/{Uri.EscapeDataString(ruleName)}?api-version=2025-07-01";
    }
    /// <summary>
    /// 釋放自行建立的 HTTP 用戶端與同步閘門；呼叫端須先停止處置作業。
    /// </summary>
    public void Dispose()
    {
        if (ownsClient) httpClient.Dispose();
        mutation.Dispose();
    }
}
