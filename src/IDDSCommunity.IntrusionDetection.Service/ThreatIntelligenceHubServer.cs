using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供集中式威脅情資中繼中心（Threat Hub）之輕量級 HTTP API 服務端點。
/// </summary>
internal sealed class ThreatIntelligenceHubServer : IDisposable
{
    private const string ApiKeyHeader = "X-IDDS-ThreatHub-ApiKey";
    private readonly IddsConfig config;
    private readonly Action<ThreatIntelligenceItem> onThreatReceived;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logError;
    private readonly ConcurrentDictionary<string, ThreatIntelligenceItem> activeThreats = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, EdgeNodeState> registeredNodes = new(StringComparer.OrdinalIgnoreCase);

    private HttpListener? listener;
    private CancellationTokenSource? cts;
    private Task? listenTask;
    private bool disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 代表已註冊邊緣節點之即時狀態。
    /// </summary>
    public sealed record EdgeNodeState(string NodeId, string NodeName, string NodeIp, DateTime LastSeenUtc, int ReportedThreatCount);

    /// <summary>
    /// 初始化 <see cref="ThreatIntelligenceHubServer"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域設定執行個體。</param>
    /// <param name="onThreatReceived">當接收到邊緣節點回報之新威脅時引發之回呼委派。</param>
    /// <param name="logInformation">資訊日誌回報委派。</param>
    /// <param name="logError">錯誤日誌回報委派。</param>
    public ThreatIntelligenceHubServer(
        IddsConfig config,
        Action<ThreatIntelligenceItem> onThreatReceived,
        Action<string>? logInformation = null,
        Action<string, Exception>? logError = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.onThreatReceived = onThreatReceived ?? throw new ArgumentNullException(nameof(onThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logError = logError ?? ((msg, ex) => System.Diagnostics.Trace.TraceError("{0}: {1}", msg, ex.Message));
    }

    /// <summary>
    /// 取得目前中繼中心所維護之全網活動威脅情資清單。
    /// </summary>
    public IReadOnlyList<ThreatIntelligenceItem> ActiveThreats => [.. activeThreats.Values];

    /// <summary>
    /// 取得目前已連線註冊之邊緣節點清單。
    /// </summary>
    public IReadOnlyList<EdgeNodeState> RegisteredNodes => [.. registeredNodes.Values];

    /// <summary>
    /// 將本機產生之硬封鎖威脅主動注入至 Hub 威脅庫中。
    /// </summary>
    /// <param name="item">威脅情資項目。</param>
    public void IngestLocalThreat(ThreatIntelligenceItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.SourceIp)) return;
        activeThreats[item.SourceIp.Trim()] = item;
    }

    /// <summary>
    /// 啟動 Threat Hub HTTP 監聽服務。
    /// </summary>
    public void Start()
    {
        if (disposed || listener != null) return;

        int port = config.ThreatHubPort > 0 ? config.ThreatHubPort : 8443;
        listener = new HttpListener();
        try
        {
            listener.Prefixes.Add($"http://+:{port}/api/threat-hub/");
            listener.Start();
        }
        catch
        {
            // 若無管理員 URL ACL 權限，回退至 localhost 監聽
            listener.Close();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/api/threat-hub/");
            try
            {
                listener.Start();
            }
            catch
            {
                listener.Close();
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/api/threat-hub/");
                listener.Start();
            }
        }

        cts = new CancellationTokenSource();
        listenTask = ListenLoopAsync(listener, cts.Token);
        logInformation($"Threat Intelligence Hub server started listening on port {port}.");
    }

    private async Task ListenLoopAsync(HttpListener httpListener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && httpListener.IsListening)
        {
            try
            {
                HttpListenerContext context = await httpListener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                logError("Threat Hub listener exception in accept loop", ex);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        HttpListenerRequest req = context.Request;
        HttpListenerResponse resp = context.Response;

        try
        {
            string? apiKey = req.Headers[ApiKeyHeader];
            if (string.IsNullOrEmpty(apiKey) || !string.Equals(apiKey, config.ThreatHubApiKey, StringComparison.Ordinal))
            {
                resp.StatusCode = (int)HttpStatusCode.Unauthorized;
                await WriteJsonResponseAsync(resp, new { error = "Unauthorized: Invalid API Key" }).ConfigureAwait(false);
                return;
            }

            string path = req.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;

            if (req.HttpMethod == "POST" && path.EndsWith("/api/threat-hub/sync", StringComparison.OrdinalIgnoreCase))
            {
                using StreamReader reader = new(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                ThreatHubSyncPayload? payload = JsonSerializer.Deserialize<ThreatHubSyncPayload>(body, JsonOptions);

                if (payload == null)
                {
                    resp.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonResponseAsync(resp, new ThreatHubSyncResponse { Success = false, ErrorMessage = "Invalid payload" }).ConfigureAwait(false);
                    return;
                }

                // 更新節點狀態
                string nodeId = string.IsNullOrWhiteSpace(payload.NodeId) ? req.RemoteEndPoint.Address.ToString() : payload.NodeId;
                registeredNodes[nodeId] = new EdgeNodeState(
                    nodeId,
                    payload.NodeName,
                    req.RemoteEndPoint.Address.ToString(),
                    DateTime.UtcNow,
                    payload.NewThreats?.Count ?? 0);

                // 處理新回報的威脅
                if (payload.NewThreats != null)
                {
                    foreach (ThreatIntelligenceItem threat in payload.NewThreats)
                    {
                        if (string.IsNullOrWhiteSpace(threat.SourceIp)) continue;
                        activeThreats[threat.SourceIp.Trim()] = threat;
                        onThreatReceived(threat);
                    }
                }

                ThreatHubSyncResponse syncResp = new()
                {
                    Success = true,
                    ServerTimeUtc = DateTime.UtcNow,
                    ActiveThreats = [.. activeThreats.Values]
                };

                resp.StatusCode = (int)HttpStatusCode.OK;
                await WriteJsonResponseAsync(resp, syncResp).ConfigureAwait(false);
                return;
            }

            resp.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteJsonResponseAsync(resp, new { error = "Endpoint not found" }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logError("Threat Hub request handling failed", ex);
            try
            {
                resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteJsonResponseAsync(resp, new { error = ex.Message }).ConfigureAwait(false);
            }
            catch { }
        }
        finally
        {
            try
            {
                resp.Close();
            }
            catch { }
        }
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object data)
    {
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
    }

    /// <summary>
    /// 停止 HTTP 監聽服務並關閉連線。
    /// </summary>
    public void Stop()
    {
        cts?.Cancel();
        try
        {
            listener?.Stop();
            listener?.Close();
        }
        catch { }
        listener = null;
        cts?.Dispose();
        cts = null;
    }

    /// <summary>
    /// 釋放未受控資源。
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Stop();
        }
    }
}
