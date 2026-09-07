using System;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Network;

namespace IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter.Providers;

/// <summary>
/// 提供 Google Cloud Platform (GCP) Cloud Armor 與 Compute Engine Firewall 邊界防禦整合。
/// </summary>
public sealed class GcpCloudArmorPerimeterProvider : ICloudPerimeterProvider, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly bool ownsClient;
    private readonly SemaphoreSlim mutation = new(1, 1);

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
        ownsClient = httpClient is null;
        this.httpClient = httpClient ?? HttpClientHelper.CreatePooledClient(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// 非同步將指定 IP 位址加入 GCP Cloud Armor 邊界阻絕清單。
    /// </summary>
    public Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default) => MutateAsync(ipAddress, true, cancellationToken);

    /// <summary>
    /// 驗證來源與產品標記後才移除 Cloud Armor 規則。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>遠端操作完成時傳回成功。</returns>
    public Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default) => MutateAsync(ipAddress, false, cancellationToken);

    private async Task<bool> MutateAsync(string ipAddress, bool block, CancellationToken token)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress? address) || string.IsNullOrWhiteSpace(BearerToken)) return false;
        address = IpAddressCanonicalizer.Canonicalize(address);
        string cidr = address + (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "/32" : "/128");
        string marker = "IDDSCommunity:v2:" + cidr;
        string policy = $"https://compute.googleapis.com/compute/v1/projects/{Uri.EscapeDataString(ProjectId)}/global/securityPolicies/{Uri.EscapeDataString(SecurityPolicyName)}";
        await mutation.WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                using HttpResponseMessage current = await SendAsync(HttpMethod.Get, policy, null, token).ConfigureAwait(false);
                if (!current.IsSuccessStatusCode) return false;
                using JsonDocument document = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(current.Content, 1024 * 1024, token).ConfigureAwait(false));
                JsonElement[] rules = document.RootElement.GetProperty("rules").EnumerateArray().ToArray();
                JsonElement[] owned = rules.Where(rule => rule.TryGetProperty("description", out var description) && description.GetString() == marker
                    && rule.GetProperty("action").GetString() == "deny(403)"
                    && rule.GetProperty("match").GetProperty("config").GetProperty("srcIpRanges").EnumerateArray().Select(ip => ip.GetString()).SequenceEqual(new[] { cidr })).ToArray();
                if (owned.Length > 1) return false;
                if (block && owned.Length == 1) return (!owned[0].TryGetProperty("preview", out var preview) || !preview.GetBoolean())
                    && !rules.Any(rule => rule.GetProperty("priority").GetInt32() < owned[0].GetProperty("priority").GetInt32() && rule.GetProperty("action").GetString() != "deny(403)");
                if (!block && owned.Length == 0) return true;
                if (!document.RootElement.TryGetProperty("fingerprint", out var fingerprint) || string.IsNullOrWhiteSpace(fingerprint.GetString())) return false;
                string url = policy;
                string? payload = null;
                if (block)
                {
                    HashSet<int> used = rules.Select(rule => rule.GetProperty("priority").GetInt32()).ToHashSet();
                    int priority = Enumerable.Range(0, 100000).Where(value => !used.Contains(value)).DefaultIfEmpty(-1).First();
                    if (priority < 0 || rules.Any(rule => rule.GetProperty("priority").GetInt32() < priority && rule.GetProperty("action").GetString() != "deny(403)")) return false;
                    payload = JsonSerializer.Serialize(new { action = "deny(403)", priority, description = marker,
                        match = new { versionedExpr = "SRC_IPS_V1", config = new { srcIpRanges = new[] { cidr } } } });

                }
                var updatedRules = rules.Where(rule => block || rule.GetProperty("priority").GetInt32() != owned[0].GetProperty("priority").GetInt32())
                    .Select(rule => System.Text.Json.Nodes.JsonNode.Parse(rule.GetRawText())!).ToList();
                if (block) updatedRules.Add(System.Text.Json.Nodes.JsonNode.Parse(payload!)!);
                foreach (var rule in updatedRules) rule.AsObject().Remove("kind");
                payload = JsonSerializer.Serialize(new { fingerprint = fingerprint.GetString(), rules = updatedRules });
                using HttpResponseMessage changed = await SendAsync(HttpMethod.Patch, url, payload, token).ConfigureAwait(false);
                if (changed.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed) continue;
                if (!changed.IsSuccessStatusCode) return false;
                using JsonDocument operation = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(changed.Content, 1024 * 1024, token).ConfigureAwait(false));
                JsonElement result = operation.RootElement;
                for (int poll = 0; poll < 15; poll++)
                {
                    if (result.TryGetProperty("error", out _)) return false;
                    if (result.GetProperty("status").GetString() == "DONE") return true;
                    string? operationName = result.GetProperty("name").GetString();
                    if (string.IsNullOrEmpty(operationName)) return false;
                    await Task.Delay(1000, token).ConfigureAwait(false);
                    using HttpResponseMessage status = await SendAsync(HttpMethod.Get, $"https://compute.googleapis.com/compute/v1/projects/{Uri.EscapeDataString(ProjectId)}/global/operations/{Uri.EscapeDataString(operationName)}", null, token).ConfigureAwait(false);
                    if (!status.IsSuccessStatusCode) return false;
                    using JsonDocument next = JsonDocument.Parse(await IDDSCommunity.IntrusionDetection.Shared.Network.BoundedHttpContent.ReadAsync(status.Content, 1024 * 1024, token).ConfigureAwait(false));
                    result = next.RootElement.Clone();
                }
                return false;
            }
            return false;
        }
        finally { mutation.Release(); }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? payload, CancellationToken token)
    {
        using HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
        if (payload is not null) request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
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
    /// <summary>
    /// 釋放自行建立的 HTTP 用戶端與同步閘門；呼叫端須先停止處置作業。
    /// </summary>
    public void Dispose()
    {
        if (ownsClient) httpClient.Dispose();
        mutation.Dispose();
    }
}
