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
    private ThreatIntelligenceHubServer? hubServer;
    private readonly DateTime startTimeUtc = DateTime.UtcNow;
    private HttpListener? listener;
    private BoundedHttpDispatcher? dispatcher;
    private CancellationTokenSource? cts;
    private bool disposed;

    /// <summary>
    /// 初始化 <see cref="MetricsHttpServer"/> 類別的新執行個體。
    /// </summary>
    /// <param name="settings">通知與觀測性設定模型。</param>
    /// <param name="database">主資料庫執行個體。</param>
    public MetricsHttpServer(NotificationSettings settings, Database database)
        : this(settings, database, null) { }

    /// <summary>
    /// 初始化 <see cref="MetricsHttpServer"/> 類別的新執行個體，並關聯威脅情資中繼中心伺服器。
    /// </summary>
    /// <param name="settings">通知與觀測性設定模型。</param>
    /// <param name="database">主資料庫執行個體。</param>
    /// <param name="hubServer">選用之威脅情資中繼中心伺服器；傳入時將額外輸出 Hub 節點數指標。</param>
    internal MetricsHttpServer(NotificationSettings settings, Database database, ThreatIntelligenceHubServer? hubServer)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(database);
        this.settings = settings;
        this.database = database;
        this.hubServer = hubServer;
    }

    /// <summary>
    /// 設定關聯的威脅情資中繼中心伺服器，用於輸出 Hub 節點數指標。
    /// 必須在 <see cref="Start"/> 之前呼叫。
    /// </summary>
    /// <param name="server">威脅情資中繼中心伺服器執行個體。</param>
    internal void SetHubServer(ThreatIntelligenceHubServer? server)
    {
        hubServer = server;
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

            dispatcher = new BoundedHttpDispatcher(ProcessRequestAsync);
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

        dispatcher?.Dispose();
        dispatcher = null;

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
                dispatcher?.Submit(context);
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
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";

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
                    string acceptHeader = context.Request.Headers["Accept"] ?? string.Empty;
                    bool isOpenMetrics = acceptHeader.Contains("application/openmetrics-text", StringComparison.OrdinalIgnoreCase);

                    if (isOpenMetrics)
                    {
                        context.Response.ContentType = "application/openmetrics-text; version=1.0.0; charset=utf-8";
                    }
                    else
                    {
                        context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    if (method != "HEAD")
                    {
                        string metricsText = BuildMetricsText(isOpenMetrics);
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
        if (IPAddress.IsLoopback(remoteIp))
            return true;

        string allowed = settings.MetricsAllowedNetworks;
        if (string.IsNullOrWhiteSpace(allowed))
            return false; // 安全預設值：若未設定允許網段，僅放行本機 Loopback 連線，杜絕未授權區域網路讀取內部指標

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
    /// <param name="isOpenMetrics">是否以 OpenMetrics 1.0.0 規範格式輸出（若為 <see langword="true"/> 結尾將包含 # EOF 標記）。</param>
    /// <returns>格式化之指標字串。</returns>
    public string BuildMetricsText(bool isOpenMetrics = false)
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

        if (hubServer != null)
        {
            sb.AppendLine("# HELP idds_threathub_connected_nodes Number of edge nodes currently registered in the Threat Hub.");
            sb.AppendLine("# TYPE idds_threathub_connected_nodes gauge");
            int connectedNodes = 0;
            try { connectedNodes = hubServer.RegisteredNodes.Count; }
            catch { }
            sb.AppendLine($"idds_threathub_connected_nodes {connectedNodes}");
            sb.AppendLine();

            sb.AppendLine("# HELP idds_threathub_active_threats Total active threat intelligence entries in the Threat Hub store.");
            sb.AppendLine("# TYPE idds_threathub_active_threats gauge");
            int hubThreats = 0;
            try { hubThreats = hubServer.ActiveThreats.Count; }
            catch { }
            sb.AppendLine($"idds_threathub_active_threats {hubThreats}");
            sb.AppendLine();
        }

        if (isOpenMetrics)
        {
            sb.AppendLine("# EOF");
        }

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
