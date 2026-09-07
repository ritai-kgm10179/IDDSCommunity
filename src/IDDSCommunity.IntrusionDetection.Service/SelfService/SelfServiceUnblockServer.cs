using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.SelfService;

namespace IDDSCommunity.IntrusionDetection.Service.SelfService;

/// <summary>
/// 提供合法使用者自助驗證解鎖門戶之嵌入式 HTTP 服務伺服器。
/// </summary>
public sealed class SelfServiceUnblockServer : IDisposable
{
    private readonly SelfServicePortalSettings settings;
    private readonly Database database;
    private HttpListener? listener;
    private BoundedHttpDispatcher? dispatcher;
    private CancellationTokenSource? cts;
    private Task? listenerTask;
    private readonly Dictionary<string, (int Count, DateTime Expires)> failedAttempts = new(StringComparer.Ordinal);
    private readonly object attemptGate = new();
    private DateTime nextAttemptCleanup;
    private bool isDisposed;

    /// <summary>
    /// 取得伺服器目前是否正在監聽運行中。
    /// </summary>
    public bool IsRunning => listener?.IsListening ?? false;

    /// <summary>
    /// 初始化 <see cref="SelfServiceUnblockServer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">門戶設定。</param>
    /// <param name="database">資料庫執行個體。</param>
    public SelfServiceUnblockServer(SelfServicePortalSettings settings, Database database)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    /// <summary>
    /// 啟動自助解鎖門戶 HTTP 伺服器。
    /// </summary>
    public void Start()
    {
        if (!settings.EnableSelfServicePortal || isDisposed) return;
        if (settings.TrustedProxyCidrs.Length == 0 || string.IsNullOrWhiteSpace(settings.TotpBase32Secret))
            throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Self-service portal requires a trusted independent proxy and a TOTP secret."));
        Stop();

        try
        {
            listener = new HttpListener();
            ConfigureHttpTimeouts(listener);
            string ip = settings.PortalListenIp?.Trim() ?? "0.0.0.0";
            int port = Math.Clamp(settings.PortalPort, 1, 65535);

            if (ip is "0.0.0.0" or "*" or "+" or "")
            {
                listener.Prefixes.Add($"https://*:{port}/");
            }
            else
            {
                string host = IPAddress.TryParse(ip, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                    ? $"[{address}]" : ip;
                listener.Prefixes.Add($"https://{host}:{port}/");
            }

            listener.Start();
            dispatcher = new BoundedHttpDispatcher(ProcessRequestAsync);
        cts = new CancellationTokenSource();
            listenerTask = ListenLoopAsync(cts.Token);
            WindowsLogManager.Instance.WriteEntry($"[SelfServicePortal] Server started listening on port {port}",
                System.Diagnostics.EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[SelfServicePortal] Failed to start HTTPS server: {ex.Message}. Ensure a TLS certificate is bound to port {settings.PortalPort} (e.g. 'netsh http add sslcert ipport=0.0.0.0:{settings.PortalPort} certhash=<THUMBPRINT> appid={Guid.NewGuid():B}').",
                System.Diagnostics.EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
    }

    /// <summary>
    /// 停止自助解鎖門戶 HTTP 伺服器。
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
                    WindowsLogManager.Instance.WriteEntry($"[SelfServicePortal] Request error: {ex.Message}",
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
            IPAddress peer = request.RemoteEndPoint.Address;
            IPAddress source = TrustedProxyParser.ResolveClientIp(peer, request.Headers["Forwarded"], request.Headers["X-Forwarded-For"], settings.TrustedProxyCidrs);
            if (!TrustedProxyParser.IsTrustedProxy(peer, settings.TrustedProxyCidrs) || source.Equals(peer))
            {
                response.StatusCode = 403;
                return;
            }
            string clientIp = IpAddressCanonicalizer.Canonicalize(source.ToString());
            // Handle IPv6 loopback
            if (clientIp == "::1") clientIp = "127.0.0.1";

            string path = request.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

            if (request.HttpMethod == "GET" && (path is "/" or "/index.html"))
            {
                await ServePortalPageAsync(response, clientIp);
                return;
            }

            if (request.HttpMethod == "POST" && path is "/api/unblock")
            {
                await HandleUnblockApiAsync(request, response, clientIp);
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            byte[] notFound = Encoding.UTF8.GetBytes("404 Not Found");
            await response.OutputStream.WriteAsync(notFound);
        }
        catch (RequestBodyTooLargeException) { response.StatusCode = 413; }
        catch (JsonException) { response.StatusCode = 400; }
        catch (Exception)
        {
            try
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] err = Encoding.UTF8.GetBytes("500 Internal Server Error");
                await response.OutputStream.WriteAsync(err);
            }
            catch { }
        }
        finally
        {
            try { response.Close(); } catch { }
        }
    }

    private async Task ServePortalPageAsync(HttpListenerResponse response, string clientIp)
    {
        // 檢查 IP 狀態
        var lockInfo = Locks.GetActiveLockByIp(clientIp);
        int lockStatus = lockInfo?.Status ?? 0;
        string statusMessage;
        string statusColor;
        bool canUnblock = false;

        if (lockStatus == Shared.Lock.LOCK_STATUS_SOFTLOCK || lockStatus == Shared.Lock.LOCK_STATUS_SOFTLOCK_REQUESTED)
        {
            statusMessage = "⚠️ 您的來源 IP 目前處於「軟封鎖 (SoftLock)」狀態，請輸入動態驗證碼解除封鎖。";
            statusColor = "#f59e0b"; // amber
            canUnblock = true;
        }
        else if (lockStatus == Shared.Lock.LOCK_STATUS_HARDLOCK || lockStatus == Shared.Lock.LOCK_STATUS_HARDLOCK_REQUESTED)
        {
            statusMessage = "⛔ 您的來源 IP 處於「永久硬封鎖」狀態，無法透過自助門戶解鎖，請聯繫系統管理員。";
            statusColor = "#ef4444"; // red
            canUnblock = false;
        }
        else
        {
            statusMessage = "✅ 您的來源 IP 目前正常，未受到任何防火牆存取限制。";
            statusColor = "#10b981"; // emerald
            canUnblock = false;
        }

        string unblockFormHtml = canUnblock ? GetUnblockFormHtml() : string.Empty;
        string encodedClientIp = System.Net.WebUtility.HtmlEncode(clientIp);

        string html = $$"""
        <!DOCTYPE html>
        <html lang="zh-TW">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>IDDS Community - 自助驗證解鎖門戶</title>
          <style>
            * { box-sizing: border-box; margin: 0; padding: 0; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; }
            body { background-color: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; min-height: 100vh; padding: 20px; }
            .card { background-color: #1e293b; border-radius: 12px; border: 1px solid #334155; max-width: 480px; width: 100%; padding: 32px; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5); }
            .header { text-align: center; margin-bottom: 24px; }
            .header h1 { font-size: 20px; font-weight: 700; color: #14b8a6; margin-bottom: 8px; }
            .header p { font-size: 13px; color: #94a3b8; }
            .status-box { background: rgba(0,0,0,0.2); border-left: 4px solid {{statusColor}}; padding: 14px; border-radius: 6px; margin-bottom: 20px; font-size: 14px; line-height: 1.5; }
            .ip-badge { background: #0f172a; color: #38bdf8; padding: 4px 10px; border-radius: 4px; font-family: monospace; font-size: 14px; display: inline-block; margin-bottom: 12px; }
            .form-group { margin-bottom: 20px; }
            label { display: block; font-size: 13px; font-weight: 600; color: #cbd5e1; margin-bottom: 8px; }
            input[type="text"] { width: 100%; padding: 12px 16px; background: #0f172a; border: 1px solid #475569; border-radius: 8px; color: #fff; font-size: 20px; text-align: center; letter-spacing: 4px; font-family: monospace; }
            input[type="text"]:focus { outline: none; border-color: #14b8a6; }
            button { width: 100%; padding: 12px; background: #14b8a6; color: #0f172a; font-weight: 700; border: none; border-radius: 8px; font-size: 15px; cursor: pointer; transition: background 0.2s; }
            button:hover { background: #0d9488; }
            button:disabled { background: #475569; cursor: not-allowed; }
            .footer { margin-top: 24px; text-align: center; font-size: 12px; color: #64748b; }
            .msg { margin-top: 16px; padding: 10px; border-radius: 6px; font-size: 13px; display: none; text-align: center; }
            .msg.success { background: #064e3b; color: #6ee7b7; display: block; }
            .msg.error { background: #7f1d1d; color: #fca5a5; display: block; }
          </style>
        </head>
        <body>
          <div class="card">
            <div class="header">
              <h1>🛡️ IDDS Community</h1>
              <p>伺服器安全防禦系統 - 合法使用者自助解鎖門戶</p>
            </div>

            <div style="text-align: center;">
              <span class="ip-badge">偵測到的來源 IP: {{encodedClientIp}}</span>
            </div>

            <div class="status-box">
              {{statusMessage}}
            </div>

            {{unblockFormHtml}}

            <div class="footer">
              IDDS Community &copy; 2026 Enterprise Perimeter Defense. All rights reserved.
            </div>
          </div>
        </body>
        </html>
        """;

        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'unsafe-inline'; script-src 'unsafe-inline';";
        response.ContentType = "text/html; charset=utf-8";
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
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

    private async Task HandleUnblockApiAsync(HttpListenerRequest request, HttpListenerResponse response, string clientIp)
    {

        string body = await BoundedHttpDispatcher.ReadBodyAsync(request);

        using JsonDocument document = JsonDocument.Parse(body);
        string code = document.RootElement.TryGetProperty("code", out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        (HttpStatusCode status, bool success, string message) result;
        lock (attemptGate) result = EvaluateUnblock(clientIp, code);
        await SendJsonResponseAsync(response, result.status, result.success, result.message);
    }

    private (HttpStatusCode, bool, string) EvaluateUnblock(string clientIp, string code)
    {
        DateTime now = DateTime.UtcNow;
        if (now >= nextAttemptCleanup)
        {
            foreach (string expired in failedAttempts.Where(p => p.Value.Expires <= now).Select(p => p.Key).ToArray()) failedAttempts.Remove(expired);
            nextAttemptCleanup = now.AddMinutes(1);
        }
        if (failedAttempts.Count >= 10000 && !failedAttempts.ContainsKey(clientIp))
            return (HttpStatusCode.TooManyRequests, false, "驗證忙碌，請稍後重試。");
        int maximum = Math.Clamp(settings.MaxFailedAttempts, 1, 20);
        int count = failedAttempts.TryGetValue(clientIp, out var state) && state.Expires > now ? state.Count : 0;
        if (count >= maximum) return (HttpStatusCode.Forbidden, false, "已超過驗證嘗試上限。");
        if (!TotpAuthenticator.VerifyCodeAndBurn(settings.TotpBase32Secret, code, clientIp))
        {
            count++;
            failedAttempts[clientIp] = (count, now.AddMinutes(15));
            if (count >= maximum)
            {
                Locks.CreateLock(now, DateTime.MaxValue, 0, Shared.Lock.LOCK_STATUS_HARDLOCK_REQUESTED, 0, clientIp);
                return (HttpStatusCode.Forbidden, false, "已超過嘗試上限，已提出永久硬封鎖要求。");
            }
            return (HttpStatusCode.BadRequest, false, $"動態密碼驗證錯誤，剩餘嘗試次數：{maximum - count} 次。");
        }
        failedAttempts.Remove(clientIp);
        bool accepted = Locks.UnlockIp(clientIp, softOnly: true);
        return accepted
            ? (HttpStatusCode.Accepted, true, "解除封鎖要求已接受，請稍後查詢處理結果。")
            : (HttpStatusCode.Conflict, false, "目前沒有可供自助解除的軟封鎖；硬封鎖請聯繫管理員。");
    }
    private static async Task SendJsonResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, bool success, string message)
    {
        response.StatusCode = (int)statusCode;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        response.ContentType = "application/json; charset=utf-8";
        string json = JsonSerializer.Serialize(new { success, message });
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }

    private static string GetUnblockFormHtml() => """
    <form id="unblockForm">
      <div class="form-group">
        <label for="totpCode">請輸入 Authenticator 6 位數即時動態碼：</label>
        <input type="text" id="totpCode" maxlength="6" placeholder="------" autocomplete="off" autofocus required />
      </div>
      <button type="submit" id="btnSubmit">立即解除軟封鎖</button>
      <div id="resultMsg" class="msg"></div>
    </form>
    <script>
      document.getElementById('unblockForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const btn = document.getElementById('btnSubmit');
        const msg = document.getElementById('resultMsg');
        const code = document.getElementById('totpCode').value.trim();
        btn.disabled = true;
        msg.className = 'msg';
        msg.style.display = 'none';

        try {
          const res = await fetch('/api/unblock', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code: code })
          });
          const data = await res.json();
          if (res.ok && data.success) {
            msg.className = 'msg success';
            msg.textContent = '🎉 ' + data.message;
            setTimeout(() => location.reload(), 3000);
          } else {
            msg.className = 'msg error';
            msg.textContent = '❌ ' + (data.message || '驗證失敗');
            btn.disabled = false;
          }
        } catch (err) {
          msg.className = 'msg error';
          msg.textContent = '❌ 網路連線錯誤，請稍後重試。';
          btn.disabled = false;
        }
      });
    </script>
    """;

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
