using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Security;

namespace IDDSCommunity.IntrusionDetection.Service.ManagementApi;

/// <summary>
/// 提供安全 RESTful Management API 伺服器，支援遠端/本機管理、查詢狀態、手動封鎖與解除封鎖。
/// </summary>
public sealed class ManagementApiHttpServer : IDisposable
{
    private readonly IddsConfig configuration;
    private readonly Database database;
    private readonly bool allowLoopbackHttp;
    private readonly FailedAttemptsRateLimiter authRateLimiter = new(maxFailedAttempts: 10, windowDuration: TimeSpan.FromMinutes(15), lockDuration: TimeSpan.FromMinutes(15));
    private HttpListener? listener;
    private BoundedHttpDispatcher? dispatcher;
    private CancellationTokenSource? cts;
    private Task? listenerTask;
    private bool isDisposed;

    /// <summary>
    /// 初始化 <see cref="ManagementApiHttpServer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="configuration">全域組態。</param>
    /// <param name="database">資料庫執行個體。</param>
    public ManagementApiHttpServer(IddsConfig configuration, Database database) : this(configuration, database, false) { }

    internal ManagementApiHttpServer(IddsConfig configuration, Database database, bool allowLoopbackHttp)
    {
        this.allowLoopbackHttp = allowLoopbackHttp;
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <summary>
    /// 取得 API 伺服器是否處於運行狀態。
    /// </summary>
    public bool IsRunning => listener != null && listener.IsListening;

    /// <summary>
    /// 啟動 RESTful Management API 伺服器。
    /// </summary>
    public void Start()
    {
        if (!configuration.EnableManagementApi) return;
        if (string.IsNullOrWhiteSpace(configuration.ManagementApiKey)) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Management API requires an API key."));
        if (listener != null && listener.IsListening) return;

        try
        {
            int port = configuration.ManagementApiPort;
            string scheme = allowLoopbackHttp ? "http" : "https";
            listener = new HttpListener();
            ConfigureHttpTimeouts(listener);
            try
            {
                listener.Prefixes.Add(allowLoopbackHttp ? $"http://localhost:{port}/" : $"https://+:{port}/");
                listener.Start();
            }
            catch
            {
                listener.Close();
                listener = new HttpListener();
                ConfigureHttpTimeouts(listener);
                listener.Prefixes.Add($"{scheme}://*:{port}/");
                try
                {
                    listener.Start();
                }
                catch
                {
                    listener.Close();
                    listener = new HttpListener();
                    ConfigureHttpTimeouts(listener);
                    listener.Prefixes.Add($"{scheme}://localhost:{port}/");
                    listener.Start();
                }
            }

            dispatcher = new BoundedHttpDispatcher(ProcessRequestAsync);
        cts = new CancellationTokenSource();
            listenerTask = ListenLoopAsync(cts.Token);
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Server started listening on port {port}",
                System.Diagnostics.EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        catch (Exception ex)
        {
            string hint = !allowLoopbackHttp
                ? $" If using HTTPS, ensure a TLS certificate is bound to port {configuration.ManagementApiPort} (e.g. 'netsh http add sslcert ipport=0.0.0.0:{configuration.ManagementApiPort} certhash=<THUMBPRINT> appid={Guid.NewGuid():B}')."
                : string.Empty;
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Failed to start server: {ex.Message}.{hint}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
    }

    /// <summary>
    /// 停止 RESTful Management API 伺服器。
    /// </summary>
    public void Stop()
    {
        try
        {
            cts?.Cancel();
            listener?.Stop();
            listener?.Close();
            listener = null;
            dispatcher?.Dispose();
            dispatcher = null;
            cts?.Dispose();
            cts = null;
        }
        catch { }
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener != null && listener.IsListening)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();
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
                if (!cancellationToken.IsCancellationRequested)
                    WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Request loop error: {ex.Message}",
                        System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            string path = request.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(path)) path = "/";
            string method = request.HttpMethod.ToUpperInvariant();

            // ChatOps 雙向互動一鍵封鎖與解鎖 (Action Token 驗證：由 URL 參數自帶 HMAC-SHA256 防偽簽章驗證，供瀏覽器與通知外鏈直接觸發)
            if (path is "/api/v1/actions/block" or "/api/v1/actions/unblock")
            {
                if (method != "GET" && method != "POST")
                {
                    response.Headers["Allow"] = "GET, POST";
                    await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                    return;
                }

                string actionType = path.EndsWith("block") && !path.EndsWith("unblock") ? "block" : "unblock";
                string? token = request.QueryString["token"];

                // RFC 9110 Safe Methods: GET 請求絕對不可修改系統狀態或銷毀 Token
                if (method == "GET")
                {
                    string accept = request.Headers["Accept"] ?? string.Empty;
                    if (Shared.Security.ActionTokenService.ValidateToken(token, actionType, out string previewIp, configuration.ManagementApiKey))
                    {
                        if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                        {
                            await ServeActionConfirmationPageAsync(response, actionType, previewIp, token!);
                            return;
                        }

                        await SendJsonResponseAsync(response, HttpStatusCode.OK, new
                        {
                            preview = true,
                            action = actionType,
                            ipAddress = previewIp,
                            requiresConfirmation = true,
                            confirmationMethod = "POST"
                        });
                        return;
                    }

                    await SendJsonResponseAsync(response, HttpStatusCode.Forbidden, new { error = "Invalid or expired Action Token" });
                    return;
                }

                // 實際狀態變更（封鎖/解鎖）必須且僅能透過 POST 請求執行，並單次銷毀 Token (Burn-on-use)
                if (method == "POST")
                {
                    if (Shared.Security.ActionTokenService.ValidateAndBurnToken(token, actionType, out string targetIp, configuration.ManagementApiKey))
                    {
                        targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                        if (actionType == "block")
                        {
                            if (configuration.IsInSafeNetwork(targetIp)) { await SendJsonResponseAsync(response, HttpStatusCode.Forbidden, new { error = "Safe network" }); return; }
                            long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), targetIp, IntrusionLog.STATUS_HARD_LOCK_REQUESTED, false);
                            Locks.CreateLock(DateTime.UtcNow, DateTime.MaxValue, incidentId, Shared.Lock.LOCK_STATUS_HARDLOCK_REQUESTED, 0, targetIp);
                            await SendJsonResponseAsync(response, HttpStatusCode.Accepted, new { success = true, action = "blockRequested", ipAddress = targetIp, message = $"IP {targetIp} block request has been accepted via ChatOps." });
                        }
                        else
                        {
                            bool unblocked = Locks.UnlockIp(targetIp);
                            await SendJsonResponseAsync(response, HttpStatusCode.Accepted, new { success = unblocked, action = "unblockRequested", ipAddress = targetIp, message = $"IP {targetIp} unblock request has been accepted via ChatOps." });
                        }
                        return;
                    }

                    await SendJsonResponseAsync(response, HttpStatusCode.Forbidden, new { error = "Invalid or expired Action Token" });
                    return;
                }
            }

            // 其餘管理 API 每次請求均驗證 API 金鑰與速率限制。
            string clientIp = request.RemoteEndPoint?.Address != null
                ? IpAddressCanonicalizer.Canonicalize(request.RemoteEndPoint.Address).ToString()
                : "unknown";

            if (authRateLimiter.IsBlocked(clientIp, out TimeSpan retryAfter))
            {
                response.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                await SendJsonResponseAsync(response, HttpStatusCode.TooManyRequests, new { error = "Too Many Requests" });
                return;
            }

            string expectedKey = configuration.ManagementApiKey;
            {
                string? providedKey = request.Headers["X-Api-Key"];
                if (string.IsNullOrWhiteSpace(providedKey))
                {
                    string? authHeader = request.Headers["Authorization"];
                    if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        providedKey = authHeader[7..].Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(expectedKey) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey)), System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(providedKey ?? string.Empty))))
                {
                    authRateLimiter.RecordFailedAttempt(clientIp);
                    await SendJsonResponseAsync(response, HttpStatusCode.Unauthorized, new { error = "Unauthorized" });
                    return;
                }

                authRateLimiter.Reset(clientIp);
            }

            if (path is "/" or "/status" or "/health" or "/healthz" or "/api/v1/status")
            {
                if (method != "GET" && method != "HEAD")
                {
                    response.Headers["Allow"] = "GET, HEAD";
                    await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                    return;
                }

                var statusObj = new
                {
                    status = "healthy",
                    system = "IDDS Community",
                    version = "10.0",
                    databaseConfigured = database.IsConfigured,
                    serverTimeUtc = DateTime.UtcNow
                };
                await SendJsonResponseAsync(response, HttpStatusCode.OK, statusObj);
                return;
            }

            if (path == "/api/v1/locks")
            {
                if (method == "GET")
                {
                    var activeLocks = Locks.GetActiveLocks();
                    await SendJsonResponseAsync(response, HttpStatusCode.OK, activeLocks);
                    return;
                }

                if (method == "POST")
                {

                    string body = await BoundedHttpDispatcher.ReadBodyAsync(request);
                    using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                    string? targetIp = doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ipAddress", out var ipElem) && ipElem.ValueKind == JsonValueKind.String
                        ? ipElem.GetString() : null;

                    if (string.IsNullOrWhiteSpace(targetIp) || !IPAddress.TryParse(targetIp, out _))
                    {
                        await SendJsonResponseAsync(response, HttpStatusCode.BadRequest, new { error = "Invalid or missing 'ipAddress'" });
                        return;
                    }

                    targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                    if (configuration.IsInSafeNetwork(targetIp)) { await SendJsonResponseAsync(response, HttpStatusCode.Forbidden, new { error = "Safe network" }); return; }
                    long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), targetIp, IntrusionLog.STATUS_HARD_LOCK_REQUESTED, false);
                    Locks.CreateLock(DateTime.UtcNow, DateTime.MaxValue, incidentId, Shared.Lock.LOCK_STATUS_HARDLOCK_REQUESTED, 0, targetIp);

                    await SendJsonResponseAsync(response, HttpStatusCode.Accepted, new { success = true, message = $"IP {targetIp} block request has been accepted.", ipAddress = targetIp });
                    return;
                }

                response.Headers["Allow"] = "GET, POST";
                await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                return;
            }

            if (path.StartsWith("/api/v1/locks/"))
            {
                if (method != "DELETE")
                {
                    response.Headers["Allow"] = "DELETE";
                    await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                    return;
                }

                string targetIp = Uri.UnescapeDataString(path[14..]);
                if (string.IsNullOrWhiteSpace(targetIp) || !IPAddress.TryParse(targetIp, out _))
                {
                    await SendJsonResponseAsync(response, HttpStatusCode.BadRequest, new { error = "Target IP required" });
                    return;
                }

                targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                bool unblocked = Locks.UnlockIp(targetIp);
                await SendJsonResponseAsync(response, HttpStatusCode.Accepted, new { success = unblocked, ipAddress = targetIp });
                return;
            }

            if (path == "/api/v1/whitelist")
            {
                if (method != "GET" && method != "HEAD")
                {
                    response.Headers["Allow"] = "GET, HEAD";
                    await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                    return;
                }

                var safeNets = configuration.SafeNetworks;
                await SendJsonResponseAsync(response, HttpStatusCode.OK, new { safeNetworks = safeNets });
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            await SendJsonResponseAsync(response, HttpStatusCode.NotFound, new { error = "Endpoint Not Found" });
        }
        catch (RequestBodyTooLargeException) { await SendJsonResponseAsync(response, HttpStatusCode.RequestEntityTooLarge, new { error = "Body too large" }); }
        catch (JsonException) { await SendJsonResponseAsync(response, HttpStatusCode.BadRequest, new { error = "Invalid JSON" }); }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Error handling request: {ex.GetType().Name}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            await SendJsonResponseAsync(response, HttpStatusCode.InternalServerError, new { error = "Internal Server Error" });
        }
        finally
        {
            try { response.Close(); } catch { }
        }
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

    private static async Task ServeActionConfirmationPageAsync(HttpListenerResponse response, string actionType, string targetIp, string token)
    {
        response.StatusCode = (int)HttpStatusCode.OK;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.Headers["Content-Security-Policy"] = "default-src 'self' 'unsafe-inline';";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        response.ContentType = "text/html; charset=utf-8";

        string actionText = actionType == "block" ? "永久硬封鎖" : "立即解除封鎖";
        string buttonColor = actionType == "block" ? "#ef4444" : "#10b981";
        string encodedIp = WebUtility.HtmlEncode(targetIp);
        string actionUrl = $"/api/v1/actions/{actionType}?token={Uri.EscapeDataString(token)}";

        string html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>IDDS Community - ChatOps 安全操作確認</title>
          <style>
            * { box-sizing: border-box; margin: 0; padding: 0; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; }
            body { background-color: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; padding: 20px; }
            .card { background-color: #1e293b; border-radius: 12px; border: 1px solid #334155; max-width: 480px; width: 100%; padding: 32px; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5); text-align: center; }
            h1 { font-size: 20px; font-weight: 700; color: #14b8a6; margin-bottom: 12px; }
            p { font-size: 14px; color: #cbd5e1; line-height: 1.6; margin-bottom: 16px; }
            .ip-badge { background: #0f172a; color: #38bdf8; padding: 6px 14px; border-radius: 6px; font-family: monospace; font-size: 16px; display: inline-block; margin-bottom: 20px; border: 1px solid #334155; }
            .warning-box { background: rgba(239, 68, 68, 0.1); border-left: 4px solid {{buttonColor}}; padding: 12px; border-radius: 6px; margin-bottom: 24px; font-size: 13px; text-align: left; color: #94a3b8; }
            button { width: 100%; padding: 14px; background: {{buttonColor}}; color: #fff; font-weight: 700; border: none; border-radius: 8px; font-size: 16px; cursor: pointer; transition: opacity 0.2s; }
            button:hover { opacity: 0.9; }
          </style>
        </head>
        <body>
          <div class="card">
            <h1>🛡️ IDDS Community 操作確認</h1>
            <p>您即將透過 SecOps / ChatOps 快速通道對來源位址實施【<strong>{{actionText}}</strong>】處置：</p>
            <div class="ip-badge">{{encodedIp}}</div>
            <div class="warning-box">
              ⚠️ 注意：此操作權杖具備單次時效性（Burn-on-use）。點擊確認按鈕後，權杖將立即銷毀且不可重複執行。
            </div>
            <form method="POST" action="{{actionUrl}}">
              <button type="submit">確認執行【{{actionText}}】</button>
            </form>
          </div>
        </body>
        </html>
        """;

        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }

    private static async Task SendJsonResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, object data)
    {
        response.StatusCode = (int)statusCode;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.ContentType = "application/json; charset=utf-8";
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }

    /// <summary>
    /// 釋放伺服器使用之資源。
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        Stop();
    }
}
