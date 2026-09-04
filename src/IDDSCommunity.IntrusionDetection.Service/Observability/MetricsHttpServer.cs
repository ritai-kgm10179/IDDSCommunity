using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.Observability;

/// <summary>
/// 提供 Prometheus / OpenMetrics 指標收集與健康狀態檢查端點之內嵌 HTTP 伺服器。
/// </summary>
public sealed class MetricsHttpServer : IDisposable
{
    private readonly NotificationSettings settings;
    private readonly Database database;
    private readonly DateTime startTimeUtc = DateTime.UtcNow;
    private HttpListener? listener;
    private CancellationTokenSource? cts;
    private bool disposed;

    /// <summary>
    /// 初始化 <see cref="MetricsHttpServer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">通知與觀測性設定模型。</param>
    /// <param name="database">主資料庫執行個體。</param>
    public MetricsHttpServer(NotificationSettings settings, Database database)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(database);
        this.settings = settings;
        this.database = database;
    }

    /// <summary>
    /// 取得伺服器目前是否處於監聽狀態。
    /// </summary>
    public bool IsListening => listener != null && listener.IsListening;

    /// <summary>
    /// 啟動 Metrics HTTP 伺服器監聽。
    /// </summary>
    public void Start()
    {
        if (!settings.EnableMetricsEndpoint) return;

        Stop();
        cts = new CancellationTokenSource();

        try
        {
            listener = new HttpListener();
            string listenIp = string.IsNullOrWhiteSpace(settings.MetricsListenIp) ? "0.0.0.0" : settings.MetricsListenIp.Trim();
            int port = settings.MetricsPort > 0 ? settings.MetricsPort : 9100;

            string prefix = listenIp == "0.0.0.0" || listenIp == "*" || listenIp == "+"
                ? $"http://*:{port}/"
                : $"http://{listenIp}:{port}/";

            listener.Prefixes.Add(prefix);
            listener.Start();

            _ = ListenAsync(listener, cts.Token);
            System.Diagnostics.Trace.TraceInformation("MetricsHttpServer started on {0}", prefix);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Failed to start MetricsHttpServer: {0}", ex.Message);
        }
    }

    /// <summary>
    /// 停止 Metrics HTTP 伺服器。
    /// </summary>
    public void Stop()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (listener != null)
        {
            try
            {
                listener.Stop();
                listener.Close();
            }
            catch { }
            listener = null;
        }
    }

    private async Task ListenAsync(HttpListener httpListener, CancellationToken token)
    {
        while (!token.IsCancellationRequested && httpListener.IsListening)
        {
            try
            {
                HttpListenerContext context = await httpListener.GetContextAsync().ConfigureAwait(false);
                _ = ProcessRequestAsync(context);
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) break;
                System.Diagnostics.Trace.TraceWarning("MetricsHttpServer exception: {0}", ex.Message);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            IPAddress? remoteIp = context.Request.RemoteEndPoint.Address;
            if (remoteIp.IsIPv4MappedToIPv6)
                remoteIp = remoteIp.MapToIPv4();

            // 驗證來源 IP 是否符合允許清單
            if (!IsIpAllowed(remoteIp))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                byte[] forbidden = Encoding.UTF8.GetBytes("Forbidden: IP not in allowed networks\n");
                await context.Response.OutputStream.WriteAsync(forbidden).ConfigureAwait(false);
                context.Response.Close();
                return;
            }

            string path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(path)) path = "/";
            string method = context.Request.HttpMethod.ToUpperInvariant();

            if (path is "/" or "/health" or "/healthz")
            {
                if (method != "GET" && method != "HEAD")
                {
                    context.Response.Headers["Allow"] = "GET, HEAD";
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    byte[] notAllowed = Encoding.UTF8.GetBytes("{\"error\":\"Method Not Allowed\"}\n");
                    await context.Response.OutputStream.WriteAsync(notAllowed).ConfigureAwait(false);
                }
                else
                {
                    string healthJson = path == "/"
                        ? $"{{\"status\":\"healthy\",\"uptime_seconds\":{(int)(DateTime.UtcNow - startTimeUtc).TotalSeconds},\"endpoints\":[\"/metrics\",\"/healthz\"]}}\n"
                        : $"{{\"status\":\"healthy\",\"uptime_seconds\":{(int)(DateTime.UtcNow - startTimeUtc).TotalSeconds}}}\n";

                    byte[] buffer = Encoding.UTF8.GetBytes(healthJson);
                    context.Response.ContentType = "application/json; charset=utf-8";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    if (method != "HEAD")
                    {
                        await context.Response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
                    }
                }
            }
            else if (path.Equals("/metrics", StringComparison.OrdinalIgnoreCase))
            {
                if (method != "GET" && method != "HEAD")
                {
                    context.Response.Headers["Allow"] = "GET, HEAD";
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    byte[] notAllowed = Encoding.UTF8.GetBytes("{\"error\":\"Method Not Allowed\"}\n");
                    await context.Response.OutputStream.WriteAsync(notAllowed).ConfigureAwait(false);
                }
                else
                {
                    context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    if (method != "HEAD")
                    {
                        string metricsText = BuildMetricsText();
                        byte[] buffer = Encoding.UTF8.GetBytes(metricsText);
                        await context.Response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "application/json; charset=utf-8";
                byte[] notFound = Encoding.UTF8.GetBytes("{\"error\":\"Not Found\"}\n");
                await context.Response.OutputStream.WriteAsync(notFound).ConfigureAwait(false);
            }

            context.Response.Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Error processing Metrics request: {0}", ex.Message);
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch { }
        }
    }

    private bool IsIpAllowed(IPAddress remoteIp)
    {
        string allowed = settings.MetricsAllowedNetworks;
        if (string.IsNullOrWhiteSpace(allowed))
            return true; // 若未設白名單則允許所有連線

        if (IPAddress.IsLoopback(remoteIp))
            return true;

        string[] tokens = allowed.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (IPNetwork.TryParse(token.Trim(), out IPNetwork network) && network.Contains(remoteIp))
                return true;
            if (IPAddress.TryParse(token.Trim(), out IPAddress? singleIp) && singleIp.Equals(remoteIp))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 建構符合 OpenMetrics / Prometheus 標準之文字指標輸出。
    /// </summary>
    /// <returns>Prometheus 格式之指標字串。</returns>
    public string BuildMetricsText()
    {
        var sb = new StringBuilder();
        double uptime = (DateTime.UtcNow - startTimeUtc).TotalSeconds;

        sb.AppendLine("# HELP idds_uptime_seconds Total seconds IDDS Community protection service has been running.");
        sb.AppendLine("# TYPE idds_uptime_seconds counter");
        sb.AppendLine($"idds_uptime_seconds {uptime:F1}");
        sb.AppendLine();

        sb.AppendLine("# HELP idds_active_firewall_blocks Current number of active IP blocking rules in Windows Firewall.");
        sb.AppendLine("# TYPE idds_active_firewall_blocks gauge");
        int activeBlocks = 0;
        try { activeBlocks = Locks.GetActiveLocks().Count; }
        catch { }
        sb.AppendLine($"idds_active_firewall_blocks {activeBlocks}");
        sb.AppendLine();

        sb.AppendLine("# HELP idds_probation_ips_total Number of IPs currently in probation observation.");
        sb.AppendLine("# TYPE idds_probation_ips_total gauge");
        int probationCount = 0;
        try
        {
            object? res = database.ExecuteScalar("select count(*) from Locks where status = @p0", Shared.Lock.LOCK_STATUS_PROBATION);
            if (res != null && int.TryParse(res.ToString(), out int cnt))
                probationCount = cnt;
        }
        catch { }
        sb.AppendLine($"idds_probation_ips_total {probationCount}");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// 釋放非受控資源。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        Stop();
        disposed = true;
    }
}
