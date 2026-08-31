using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供邊緣節點（Edge Node）與威脅情資中繼中心（Threat Hub）通訊之客戶端。
/// </summary>
public sealed class ThreatHubClient : IDisposable
{
    private const string ApiKeyHeader = "X-IDDS-ThreatHub-ApiKey";
    private readonly HttpClient httpClient;
    private readonly bool disposeHttpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 初始化 <see cref="ThreatHubClient"/> 類別之新執行個體。
    /// </summary>
    /// <param name="httpClient">欲使用之 HTTP 客戶端執行個體，若為 null 則自動建立。</param>
    public ThreatHubClient(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            disposeHttpClient = true;
        }
        else
        {
            this.httpClient = httpClient;
            disposeHttpClient = false;
        }
    }

    /// <summary>
    /// 向指定的威脅情資中繼中心發送同步請求。
    /// </summary>
    /// <param name="endpoint">Threat Hub 伺服器端點 URL（例如 https://hub.corp.local:8443）。</param>
    /// <param name="apiKey">叢集授權 API 金鑰。</param>
    /// <param name="payload">本次同步請求載體。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>包含叢集生效威脅黑名單之回應載體。</returns>
    public async Task<ThreatHubSyncResponse> SynchronizeAsync(
        string endpoint,
        string apiKey,
        ThreatHubSyncPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(payload);

        string targetUrl = endpoint.TrimEnd('/') + "/api/threat-hub/sync";

        using HttpRequestMessage request = new(HttpMethod.Post, targetUrl);
        request.Headers.Add(ApiKeyHeader, apiKey);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new ThreatHubSyncResponse
            {
                Success = false,
                ErrorMessage = $"Threat Hub returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            };
        }

        ThreatHubSyncResponse? result = await response.Content.ReadFromJsonAsync<ThreatHubSyncResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? new ThreatHubSyncResponse { Success = false, ErrorMessage = "Empty response from Threat Hub." };
    }

    /// <summary>
    /// 釋放非受控資源。
    /// </summary>
    public void Dispose()
    {
        if (disposeHttpClient)
        {
            httpClient.Dispose();
        }
    }
}
