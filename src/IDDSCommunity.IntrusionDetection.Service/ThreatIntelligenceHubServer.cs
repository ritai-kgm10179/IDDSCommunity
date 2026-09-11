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
    private readonly DateTime serverStartTimeUtc = DateTime.UtcNow;
    private readonly Database? database;
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
        this.database = database;
        store = new ThreatHubStore(database);
        this.allowLoopbackHttp = allowLoopbackHttp;
        this.onThreatReceived = onThreatReceived ?? throw new ArgumentNullException(nameof(onThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logError = logError ?? ((msg, ex) => System.Diagnostics.Trace.TraceError("{0}: {1}", msg, ex.Message));
    }

    private (int activeBlocks, int probationCount) GetLocalDefenseMetrics()
    {
        int activeBlocks = 0;
        try { activeBlocks = Locks.GetActiveLocks().Count; }
        catch { }

        int probationCount = 0;
        try
        {
            if (database != null)
            {
                object? res = database.ExecuteScalar("select count(*) from Locks where status = @p0", Shared.Lock.LOCK_STATUS_PROBATION);
                if (res != null && int.TryParse(res.ToString(), out int cnt))
                    probationCount = cnt;
            }
        }
        catch { }

        return (activeBlocks, probationCount);
    }

    private async Task HandleAuthFailureAsync(HttpListenerRequest req, HttpListenerResponse resp, string clientIp)
    {
        int failures = authRateLimiter.RecordFailedAttempt(clientIp);
        if (failures >= 3)
        {
            int delayMs = Math.Min(500 * (1 << Math.Min(failures - 3, 3)), 3000);
            await Task.Delay(delayMs).ConfigureAwait(false);
        }
        if (failures >= 10)
        {
            try
            {
                if (req.RemoteEndPoint?.Address != null && !IPAddress.IsLoopback(req.RemoteEndPoint.Address))
                {
                    IntrusionLog.AddEntry(DateTime.UtcNow, WellKnownAgentIds.ClusterThreatHub, clientIp, IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);
                }
            }
            catch { }
        }
        resp.StatusCode = (int)HttpStatusCode.Unauthorized;
        await WriteJsonResponseAsync(resp, new { error = "Unauthorized" }).ConfigureAwait(false);
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
        bool useReverseProxy = allowLoopbackHttp || config.ThreatHubUseReverseProxy;
        bool loopbackOnly = allowLoopbackHttp || config.ThreatHubReverseProxyLoopbackOnly;
        listener.Prefixes.Add(useReverseProxy
            ? loopbackOnly ? $"http://localhost:{port}/" : $"http://+:{port}/"
            : $"https://+:{port}/");
        try { listener.Start(); }
        catch (Exception ex)
        {
            listener.Close();
            listener = null;
            if (!useReverseProxy)
            {
                logError($"Threat Hub failed to start HTTPS listener on port {port}. Ensure a TLS certificate is bound using 'netsh http add sslcert ipport=0.0.0.0:{port} certhash=<THUMBPRINT> appid={Guid.NewGuid():B}'.", ex);
            }
            throw;
        }
        dispatcher = new BoundedHttpDispatcher(HandleRequestAsync);
        cts = new CancellationTokenSource();
        listenTask = ListenLoopAsync(listener, cts.Token);
        logInformation(useReverseProxy
            ? $"Threat Intelligence Hub server started on {(loopbackOnly ? "loopback" : "all interfaces")} HTTP port {port}; TLS must be terminated by a trusted reverse proxy."
            : $"Threat Intelligence Hub server started listening on HTTPS port {port}.");
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
                    await HandleAuthFailureAsync(req, resp, clientIp).ConfigureAwait(false);
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
                    await HandleAuthFailureAsync(req, resp, clientIp).ConfigureAwait(false);
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
                int activeThreatCount = store.ActiveThreatCount;
                (int localBlocks, int localProbation) = GetLocalDefenseMetrics();
                int port = config.ThreatHubPort > 0 ? config.ThreatHubPort : 8443;
                bool useReverseProxy = allowLoopbackHttp || config.ThreatHubUseReverseProxy;
                string listenMode = useReverseProxy ? $"HTTP (Proxy):{port}" : $"HTTPS:{port}";
                double uptimeSeconds = (DateTime.UtcNow - serverStartTimeUtc).TotalSeconds;

                resp.StatusCode = (int)HttpStatusCode.OK;
                await WriteJsonResponseAsync(resp, new
                {
                    generatedUtc = DateTime.UtcNow,
                    totalActiveThreatCount = activeThreatCount,
                    hub = new
                    {
                        hostName = Environment.MachineName,
                        version = typeof(ThreatIntelligenceHubServer).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                        uptimeSeconds = Math.Round(uptimeSeconds, 0),
                        listenMode = listenMode,
                        activeThreatCount = activeThreatCount,
                        maxThreatCapacity = ThreatHubStore.MaximumEntries,
                        generation = store.Generation,
                        threatTtlDays = Math.Clamp(config.ThreatFeedTtlDays, 1, 365),
                        localActiveBlocks = localBlocks,
                        localProbationCount = localProbation
                    },
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
        // 每次回應皆產生獨立、不可預測的 128 位元隨機值作為 CSP nonce，取代 'unsafe-inline'。
        // nonce 絕不可重複使用於下一次回應，否則將失去其防止注入腳本被瀏覽器信任執行的效果。
        string nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(BuildDashboardHtml(language, nonce));
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["Content-Security-Policy"] = $"default-src 'none'; style-src 'nonce-{nonce}'; script-src 'nonce-{nonce}'; img-src data:; connect-src 'self';";
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
    /// <param name="nonce">本次回應專屬的 CSP nonce，用於內嵌 &lt;style&gt;/&lt;script&gt; 標籤。</param>
    /// <returns>完整的儀表板 HTML。</returns>
    internal static string BuildDashboardHtml(string language, string nonce)
    {
        DashboardText text = string.Equals(language, "zh-Hant-TW", StringComparison.OrdinalIgnoreCase)
            ? DashboardText.TraditionalChinese
            : DashboardText.English;
        string html = DashboardHtml
            .Replace("{{LANG}}", text.Language, StringComparison.Ordinal)
            .Replace("{{NONCE}}", nonce, StringComparison.Ordinal);
        foreach ((string token, string value) in text.Replacements)
            html = html.Replace("{{" + token + "}}", value, StringComparison.Ordinal);
        return html;
    }

    private sealed record DashboardText(string Language, IReadOnlyList<(string Token, string Value)> Replacements)
    {
        internal static DashboardText TraditionalChinese { get; } = new("zh-Hant-TW",
        [
            ("BRAND_NAME", "IDDS 社群版"),
            ("TITLE", "IDDS 社群版 - 威脅情資中繼中心儀表板"), ("OFFLINE", "離線"),
            ("ONLINE", "線上"), ("DELAYED", "延遲"),
            ("API_KEY_PLACEHOLDER", "輸入 API Key..."), ("APPLY", "套用"),
            ("AUTHENTICATED", "已認證"), ("LOGOUT", "登出"),
            ("CONNECTED_NODES", "連線節點數"), ("ACTIVE_THREATS", "全網活動威脅情資"),
            ("HUB_DEFENSE", "Hub 本機防護"), ("HUB_UPTIME", "Hub 運行時間"),
            ("LAST_UPDATED", "最後更新時間"),
            ("HUB_OVERVIEW", "威脅情資中繼中心系統運作概況"),
            ("HUB_HOST", "主機名稱"), ("HUB_VERSION", "軟體版本"),
            ("HUB_ENDPOINT", "監聽端點"), ("HUB_GENERATION", "情資世代"),
            ("HUB_TTL", "情資保留天數"),
            ("EDGE_NODES", "邊緣節點清單"),
            ("STATUS", "狀態"), ("NODE_ID", "節點 ID"), ("NODE_NAME", "節點名稱"),
            ("SOURCE_IP", "來源 IP"), ("LAST_HEARTBEAT", "最後心跳"),
            ("REPORTED_THREATS", "回報情資數"), ("ENTER_KEY", "請輸入 API Key 後載入資料"),
            ("TOO_MANY_REQUESTS", "請求頻率過高，請稍候重試："),
            ("INVALID_KEY", "API Key 驗證失敗或已過期，請重新輸入。"),
            ("SERVER_ERROR", "伺服器回傳錯誤：HTTP "),
            ("REFRESH_PREFIX", "自動每 30 秒更新 · 最後更新："),
            ("NO_NODES", "目前沒有已連線的邊緣節點"),
            ("UNNAMED", "（未命名）"), ("UNKNOWN_ERROR", "未知錯誤"),
            ("CONNECTION_ERROR", "無法連線至威脅情資中繼中心："),
            ("THEME_LABEL", "佈景主題"), ("THEME_AUTO", "自動"),
            ("THEME_LIGHT", "淺色"), ("THEME_DARK", "深色"),
            ("DAYS", "天"), ("HOURS", "小時"),
            ("MINUTES", "分"), ("SECONDS", "秒"),
            ("BLOCKS", "項封鎖"), ("PROBATION", "個假釋中")
        ]);

        internal static DashboardText English { get; } = new("en-US",
        [
            ("BRAND_NAME", "IDDS Community"),
            ("TITLE", "IDDS Community - Threat Hub Dashboard"), ("OFFLINE", "Offline"),
            ("ONLINE", "Online"), ("DELAYED", "Delayed"),
            ("API_KEY_PLACEHOLDER", "Enter API Key..."), ("APPLY", "Apply"),
            ("AUTHENTICATED", "Authenticated"), ("LOGOUT", "Logout"),
            ("CONNECTED_NODES", "Connected nodes"), ("ACTIVE_THREATS", "Active global threats"),
            ("HUB_DEFENSE", "Hub local defense"), ("HUB_UPTIME", "Hub uptime"),
            ("LAST_UPDATED", "Last updated"),
            ("HUB_OVERVIEW", "Threat Hub system overview"),
            ("HUB_HOST", "Host name"), ("HUB_VERSION", "Version"),
            ("HUB_ENDPOINT", "Listen endpoint"), ("HUB_GENERATION", "Store generation"),
            ("HUB_TTL", "Threat TTL"),
            ("EDGE_NODES", "Edge nodes"),
            ("STATUS", "Status"), ("NODE_ID", "Node ID"), ("NODE_NAME", "Node name"),
            ("SOURCE_IP", "Source IP"), ("LAST_HEARTBEAT", "Last heartbeat"),
            ("REPORTED_THREATS", "Reported threats"), ("ENTER_KEY", "Enter an API Key to load data"),
            ("TOO_MANY_REQUESTS", "Too many requests. Please wait: "),
            ("INVALID_KEY", "API Key authentication failed or expired. Please re-enter."),
            ("SERVER_ERROR", "Server returned an error: HTTP "),
            ("REFRESH_PREFIX", "Refreshes every 30 seconds · Last updated: "),
            ("NO_NODES", "No edge nodes are connected"),
            ("UNNAMED", "(unnamed)"), ("UNKNOWN_ERROR", "Unknown error"),
            ("CONNECTION_ERROR", "Unable to connect to Threat Hub: "),
            ("THEME_LABEL", "Theme"), ("THEME_AUTO", "Auto"),
            ("THEME_LIGHT", "Light"), ("THEME_DARK", "Dark"),
            ("DAYS", "d"), ("HOURS", "h"),
            ("MINUTES", "m"), ("SECONDS", "s"),
            ("BLOCKS", "blocks"), ("PROBATION", "in probation")
        ]);
    }

    private const string DashboardHtml = """
        <!DOCTYPE html>
        <html lang="{{LANG}}">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{TITLE}}</title>
          <meta name="color-scheme" content="dark light">
          <link rel="icon" type="image/svg+xml" href="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cpath fill='%230f172a' d='M12 1.5 3 5v6.2c0 6.1 3.8 10.9 9 12.3 5.2-1.4 9-6.2 9-12.3V5z'/%3E%3Cpath fill='%2314b8a6' d='M12 3.3 5 6.1v5.1c0 5.1 3.1 9.1 7 10.3 3.9-1.2 7-5.2 7-10.3V6.1z'/%3E%3Cpath fill='%230f172a' d='m10.6 13.4-2-2-1.4 1.4 3.4 3.4 6-6-1.4-1.4z'/%3E%3C/svg%3E">
          <style nonce="{{NONCE}}">
            :root {
              --bg: #0f172a; --fg: #f8fafc; --muted: #94a3b8;
              --accent: #14b8a6; --accent-hover: #0d9488; --accent-fg: #0f172a; --accent-shadow: rgba(20, 184, 166, 0.5);
              --card-bg: #1e293b; --card-border: #334155;
              --input-bg: #1e293b; --input-border: #334155; --input-fg: #f1f5f9;
              --badge-online-bg: #064e3b; --badge-online-fg: #6ee7b7;
              --badge-offline-bg: #7f1d1d; --badge-offline-fg: #fca5a5;
              --td-fg: #cbd5e1; --td-border: #1e293b; --row-hover-bg: #1e293b44;
              --node-id-fg: #38bdf8;
              --error-bg: #7f1d1d22; --error-border: #7f1d1d; --error-fg: #fca5a5;
            }
            @media (prefers-color-scheme: light) {
              :root {
                --bg: #f8fafc; --fg: #0f172a; --muted: #64748b;
                --accent: #0d9488; --accent-hover: #0f766e; --accent-fg: #ffffff; --accent-shadow: rgba(13, 148, 136, 0.35);
                --card-bg: #ffffff; --card-border: #e2e8f0;
                --input-bg: #ffffff; --input-border: #cbd5e1; --input-fg: #0f172a;
                --badge-online-bg: #d1fae5; --badge-online-fg: #047857;
                --badge-offline-bg: #fee2e2; --badge-offline-fg: #b91c1c;
                --td-fg: #334155; --td-border: #f1f5f9; --row-hover-bg: #f1f5f9;
                --node-id-fg: #0284c7;
                --error-bg: #fef2f2; --error-border: #fecaca; --error-fg: #b91c1c;
              }
            }
            :root[data-theme="dark"] {
              --bg: #0f172a; --fg: #f8fafc; --muted: #94a3b8;
              --accent: #14b8a6; --accent-hover: #0d9488; --accent-fg: #0f172a; --accent-shadow: rgba(20, 184, 166, 0.5);
              --card-bg: #1e293b; --card-border: #334155;
              --input-bg: #1e293b; --input-border: #334155; --input-fg: #f1f5f9;
              --badge-online-bg: #064e3b; --badge-online-fg: #6ee7b7;
              --badge-offline-bg: #7f1d1d; --badge-offline-fg: #fca5a5;
              --td-fg: #cbd5e1; --td-border: #1e293b; --row-hover-bg: #1e293b44;
              --node-id-fg: #38bdf8;
              --error-bg: #7f1d1d22; --error-border: #7f1d1d; --error-fg: #fca5a5;
            }
            :root[data-theme="light"] {
              --bg: #f8fafc; --fg: #0f172a; --muted: #64748b;
              --accent: #0d9488; --accent-hover: #0f766e; --accent-fg: #ffffff; --accent-shadow: rgba(13, 148, 136, 0.35);
              --card-bg: #ffffff; --card-border: #e2e8f0;
              --input-bg: #ffffff; --input-border: #cbd5e1; --input-fg: #0f172a;
              --badge-online-bg: #d1fae5; --badge-online-fg: #047857;
              --badge-offline-bg: #fee2e2; --badge-offline-fg: #b91c1c;
              --td-fg: #334155; --td-border: #f1f5f9; --row-hover-bg: #f1f5f9;
              --node-id-fg: #0284c7;
              --error-bg: #fef2f2; --error-border: #fecaca; --error-fg: #b91c1c;
            }
            * { box-sizing: border-box; margin: 0; padding: 0; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; }
            body { background-color: var(--bg); color: var(--fg); min-height: 100vh; padding: 24px; }
            h1 { font-size: 22px; font-weight: 700; color: var(--accent); }
            h2 { font-size: 14px; font-weight: 600; color: var(--muted); text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 12px; }
            .top-bar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 28px; flex-wrap: wrap; gap: 12px; }
            .top-bar-left { display: flex; align-items: center; gap: 14px; }
            .badge { font-size: 12px; padding: 3px 10px; border-radius: 999px; font-weight: 600; }
            .badge-online { background: var(--badge-online-bg); color: var(--badge-online-fg); }
            .badge-offline { background: var(--badge-offline-bg); color: var(--badge-offline-fg); }
            .badge-authenticated { background: var(--badge-online-bg); color: var(--badge-online-fg); }
            .key-row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
            .key-group { display: flex; gap: 8px; align-items: center; }
            .language-select { background: var(--input-bg); border: 1px solid var(--input-border); color: var(--input-fg); padding: 7px 10px; border-radius: 6px; font-size: 13px; }
            .key-group input { background: var(--input-bg); border: 1px solid var(--input-border); color: var(--input-fg); padding: 7px 12px; border-radius: 6px; font-size: 13px; width: 280px; max-width: 100%; min-width: 140px; flex: 1 1 auto; font-family: monospace; }
            .key-group input:focus { outline: none; border-color: var(--accent); box-shadow: 0 0 0 3px var(--accent-shadow); }
            .key-group button { background: var(--accent); color: var(--accent-fg); border: 1px solid transparent; border-radius: 6px; padding: 7px 16px; font-size: 13px; font-weight: 700; cursor: pointer; white-space: nowrap; flex-shrink: 0; line-height: 1.25; display: inline-flex; align-items: center; justify-content: center; transition: background 0.15s; }
            .key-group button:hover:not(:disabled) { background: var(--accent-hover); }
            .key-group button:disabled { opacity: 0.6; cursor: not-allowed; }
            .key-group .btn-logout { background: var(--accent); border: 1px solid transparent; color: var(--accent-fg); border-radius: 6px; padding: 7px 16px; font-size: 13px; font-weight: 700; cursor: pointer; white-space: nowrap; flex-shrink: 0; line-height: 1.25; display: inline-flex; align-items: center; justify-content: center; transition: background 0.15s; }
            .key-group .btn-logout:hover { background: var(--accent-hover); }
            .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 20px; }
            .card { background: var(--card-bg); border-radius: 10px; border: 1px solid var(--card-border); padding: 20px; }
            .stat-value { font-size: 32px; font-weight: 800; color: var(--fg); margin-bottom: 4px; }
            .stat-label { font-size: 13px; color: var(--muted); }
            .stat-sub { font-size: 12px; color: var(--muted); margin-top: 4px; font-weight: 500; }
            .hub-overview { background: var(--card-bg); border-radius: 10px; border: 1px solid var(--card-border); padding: 16px 20px; margin-bottom: 24px; }
            .hub-overview-title { font-size: 12px; font-weight: 700; color: var(--muted); text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 12px; }
            .hub-overview-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px 20px; font-size: 13px; }
            .hub-prop { display: flex; flex-direction: column; gap: 2px; }
            .hub-prop-k { font-size: 11px; color: var(--muted); text-transform: uppercase; letter-spacing: 0.03em; }
            .hub-prop-v { font-weight: 600; color: var(--fg); word-break: break-all; }
            .monospace { font-family: monospace; font-size: 12px; }
            table { width: 100%; border-collapse: collapse; font-size: 13px; }
            th { text-align: left; color: var(--muted); font-weight: 600; font-size: 11px; text-transform: uppercase; letter-spacing: 0.04em; padding: 8px 12px; border-bottom: 1px solid var(--card-border); }
            td { padding: 10px 12px; border-bottom: 1px solid var(--td-border); color: var(--td-fg); vertical-align: middle; }
            tr:last-child td { border-bottom: none; }
            tr:hover td { background: var(--row-hover-bg); }
            .node-id { font-family: monospace; color: var(--node-id-fg); font-size: 12px; }
            .status-dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 6px; }
            .dot-green { background: #22c55e; }
            .dot-yellow { background: #eab308; }
            .dot-gray { background: #475569; }
            .empty { color: var(--muted); text-align: center; padding: 32px; font-size: 13px; }
            .error-bar { background: var(--error-bg); border: 1px solid var(--error-border); border-radius: 6px; color: var(--error-fg); padding: 10px 14px; font-size: 13px; margin-bottom: 16px; display: none; }
            .refresh-info { font-size: 11px; color: var(--muted); text-align: right; margin-top: 8px; }
            .table-wrap { background: var(--card-bg); border-radius: 10px; border: 1px solid var(--card-border); overflow-x: auto; overflow-y: hidden; }
          </style>
        </head>
        <body>
          <script nonce="{{NONCE}}">
            (function() {
              var savedTheme = sessionStorage.getItem('idds_hub_theme');
              if (savedTheme === 'light' || savedTheme === 'dark') {
                document.documentElement.setAttribute('data-theme', savedTheme);
              }
            })();
          </script>
          <div class="top-bar">
            <div class="top-bar-left">
              <h1><span aria-hidden="true">&#x1F6E1;&#xFE0F;</span> {{BRAND_NAME}}</h1>
              <span id="hub-status" class="badge badge-offline" role="status">{{OFFLINE}}</span>
            </div>
            <div class="key-row">
              <select id="theme-select" class="language-select" aria-label="{{THEME_LABEL}}">
                <option value="auto">{{THEME_AUTO}}</option>
                <option value="light">{{THEME_LIGHT}}</option>
                <option value="dark">{{THEME_DARK}}</option>
              </select>
              <select id="language-select" class="language-select" aria-label="Language">
                <option value="zh-Hant-TW">繁體中文</option>
                <option value="en-US">English</option>
              </select>
              <div id="key-input-group" class="key-group">
                <input type="password" id="api-key" placeholder="{{API_KEY_PLACEHOLDER}}" aria-label="{{API_KEY_PLACEHOLDER}}" autocomplete="off" />
                <button id="apply-key-btn">{{APPLY}}</button>
              </div>
              <div id="key-auth-group" class="key-group" style="display: none;">
                <span class="badge badge-authenticated">{{AUTHENTICATED}}</span>
                <button id="logout-btn" class="btn-logout">{{LOGOUT}}</button>
              </div>
            </div>
          </div>

          <div id="error-bar" class="error-bar" role="status" aria-live="polite"></div>

          <div class="cards">
            <div class="card">
              <div class="stat-value" id="stat-nodes">—</div>
              <div class="stat-label">{{CONNECTED_NODES}}</div>
            </div>
            <div class="card">
              <div class="stat-value" id="stat-threats">—</div>
              <div class="stat-label">{{ACTIVE_THREATS}}</div>
              <div class="stat-sub" id="stat-threats-cap">—</div>
            </div>
            <div class="card">
              <div class="stat-value" id="stat-defense">—</div>
              <div class="stat-label">{{HUB_DEFENSE}}</div>
              <div class="stat-sub" id="stat-defense-sub">—</div>
            </div>
            <div class="card">
              <div class="stat-value" id="stat-uptime">—</div>
              <div class="stat-label">{{HUB_UPTIME}}</div>
            </div>
          </div>

          <div class="hub-overview">
            <div class="hub-overview-title">{{HUB_OVERVIEW}}</div>
            <div class="hub-overview-grid">
              <div class="hub-prop"><span class="hub-prop-k">{{HUB_HOST}}</span><span id="hub-host" class="hub-prop-v">—</span></div>
              <div class="hub-prop"><span class="hub-prop-k">{{HUB_VERSION}}</span><span id="hub-ver" class="hub-prop-v">—</span></div>
              <div class="hub-prop"><span class="hub-prop-k">{{HUB_ENDPOINT}}</span><span id="hub-endpoint" class="hub-prop-v">—</span></div>
              <div class="hub-prop"><span class="hub-prop-k">{{HUB_GENERATION}}</span><span id="hub-generation" class="hub-prop-v monospace">—</span></div>
              <div class="hub-prop"><span class="hub-prop-k">{{HUB_TTL}}</span><span id="hub-ttl" class="hub-prop-v">—</span></div>
              <div class="hub-prop"><span class="hub-prop-k">{{LAST_UPDATED}}</span><span id="stat-updated" class="hub-prop-v">—</span></div>
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

          <script nonce="{{NONCE}}">
            var currentKey = sessionStorage.getItem('idds_hub_key') || '';
            var refreshTimer = null;
            var cooldownTimer = null;
            var currentLanguage = '{{LANG}}';
            var savedLanguage = sessionStorage.getItem('idds_hub_language');

            if (!new URL(window.location.href).searchParams.has('lang') && savedLanguage && savedLanguage !== currentLanguage) {
              var savedUrl = new URL(window.location.href);
              savedUrl.searchParams.set('lang', savedLanguage);
              window.location.replace(savedUrl.toString());
            }

            document.getElementById('apply-key-btn').addEventListener('click', applyKey);
            document.getElementById('logout-btn').addEventListener('click', logout);
            document.getElementById('api-key').addEventListener('keydown', function(e) {
              if (e.key === 'Enter') { applyKey(); }
            });

            document.getElementById('theme-select').value = sessionStorage.getItem('idds_hub_theme') || 'auto';
            document.getElementById('theme-select').addEventListener('change', function(event) {
              var selected = event.target.value;
              sessionStorage.setItem('idds_hub_theme', selected);
              if (selected === 'light' || selected === 'dark') {
                document.documentElement.setAttribute('data-theme', selected);
              } else {
                document.documentElement.removeAttribute('data-theme');
              }
            });
            document.getElementById('language-select').value = currentLanguage;
            document.getElementById('language-select').addEventListener('change', function(event) {
              var selected = event.target.value;
              sessionStorage.setItem('idds_hub_language', selected);
              var url = new URL(window.location.href);
              url.searchParams.set('lang', selected);
              window.location.assign(url.toString());
            });

            function setAuthState(authenticated) {
              var inputGroup = document.getElementById('key-input-group');
              var authGroup = document.getElementById('key-auth-group');
              if (authenticated) {
                inputGroup.style.display = 'none';
                authGroup.style.display = 'flex';
              } else {
                inputGroup.style.display = 'flex';
                authGroup.style.display = 'none';
                document.getElementById('api-key').value = '';
              }
            }

            function applyKey() {
              var input = document.getElementById('api-key').value.trim();
              if (input) {
                currentKey = input;
                sessionStorage.setItem('idds_hub_key', currentKey);
              }
              fetchData();
            }

            function logout() {
              currentKey = '';
              sessionStorage.removeItem('idds_hub_key');
              if (refreshTimer) {
                clearInterval(refreshTimer);
                refreshTimer = null;
              }
              if (cooldownTimer) {
                clearInterval(cooldownTimer);
                cooldownTimer = null;
              }
              setAuthState(false);
              resetView();
              hideError();
              document.getElementById('api-key').focus();
            }

            function resetView() {
              document.getElementById('hub-status').textContent = '{{OFFLINE}}';
              document.getElementById('hub-status').className = 'badge badge-offline';
              document.getElementById('stat-nodes').textContent = '—';
              document.getElementById('stat-threats').textContent = '—';
              document.getElementById('stat-threats-cap').textContent = '—';
              document.getElementById('stat-defense').textContent = '—';
              document.getElementById('stat-defense-sub').textContent = '—';
              document.getElementById('stat-uptime').textContent = '—';
              document.getElementById('stat-updated').textContent = '—';
              document.getElementById('hub-host').textContent = '—';
              document.getElementById('hub-ver').textContent = '—';
              document.getElementById('hub-endpoint').textContent = '—';
              document.getElementById('hub-generation').textContent = '—';
              document.getElementById('hub-ttl').textContent = '—';
              document.getElementById('refresh-info').textContent = '';
              var tbody = document.getElementById('node-tbody');
              tbody.replaceChildren();
              var emptyTr = document.createElement('tr');
              var emptyTd = document.createElement('td');
              emptyTd.colSpan = 6;
              emptyTd.className = 'empty';
              emptyTd.textContent = '{{ENTER_KEY}}';
              emptyTr.appendChild(emptyTd);
              tbody.appendChild(emptyTr);
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

            function fmtUptime(seconds) {
              if (typeof seconds !== 'number' || isNaN(seconds) || seconds < 0) return '—';
              var d = Math.floor(seconds / 86400);
              var h = Math.floor((seconds % 86400) / 3600);
              var m = Math.floor((seconds % 3600) / 60);
              var s = Math.floor(seconds % 60);
              if (d > 0) return d + ' {{DAYS}} ' + h + ' {{HOURS}} ' + m + ' {{MINUTES}}';
              if (h > 0) return h + ' {{HOURS}} ' + m + ' {{MINUTES}}';
              if (m > 0) return m + ' {{MINUTES}} ' + s + ' {{SECONDS}}';
              return s + ' {{SECONDS}}';
            }

            function showError(msg) {
              var bar = document.getElementById('error-bar');
              bar.textContent = msg;
              bar.style.display = 'block';
            }

            function hideError() {
              document.getElementById('error-bar').style.display = 'none';
            }

            function startCooldown(seconds) {
              if (cooldownTimer) clearInterval(cooldownTimer);
              var applyBtn = document.getElementById('apply-key-btn');
              applyBtn.disabled = true;
              var remaining = seconds;
              function updateCooldownMsg() {
                showError('{{TOO_MANY_REQUESTS}} ' + remaining + ' {{SECONDS}}');
              }
              updateCooldownMsg();
              cooldownTimer = setInterval(function() {
                remaining--;
                if (remaining <= 0) {
                  clearInterval(cooldownTimer);
                  cooldownTimer = null;
                  applyBtn.disabled = false;
                  hideError();
                } else {
                  updateCooldownMsg();
                }
              }, 1000);
            }

            function fetchData() {
              if (!currentKey) { return; }
              fetch('/api/threat-hub/nodes', {
                method: 'GET',
                headers: { 'X-IDDS-ThreatHub-ApiKey': currentKey }
              })
              .then(function(r) {
                if (r.status === 429) {
                  var retrySec = parseInt(r.headers.get('Retry-After'), 10) || 60;
                  startCooldown(retrySec);
                  return null;
                }
                if (r.status === 401) {
                  logout();
                  showError('{{INVALID_KEY}}');
                  return null;
                }
                if (!r.ok) { showError('{{SERVER_ERROR}}' + r.status); return null; }
                return r.json();
              })
              .then(function(data) {
                if (!data) return;
                hideError();
                setAuthState(true);
                document.getElementById('hub-status').textContent = '{{ONLINE}}';
                document.getElementById('hub-status').className = 'badge badge-online';
                document.getElementById('stat-nodes').textContent = data.nodes ? data.nodes.length : 0;
                var threats = data.totalActiveThreatCount ?? 0;
                document.getElementById('stat-threats').textContent = threats;
                if (data.hub) {
                  var cap = data.hub.maxThreatCapacity || 100000;
                  var pct = ((threats / cap) * 100).toFixed(2);
                  document.getElementById('stat-threats-cap').textContent = threats + ' / ' + cap + ' (' + pct + '%)';
                  var blocks = data.hub.localActiveBlocks ?? 0;
                  var probation = data.hub.localProbationCount ?? 0;
                  document.getElementById('stat-defense').textContent = blocks + ' {{BLOCKS}}';
                  document.getElementById('stat-defense-sub').textContent = probation + ' {{PROBATION}}';
                  document.getElementById('stat-uptime').textContent = fmtUptime(data.hub.uptimeSeconds);
                  document.getElementById('hub-host').textContent = data.hub.hostName || '—';
                  document.getElementById('hub-ver').textContent = data.hub.version ? ('v' + data.hub.version) : '—';
                  document.getElementById('hub-endpoint').textContent = data.hub.listenMode || '—';
                  document.getElementById('hub-generation').textContent = data.hub.generation || '—';
                  document.getElementById('hub-ttl').textContent = (data.hub.threatTtlDays ?? '—') + ' {{DAYS}}';
                }
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

            if (currentKey) {
              setAuthState(true);
              fetchData();
            } else {
              setAuthState(false);
            }
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
