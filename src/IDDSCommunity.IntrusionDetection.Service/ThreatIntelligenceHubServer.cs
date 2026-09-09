using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Security;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供集中式威脅情資中繼中心（Threat Hub）之輕量級 HTTP API 服務端點。
/// </summary>
internal sealed class ThreatIntelligenceHubServer : IDisposable
{
    private const string ApiKeyHeader = "X-IDDS-ThreatHub-ApiKey";
    private readonly FailedAttemptsRateLimiter authRateLimiter = new(maxFailedAttempts: 10, windowDuration: TimeSpan.FromMinutes(15), lockDuration: TimeSpan.FromMinutes(15));
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
        ConfigureHttpTimeouts(listener);
        listener.Prefixes.Add(allowLoopbackHttp ? $"http://localhost:{port}/" : $"https://+:{port}/");
        try { listener.Start(); }
        catch (Exception ex)
        {
            listener.Close();
            listener = null;
            if (!allowLoopbackHttp)
            {
                logError($"Threat Hub failed to start HTTPS listener on port {port}. Ensure a TLS certificate is bound using 'netsh http add sslcert ipport=0.0.0.0:{port} certhash=<THUMBPRINT> appid={Guid.NewGuid():B}'.", ex);
            }
            throw;
        }
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

            // 1. 輕量儀表板端點 (GET /dashboard)：免驗證，頁面本身不包含敏感資料
            if (path.Equals("/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                if (method != "GET" && method != "HEAD")
                {
                    resp.Headers["Allow"] = "GET, HEAD";
                    resp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonResponseAsync(resp, new { error = "Method Not Allowed" }).ConfigureAwait(false);
                    return;
                }

                resp.StatusCode = (int)HttpStatusCode.OK;
                string language = ResolveDashboardLanguage(req.QueryString["lang"], req.Headers["Accept-Language"]);
                await WriteDashboardHtmlAsync(resp, method, language).ConfigureAwait(false);
                return;
            }

            // 2. 輕量公開健康探針 (Health Probe per RFC 9110 & CNCF Liveness / Readiness Standards)
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

            // 3. 核心威脅情資同步端點 (/api/threat-hub/sync)
            if (path.Equals("/api/threat-hub/sync", StringComparison.OrdinalIgnoreCase))
            {
                string clientIp = req.RemoteEndPoint?.Address != null
                    ? IpAddressCanonicalizer.Canonicalize(req.RemoteEndPoint.Address).ToString()
                    : "unknown";

                if (authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
                {
                    resp.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    resp.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await WriteJsonResponseAsync(resp, new { error = "Too Many Requests" }).ConfigureAwait(false);
                    return;
                }

                string? apiKey = req.Headers[ApiKeyHeader];
                if (string.IsNullOrWhiteSpace(config.ThreatHubApiKey) || string.IsNullOrEmpty(apiKey) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)), System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(config.ThreatHubApiKey))))
                {
                    authRateLimiter.RecordFailedAttempt(clientIp);
                    resp.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await WriteJsonResponseAsync(resp, new { error = "Unauthorized" }).ConfigureAwait(false);
                    return;
                }

                authRateLimiter.Reset(clientIp);

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

                string nodeId = string.IsNullOrWhiteSpace(payload.NodeId) ? clientIp : payload.NodeId;
                lock (nodeGate)
                {
                    foreach (var node in registeredNodes.Where(p => p.Value.LastSeenUtc < DateTime.UtcNow.AddHours(-1)).ToArray()) registeredNodes.TryRemove(node.Key, out _);
                    if (!registeredNodes.ContainsKey(nodeId) && registeredNodes.Count >= 1024) { resp.StatusCode = 503; return; }
                    registeredNodes[nodeId] = new EdgeNodeState(nodeId, payload.NodeName ?? string.Empty, clientIp, DateTime.UtcNow, payload.NewThreats.Count);
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

            // 4. Hub 節點狀態 API 端點 (GET /api/threat-hub/nodes)
            if (path.Equals("/api/threat-hub/nodes", StringComparison.OrdinalIgnoreCase))
            {
                string clientIp = req.RemoteEndPoint?.Address != null
                    ? IpAddressCanonicalizer.Canonicalize(req.RemoteEndPoint.Address).ToString()
                    : "unknown";

                if (authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
                {
                    resp.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    resp.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await WriteJsonResponseAsync(resp, new { error = "Too Many Requests" }).ConfigureAwait(false);
                    return;
                }

                string? apiKey = req.Headers[ApiKeyHeader];
                if (string.IsNullOrWhiteSpace(config.ThreatHubApiKey) || string.IsNullOrEmpty(apiKey) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)), System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(config.ThreatHubApiKey))))
                {
                    authRateLimiter.RecordFailedAttempt(clientIp);
                    resp.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await WriteJsonResponseAsync(resp, new { error = "Unauthorized" }).ConfigureAwait(false);
                    return;
                }

                authRateLimiter.Reset(clientIp);

                if (method != "GET" && method != "HEAD")
                {
                    resp.Headers["Allow"] = "GET, HEAD";
                    resp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonResponseAsync(resp, new { error = "Method Not Allowed" }).ConfigureAwait(false);
                    return;
                }

                IReadOnlyList<EdgeNodeState> nodes = RegisteredNodes;
                int activeThreatCount = store.ReadPage(0, string.Empty).ActiveThreats.Count;
                resp.StatusCode = (int)HttpStatusCode.OK;
                await WriteJsonResponseAsync(resp, new
                {
                    generatedUtc = DateTime.UtcNow,
                    totalActiveThreatCount = activeThreatCount,
                    nodes = nodes.Select(n => new
                    {
                        nodeId = n.NodeId,
                        nodeName = n.NodeName,
                        nodeIp = n.NodeIp,
                        lastSeenUtc = n.LastSeenUtc,
                        reportedThreatCount = n.ReportedThreatCount
                    }).ToList()
                }).ConfigureAwait(false);
                return;
            }

            // 5. 惡意路徑探測檢測與資安記錄 (Scan-to-Ban 防禦陷阱)
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

            // 6. 其他未定義端點
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

    private static void ConfigureHttpTimeouts(HttpListener listener)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                listener.TimeoutManager.HeaderWait = TimeSpan.FromSeconds(15);
                listener.TimeoutManager.EntityBody = TimeSpan.FromSeconds(15);
                listener.TimeoutManager.DrainEntityBody = TimeSpan.FromSeconds(15);
            }
            catch (Exception) { }
        }
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object data)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.ContentType = "application/json; charset=utf-8";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
    }

    /// <summary>
    /// 將內嵌單頁儀表板 HTML 寫入 HTTP 回應，並設定完整安全標頭。
    /// </summary>
    /// <param name="response">HTTP 回應物件。</param>
    /// <param name="method">HTTP 方法字串（HEAD 方法不回傳主體）。</param>
    /// <param name="language">已正規化的儀表板語言標籤。</param>
    private static async Task WriteDashboardHtmlAsync(HttpListenerResponse response, string method, string language)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(BuildDashboardHtml(language));
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self';";
        response.Headers["Content-Language"] = language;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        if (method != "HEAD")
        {
            await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 依查詢參數與 Accept-Language 標頭選擇儀表板支援的語言。
    /// </summary>
    /// <param name="requestedLanguage">查詢參數指定的語言。</param>
    /// <param name="acceptLanguage">瀏覽器傳入的 Accept-Language 標頭。</param>
    /// <returns>正規化為 zh-Hant-TW 或 en-US 的語言標籤。</returns>
    internal static string ResolveDashboardLanguage(string? requestedLanguage, string? acceptLanguage)
    {
        string? requested = NormalizeDashboardLanguage(requestedLanguage);
        if (requested is not null)
            return requested;

        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            IEnumerable<(string Tag, double Quality, int Index)> candidates = acceptLanguage.Split(',')
                .Select((item, index) =>
                {
                    string[] parts = item.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    double quality = 1D;
                    foreach (string parameter in parts.Skip(1))
                    {
                        if (parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                            double.TryParse(parameter.AsSpan(2), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsed))
                        {
                            quality = Math.Clamp(parsed, 0D, 1D);
                        }
                    }
                    return (Tag: parts.Length > 0 ? parts[0] : string.Empty, Quality: quality, Index: index);
                })
                .Where(candidate => candidate.Quality > 0D)
                .OrderByDescending(candidate => candidate.Quality)
                .ThenBy(candidate => candidate.Index);

            foreach ((string Tag, double Quality, int Index) candidate in candidates)
            {
                string? normalized = NormalizeDashboardLanguage(candidate.Tag);
                if (normalized is not null)
                    return normalized;
            }
        }

        return "en-US";
    }

    private static string? NormalizeDashboardLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "zh-Hant-TW";
        if (language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return "en-US";
        return null;
    }

    /// <summary>
    /// 建立指定語言的 Threat Hub 儀表板 HTML。
    /// </summary>
    /// <param name="language">正規化的語言標籤。</param>
    /// <returns>完整的儀表板 HTML。</returns>
    internal static string BuildDashboardHtml(string language)
    {
        DashboardText text = string.Equals(language, "zh-Hant-TW", StringComparison.OrdinalIgnoreCase)
            ? DashboardText.TraditionalChinese
            : DashboardText.English;
        string html = DashboardHtml.Replace("{{LANG}}", text.Language, StringComparison.Ordinal);
        foreach ((string token, string value) in text.Replacements)
            html = html.Replace("{{" + token + "}}", value, StringComparison.Ordinal);
        return html;
    }

    private sealed record DashboardText(string Language, IReadOnlyList<(string Token, string Value)> Replacements)
    {
        internal static DashboardText TraditionalChinese { get; } = new("zh-Hant-TW",
        [
            ("TITLE", "IDDS Community - Threat Hub 儀表板"), ("OFFLINE", "離線"),
            ("API_KEY_PLACEHOLDER", "輸入 API Key..."), ("APPLY", "套用"),
            ("CONNECTED_NODES", "連線節點數"), ("ACTIVE_THREATS", "全網活動威脅情資"),
            ("LAST_UPDATED", "最後更新時間"), ("EDGE_NODES", "邊緣節點清單"),
            ("STATUS", "狀態"), ("NODE_ID", "節點 ID"), ("NODE_NAME", "節點名稱"),
            ("SOURCE_IP", "來源 IP"), ("LAST_HEARTBEAT", "最後心跳"),
            ("REPORTED_THREATS", "回報情資數"), ("ENTER_KEY", "請輸入 API Key 後載入資料"),
            ("TOO_MANY_REQUESTS", "請求頻率過高，請稍後再試。"),
            ("INVALID_KEY", "API Key 驗證失敗，請確認後重新輸入。"),
            ("SERVER_ERROR", "伺服器回傳錯誤：HTTP "), ("ONLINE", "線上"),
            ("REFRESH_PREFIX", "自動每 30 秒更新 · 最後更新："),
            ("NO_NODES", "目前沒有已連線的邊緣節點"), ("DELAYED", "延遲"),
            ("UNNAMED", "（未命名）"), ("UNKNOWN_ERROR", "未知錯誤"),
            ("CONNECTION_ERROR", "無法連線至 Threat Hub：")
        ]);

        internal static DashboardText English { get; } = new("en-US",
        [
            ("TITLE", "IDDS Community - Threat Hub Dashboard"), ("OFFLINE", "Offline"),
            ("API_KEY_PLACEHOLDER", "Enter API Key..."), ("APPLY", "Apply"),
            ("CONNECTED_NODES", "Connected nodes"), ("ACTIVE_THREATS", "Active global threats"),
            ("LAST_UPDATED", "Last updated"), ("EDGE_NODES", "Edge nodes"),
            ("STATUS", "Status"), ("NODE_ID", "Node ID"), ("NODE_NAME", "Node name"),
            ("SOURCE_IP", "Source IP"), ("LAST_HEARTBEAT", "Last heartbeat"),
            ("REPORTED_THREATS", "Reported threats"), ("ENTER_KEY", "Enter an API Key to load data"),
            ("TOO_MANY_REQUESTS", "Too many requests. Try again later."),
            ("INVALID_KEY", "API Key authentication failed. Check the key and try again."),
            ("SERVER_ERROR", "Server returned an error: HTTP "), ("ONLINE", "Online"),
            ("REFRESH_PREFIX", "Refreshes every 30 seconds · Last updated: "),
            ("NO_NODES", "No edge nodes are connected"), ("DELAYED", "Delayed"),
            ("UNNAMED", "(unnamed)"), ("UNKNOWN_ERROR", "Unknown error"),
            ("CONNECTION_ERROR", "Unable to connect to Threat Hub: ")
        ]);
    }

    private const string DashboardHtml = """
        <!DOCTYPE html>
        <html lang="{{LANG}}">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{TITLE}}</title>
          <style>
            * { box-sizing: border-box; margin: 0; padding: 0; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; }
            body { background-color: #0f172a; color: #f8fafc; min-height: 100vh; padding: 24px; }
            h1 { font-size: 22px; font-weight: 700; color: #14b8a6; }
            h2 { font-size: 14px; font-weight: 600; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 12px; }
            .top-bar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 28px; flex-wrap: wrap; gap: 12px; }
            .top-bar-left { display: flex; align-items: center; gap: 14px; }
            .badge { font-size: 12px; padding: 3px 10px; border-radius: 999px; font-weight: 600; }
            .badge-online { background: #064e3b; color: #6ee7b7; }
            .badge-offline { background: #7f1d1d; color: #fca5a5; }
            .key-row { display: flex; gap: 8px; align-items: center; }
            .language-select { background: #1e293b; border: 1px solid #334155; color: #f1f5f9; padding: 7px 10px; border-radius: 6px; font-size: 13px; }
            .key-row input { background: #1e293b; border: 1px solid #334155; color: #f1f5f9; padding: 7px 12px; border-radius: 6px; font-size: 13px; width: 300px; font-family: monospace; }
            .key-row input:focus { outline: none; border-color: #14b8a6; }
            .key-row button { background: #14b8a6; color: #0f172a; border: none; border-radius: 6px; padding: 7px 16px; font-size: 13px; font-weight: 700; cursor: pointer; transition: background 0.15s; }
            .key-row button:hover { background: #0d9488; }
            .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }
            .card { background: #1e293b; border-radius: 10px; border: 1px solid #334155; padding: 20px; }
            .stat-value { font-size: 36px; font-weight: 800; color: #f8fafc; margin-bottom: 4px; }
            .stat-label { font-size: 13px; color: #64748b; }
            table { width: 100%; border-collapse: collapse; font-size: 13px; }
            th { text-align: left; color: #64748b; font-weight: 600; font-size: 11px; text-transform: uppercase; letter-spacing: 0.04em; padding: 8px 12px; border-bottom: 1px solid #334155; }
            td { padding: 10px 12px; border-bottom: 1px solid #1e293b; color: #cbd5e1; vertical-align: middle; }
            tr:last-child td { border-bottom: none; }
            tr:hover td { background: #1e293b44; }
            .node-id { font-family: monospace; color: #38bdf8; font-size: 12px; }
            .status-dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 6px; }
            .dot-green { background: #22c55e; }
            .dot-yellow { background: #eab308; }
            .dot-gray { background: #475569; }
            .empty { color: #475569; text-align: center; padding: 32px; font-size: 13px; }
            .error-bar { background: #7f1d1d22; border: 1px solid #7f1d1d; border-radius: 6px; color: #fca5a5; padding: 10px 14px; font-size: 13px; margin-bottom: 16px; display: none; }
            .refresh-info { font-size: 11px; color: #475569; text-align: right; margin-top: 8px; }
            .table-wrap { background: #1e293b; border-radius: 10px; border: 1px solid #334155; overflow: hidden; }
          </style>
        </head>
        <body>
          <div class="top-bar">
            <div class="top-bar-left">
              <h1>&#x1F6E1;&#xFE0F; IDDS Community</h1>
              <span id="hub-status" class="badge badge-offline">{{OFFLINE}}</span>
            </div>
            <div class="key-row">
              <select id="language-select" class="language-select" aria-label="Language">
                <option value="zh-Hant-TW">繁體中文</option>
                <option value="en-US">English</option>
              </select>
              <input type="password" id="api-key" placeholder="{{API_KEY_PLACEHOLDER}}" autocomplete="off" />
              <button onclick="applyKey()">{{APPLY}}</button>
            </div>
          </div>

          <div id="error-bar" class="error-bar"></div>

          <div class="cards">
            <div class="card">
              <div class="stat-value" id="stat-nodes">—</div>
              <div class="stat-label">{{CONNECTED_NODES}}</div>
            </div>
            <div class="card">
              <div class="stat-value" id="stat-threats">—</div>
              <div class="stat-label">{{ACTIVE_THREATS}}</div>
            </div>
            <div class="card">
              <div class="stat-value" id="stat-updated" style="font-size:18px;padding-top:8px;">—</div>
              <div class="stat-label">{{LAST_UPDATED}}</div>
            </div>
          </div>

          <h2>{{EDGE_NODES}}</h2>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>{{STATUS}}</th>
                  <th>{{NODE_ID}}</th>
                  <th>{{NODE_NAME}}</th>
                  <th>{{SOURCE_IP}}</th>
                  <th>{{LAST_HEARTBEAT}}</th>
                  <th>{{REPORTED_THREATS}}</th>
                </tr>
              </thead>
              <tbody id="node-tbody">
                <tr><td colspan="6" class="empty">{{ENTER_KEY}}</td></tr>
              </tbody>
            </table>
          </div>
          <div class="refresh-info" id="refresh-info"></div>

          <script>
            var currentKey = sessionStorage.getItem('idds_hub_key') || '';
            var refreshTimer = null;
            var currentLanguage = '{{LANG}}';
            var savedLanguage = sessionStorage.getItem('idds_hub_language');

            if (!new URL(window.location.href).searchParams.has('lang') && savedLanguage && savedLanguage !== currentLanguage) {
              var savedUrl = new URL(window.location.href);
              savedUrl.searchParams.set('lang', savedLanguage);
              window.location.replace(savedUrl.toString());
            }

            document.getElementById('api-key').value = currentKey ? '••••••••' : '';
            document.getElementById('language-select').value = currentLanguage;
            document.getElementById('language-select').addEventListener('change', function(event) {
              var selected = event.target.value;
              sessionStorage.setItem('idds_hub_language', selected);
              var url = new URL(window.location.href);
              url.searchParams.set('lang', selected);
              window.location.assign(url.toString());
            });

            function applyKey() {
              var input = document.getElementById('api-key').value.trim();
              if (input && input !== '••••••••') {
                currentKey = input;
                sessionStorage.setItem('idds_hub_key', currentKey);
              }
              fetchData();
            }

            function statusClass(lastSeen) {
              var diff = (Date.now() - new Date(lastSeen).getTime()) / 1000;
              if (diff <= 90) return 'dot-green';
              if (diff <= 300) return 'dot-yellow';
              return 'dot-gray';
            }

            function fmtLocal(iso) {
              try { return new Date(iso).toLocaleString('{{LANG}}', { hour12: false }); } catch(e) { return iso; }
            }

            function showError(msg) {
              var bar = document.getElementById('error-bar');
              bar.textContent = msg;
              bar.style.display = 'block';
            }

            function hideError() {
              document.getElementById('error-bar').style.display = 'none';
            }

            function fetchData() {
              if (!currentKey) { return; }
              fetch('/api/threat-hub/nodes', {
                method: 'GET',
                headers: { 'X-IDDS-ThreatHub-ApiKey': currentKey }
              })
              .then(function(r) {
                if (r.status === 429) { showError('{{TOO_MANY_REQUESTS}}'); return null; }
                if (r.status === 401) { showError('{{INVALID_KEY}}'); return null; }
                if (!r.ok) { showError('{{SERVER_ERROR}}' + r.status); return null; }
                return r.json();
              })
              .then(function(data) {
                if (!data) return;
                hideError();
                document.getElementById('hub-status').textContent = '{{ONLINE}}';
                document.getElementById('hub-status').className = 'badge badge-online';
                document.getElementById('stat-nodes').textContent = data.nodes ? data.nodes.length : 0;
                document.getElementById('stat-threats').textContent = data.totalActiveThreatCount ?? '—';
                document.getElementById('stat-updated').textContent = fmtLocal(data.generatedUtc);
                document.getElementById('refresh-info').textContent = '{{REFRESH_PREFIX}}' + fmtLocal(data.generatedUtc);

                var tbody = document.getElementById('node-tbody');
                tbody.replaceChildren();
                if (!data.nodes || data.nodes.length === 0) {
                  var emptyTr = document.createElement('tr');
                  var emptyTd = document.createElement('td');
                  emptyTd.colSpan = 6;
                  emptyTd.className = 'empty';
                  emptyTd.textContent = '{{NO_NODES}}';
                  emptyTr.appendChild(emptyTd);
                  tbody.appendChild(emptyTr);
                  return;
                }
                data.nodes.forEach(function(n) {
                  var sc = statusClass(n.lastSeenUtc);
                  var tr = document.createElement('tr');

                  // 狀態欄
                  var tdStatus = document.createElement('td');
                  var dot = document.createElement('span');
                  dot.className = 'status-dot ' + sc;
                  tdStatus.appendChild(dot);
                  tdStatus.appendChild(document.createTextNode(
                    sc === 'dot-green' ? '{{ONLINE}}' : sc === 'dot-yellow' ? '{{DELAYED}}' : '{{OFFLINE}}'
                  ));

                  // 節點 ID（前 8 碼）
                  var tdId = document.createElement('td');
                  tdId.className = 'node-id';
                  tdId.textContent = (n.nodeId || '').substring(0, 8);

                  // 節點名稱
                  var tdName = document.createElement('td');
                  tdName.textContent = (n.nodeName || '') || '{{UNNAMED}}';

                  // 來源 IP
                  var tdIp = document.createElement('td');
                  tdIp.className = 'node-id';
                  tdIp.textContent = n.nodeIp || '';

                  // 最後心跳
                  var tdSeen = document.createElement('td');
                  tdSeen.textContent = fmtLocal(n.lastSeenUtc);

                  // 回報情資數
                  var tdCount = document.createElement('td');
                  tdCount.textContent = n.reportedThreatCount ?? 0;

                  tr.appendChild(tdStatus);
                  tr.appendChild(tdId);
                  tr.appendChild(tdName);
                  tr.appendChild(tdIp);
                  tr.appendChild(tdSeen);
                  tr.appendChild(tdCount);
                  tbody.appendChild(tr);
                });
              })
              .catch(function(err) {
                document.getElementById('hub-status').textContent = '{{OFFLINE}}';
                document.getElementById('hub-status').className = 'badge badge-offline';
                var errMsg = document.createElement('span');
                errMsg.textContent = err.message || '{{UNKNOWN_ERROR}}';
                var bar = document.getElementById('error-bar');
                bar.replaceChildren(document.createTextNode('{{CONNECTION_ERROR}}'), errMsg);
                bar.style.display = 'block';
              });
            }

            function scheduleRefresh() {
              if (refreshTimer) clearInterval(refreshTimer);
              refreshTimer = setInterval(fetchData, 30000);
            }

            if (currentKey) { fetchData(); }
            scheduleRefresh();
          </script>
        </body>
        </html>
        """;

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
