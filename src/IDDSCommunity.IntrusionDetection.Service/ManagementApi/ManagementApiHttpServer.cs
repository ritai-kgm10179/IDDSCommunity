using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.ManagementApi;

/// <summary>
/// 提供安全 RESTful Management API 伺服器，支援遠端/本機管理、查詢狀態、手動封鎖與解除封鎖。
/// </summary>
public sealed class ManagementApiHttpServer : IDisposable
{
    private readonly IddsConfig configuration;
    private readonly Database database;
    private HttpListener? listener;
    private CancellationTokenSource? cts;
    private Task? listenerTask;
    private bool isDisposed;

    /// <summary>
    /// 初始化 <see cref="ManagementApiHttpServer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="configuration">全域組態。</param>
    /// <param name="database">資料庫執行個體。</param>
    public ManagementApiHttpServer(IddsConfig configuration, Database database)
    {
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
        if (listener != null && listener.IsListening) return;

        try
        {
            int port = configuration.ManagementApiPort;
            listener = new HttpListener();
            try
            {
                listener.Prefixes.Add($"http://+:{port}/");
                listener.Start();
            }
            catch
            {
                listener.Close();
                listener = new HttpListener();
                listener.Prefixes.Add($"http://*:{port}/");
                try
                {
                    listener.Start();
                }
                catch
                {
                    listener.Close();
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                }
            }

            cts = new CancellationTokenSource();
            listenerTask = Task.Run(() => ListenLoopAsync(cts.Token));
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Server started listening on port {port}",
                System.Diagnostics.EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Failed to start server: {ex.Message}",
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
                _ = Task.Run(() => ProcessRequestAsync(context), cancellationToken);
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
            // 驗證 API 金鑰 (若有設定)
            string expectedKey = configuration.ManagementApiKey;
            if (!string.IsNullOrWhiteSpace(expectedKey))
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

                if (!string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
                {
                    await SendJsonResponseAsync(response, HttpStatusCode.Unauthorized, new { error = "Unauthorized" });
                    return;
                }
            }

            string path = request.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(path)) path = "/";
            string method = request.HttpMethod.ToUpperInvariant();

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
                    using var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8);
                    string body = await reader.ReadToEndAsync();
                    var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                    string? targetIp = doc.RootElement.TryGetProperty("ipAddress", out var ipElem) ? ipElem.GetString() : null;

                    if (string.IsNullOrWhiteSpace(targetIp) || !IPAddress.TryParse(targetIp, out _))
                    {
                        await SendJsonResponseAsync(response, HttpStatusCode.BadRequest, new { error = "Invalid or missing 'ipAddress'" });
                        return;
                    }

                    targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                    long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), targetIp, IntrusionLog.STATUS_HARD_LOCKED, false);
                    Locks.CreateLock(DateTime.UtcNow, DateTime.MaxValue, incidentId, Shared.Lock.LOCK_STATUS_HARDLOCK, 0, targetIp);

                    await SendJsonResponseAsync(response, HttpStatusCode.Created, new { success = true, message = $"IP {targetIp} has been hard-locked.", ipAddress = targetIp });
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

                string targetIp = path[14..];
                if (string.IsNullOrWhiteSpace(targetIp))
                {
                    await SendJsonResponseAsync(response, HttpStatusCode.BadRequest, new { error = "Target IP required" });
                    return;
                }

                targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                bool unblocked = Locks.UnlockIp(targetIp);
                await SendJsonResponseAsync(response, HttpStatusCode.OK, new { success = unblocked, ipAddress = targetIp });
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

            // ChatOps 雙向互動一鍵封鎖與解鎖 (Action Token 驗證)
            if (path is "/api/v1/actions/block" or "/api/v1/actions/unblock")
            {
                if (method != "GET")
                {
                    response.Headers["Allow"] = "GET";
                    await SendJsonResponseAsync(response, HttpStatusCode.MethodNotAllowed, new { error = "Method Not Allowed" });
                    return;
                }

                string actionType = path.EndsWith("block") && !path.EndsWith("unblock") ? "block" : "unblock";
                string? token = request.QueryString["token"];
                if (Shared.Security.ActionTokenService.ValidateToken(token, actionType, out string targetIp, configuration.ManagementApiKey))
                {
                    targetIp = IpAddressCanonicalizer.Canonicalize(targetIp);
                    if (actionType == "block")
                    {
                        long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), targetIp, IntrusionLog.STATUS_HARD_LOCKED, false);
                        Locks.CreateLock(DateTime.UtcNow, DateTime.MaxValue, incidentId, Shared.Lock.LOCK_STATUS_HARDLOCK, 0, targetIp);
                        await SendJsonResponseAsync(response, HttpStatusCode.OK, new { success = true, action = "blocked", ipAddress = targetIp, message = $"IP {targetIp} has been hard-locked via ChatOps." });
                    }
                    else
                    {
                        bool unblocked = Locks.UnlockIp(targetIp);
                        await SendJsonResponseAsync(response, HttpStatusCode.OK, new { success = unblocked, action = "unblocked", ipAddress = targetIp, message = $"IP {targetIp} has been unblocked via ChatOps." });
                    }
                    return;
                }
                await SendJsonResponseAsync(response, HttpStatusCode.Forbidden, new { error = "Invalid or expired Action Token" });
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            await SendJsonResponseAsync(response, HttpStatusCode.NotFound, new { error = "Endpoint Not Found" });
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[ManagementAPI] Error handling request: {ex.Message}",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            await SendJsonResponseAsync(response, HttpStatusCode.InternalServerError, new { error = "Internal Server Error" });
        }
        finally
        {
            try { response.Close(); } catch { }
        }
    }

    private static async Task SendJsonResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, object data)
    {
        response.StatusCode = (int)statusCode;
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
