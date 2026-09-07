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
    private readonly ThreatHubStore store;
    private readonly bool allowLoopbackHttp;
    private readonly object nodeGate = new();
    private readonly ConcurrentDictionary<string, EdgeNodeState> registeredNodes = new(StringComparer.OrdinalIgnoreCase);

    private HttpListener? listener;
    private BoundedHttpDispatcher? dispatcher;
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
    /// <param name="database">持久化資料庫；測試可省略以使用有限記憶體儲存。</param>
    /// <param name="allowLoopbackHttp">僅供本機測試使用的 HTTP 入口；正式服務固定採 HTTPS。</param>
    public ThreatIntelligenceHubServer(
        IddsConfig config,
        Action<ThreatIntelligenceItem> onThreatReceived,
        Action<string>? logInformation = null,
        Action<string, Exception>? logError = null, Database? database = null, bool allowLoopbackHttp = false)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        store = new ThreatHubStore(database);
        this.allowLoopbackHttp = allowLoopbackHttp;
        this.onThreatReceived = onThreatReceived ?? throw new ArgumentNullException(nameof(onThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logError = logError ?? ((msg, ex) => System.Diagnostics.Trace.TraceError("{0}: {1}", msg, ex.Message));
    }

    /// <summary>
    /// 取得目前中繼中心所維護之全網活動威脅情資清單。
    /// </summary>
    public IReadOnlyList<ThreatIntelligenceItem> ActiveThreats => store.ReadPage(0, string.Empty).ActiveThreats;

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
        if (!AcceptThreat(item)) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("The threat is invalid or the hub capacity has been reached."));
    }

    /// <summary>
    /// 取得伺服器目前是否處於監聽狀態。
    /// </summary>
    public bool IsListening => listener != null && listener.IsListening;

    /// <summary>
    /// 啟動 Threat Hub HTTP 監聽服務。
    /// </summary>
    public void Start()
    {
        if (disposed || listener != null) return;

        if (string.IsNullOrWhiteSpace(config.ThreatHubApiKey)) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat Hub requires an API key."));
        int port = config.ThreatHubPort > 0 ? config.ThreatHubPort : 8443;
        listener = new HttpListener();
        listener.Prefixes.Add(allowLoopbackHttp ? $"http://localhost:{port}/" : $"https://+:{port}/");
        try { listener.Start(); }
        catch { listener.Close(); listener = null; throw; }
        dispatcher = new BoundedHttpDispatcher(HandleRequestAsync);
        cts = new CancellationTokenSource();
        listenTask = ListenLoopAsync(listener, cts.Token);
        logInformation($"Threat Intelligence Hub server started listening on port {port}.");
    }

    private static readonly string[] SuspiciousProbePatterns =
    [
        ".env", "wp-", "admin", "phpmyadmin", "cgi-bin", ".git", "shell", "actuator", "swagger", "api-docs", "console", "solr"
    ];

    private static bool IsSuspiciousProbePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string lower = path.ToLowerInvariant();
        return SuspiciousProbePatterns.Any(pattern => lower.Contains(pattern, StringComparison.Ordinal));
    }

    private async Task ListenLoopAsync(HttpListener httpListener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && httpListener.IsListening)
        {
            try
            {
                HttpListenerContext context = await httpListener.GetContextAsync().ConfigureAwait(false);
                dispatcher?.Submit(context);
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
            string path = req.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(path)) path = "/";
            string method = req.HttpMethod.ToUpperInvariant();

            // 1. 輕量公開健康探針 (Health Probe per RFC 9110 & CNCF Liveness / Readiness Standards)
            if (path is "/" or "/health" or "/healthz")
            {
                if (method != "GET" && method != "HEAD")
                {
                    resp.Headers["Allow"] = "GET, HEAD";
                    resp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonResponseAsync(resp, new { error = "Method Not Allowed" }).ConfigureAwait(false);
                    return;
                }

                resp.StatusCode = (int)HttpStatusCode.OK;
                await WriteJsonResponseAsync(resp, new
                {
                    status = "online",
                    timestampUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
                return;
            }

            // 2. 核心威脅情資同步端點 (/api/threat-hub/sync)
            if (path.Equals("/api/threat-hub/sync", StringComparison.OrdinalIgnoreCase))
            {
                string? apiKey = req.Headers[ApiKeyHeader];
                if (string.IsNullOrWhiteSpace(config.ThreatHubApiKey) || string.IsNullOrEmpty(apiKey) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)), System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(config.ThreatHubApiKey))))
                {
                    resp.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await WriteJsonResponseAsync(resp, new { error = "Unauthorized" }).ConfigureAwait(false);
                    return;
                }

                if (method != "POST")
                {
                    // RFC 9110 §15.5.6: 405 Method Not Allowed MUST include Allow header
                    resp.Headers["Allow"] = "POST";
                    resp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonResponseAsync(resp, new { error = "Method Not Allowed" }).ConfigureAwait(false);
                    return;
                }


                string body = await BoundedHttpDispatcher.ReadBodyAsync(req, 1024 * 1024).ConfigureAwait(false);
                ThreatHubSyncPayload? payload = JsonSerializer.Deserialize<ThreatHubSyncPayload>(body, JsonOptions);

                if (payload == null || payload.NewThreats is null || payload.NewThreats.Count > 256 || payload.NodeId?.Length > 128 || payload.NodeName?.Length > 128 || payload.Generation?.Length > 128)
                {
                    resp.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonResponseAsync(resp, new ThreatHubSyncResponse { Success = false, ErrorMessage = "Invalid payload" }).ConfigureAwait(false);
                    return;
                }

                string nodeId = string.IsNullOrWhiteSpace(payload.NodeId) ? req.RemoteEndPoint.Address.ToString() : payload.NodeId;
                lock (nodeGate)
                {
                    foreach (var node in registeredNodes.Where(p => p.Value.LastSeenUtc < DateTime.UtcNow.AddHours(-1)).ToArray()) registeredNodes.TryRemove(node.Key, out _);
                    if (!registeredNodes.ContainsKey(nodeId) && registeredNodes.Count >= 1024) { resp.StatusCode = 503; return; }
                    registeredNodes[nodeId] = new EdgeNodeState(nodeId, payload.NodeName ?? string.Empty, req.RemoteEndPoint.Address.ToString(), DateTime.UtcNow, payload.NewThreats.Count);
                }
                foreach (ThreatIntelligenceItem threat in payload.NewThreats)
                {
                    if (!IsValidThreat(threat)) continue;
                    if (!AcceptThreat(threat)) { resp.StatusCode = 503; return; }
                    threat.SourceIp = IpAddressCanonicalizer.Canonicalize(threat.SourceIp);
                    DateTime maximumExpiry = threat.ReportedUtc.AddDays(Math.Clamp(config.ThreatFeedTtlDays, 1, 365));
                    if (threat.ExpiresUtc > maximumExpiry) threat.ExpiresUtc = maximumExpiry;
                    onThreatReceived(threat);
                }
                ThreatHubSyncResponse syncResp = store.ReadPage(payload.Cursor, payload.Generation ?? string.Empty);
                resp.StatusCode = (int)HttpStatusCode.OK;
                await WriteJsonResponseAsync(resp, syncResp).ConfigureAwait(false);
                return;
            }

            // 3. 惡意路徑探測檢測與資安記錄 (Scan-to-Ban 防禦陷阱)
            if (IsSuspiciousProbePath(path))
            {
                logInformation($"Threat Hub probe detected from {req.RemoteEndPoint.Address}: {path}");
                try
                {
                    if (!IPAddress.IsLoopback(req.RemoteEndPoint.Address))
                    {
                        string clientIp = req.RemoteEndPoint.Address.ToString();
                        IntrusionLog.AddEntry(DateTime.UtcNow, WellKnownAgentIds.ClusterThreatHub, clientIp, IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);
                    }
                }
                catch { }
            }

            // 4. 其他未定義端點
            resp.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteJsonResponseAsync(resp, new { error = "Not Found" }).ConfigureAwait(false);
        }
        catch (RequestBodyTooLargeException) { resp.StatusCode = 413; }
        catch (JsonException) { resp.StatusCode = 400; }
        catch (Exception ex)
        {
            logError("Threat Hub request handling failed", ex);
            try
            {
                resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteJsonResponseAsync(resp, new { error = "Internal Server Error" }).ConfigureAwait(false);
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

    private bool IsValidThreat(ThreatIntelligenceItem? item) => item is not null
        && IPAddress.TryParse(item.SourceIp, out var ip) && !BogonIpFilter.IsBogonOrReserved(ip)
        && !config.IsInSafeNetwork(ip.ToString())
        && double.IsFinite(item.ConfidenceScore) && item.ConfidenceScore >= 0.8 && item.ConfidenceScore <= 1
        && item.ExpiresUtc > DateTime.UtcNow && item.ReportedUtc <= DateTime.UtcNow.AddMinutes(5)
        && item.ReportedUtc >= DateTime.UtcNow.AddDays(-Math.Clamp(config.ThreatFeedTtlDays, 1, 365))
        && item.Notes?.Length <= 1024 && item.ThreatCategory?.Length <= 128
        && item.ReporterNodeId?.Length <= 128 && item.ReporterNodeName?.Length <= 128;

    private bool AcceptThreat(ThreatIntelligenceItem item)
    {
        if (!IsValidThreat(item)) return false;
        ThreatIntelligenceItem normalized = JsonSerializer.Deserialize<ThreatIntelligenceItem>(JsonSerializer.Serialize(item))!;
        normalized.SourceIp = IpAddressCanonicalizer.Canonicalize(item.SourceIp);
        DateTime maximumExpiry = item.ReportedUtc.AddDays(Math.Clamp(config.ThreatFeedTtlDays, 1, 365));
        if (normalized.ExpiresUtc > maximumExpiry) normalized.ExpiresUtc = maximumExpiry;
        if (normalized.ExpiresUtc <= DateTime.UtcNow) return false;
        return store.Upsert(normalized);
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object data)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
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
        dispatcher?.Dispose();
        dispatcher = null;
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
