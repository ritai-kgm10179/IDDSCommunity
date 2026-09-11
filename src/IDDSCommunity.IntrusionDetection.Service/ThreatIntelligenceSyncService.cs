using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供邊緣節點（Edge Node）定期與威脅情資中繼中心（Threat Hub）同步之背景服務。
/// </summary>
internal sealed class ThreatIntelligenceSyncService : IDisposable
{
    private readonly IddsConfig config;
    private readonly Action<ThreatIntelligenceItem> onClusterThreatReceived;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logWarning;
    private readonly Action<string, string, string, string?>? recordAudit;
    private readonly ThreatHubClient client;
    private readonly ThreatHubStore localStore;
    private readonly Dictionary<string, (long Cursor, string Generation, long LocalCursor)> cursors = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource stopping = new();
    private readonly SemaphoreSlim syncGate = new(1, 1);

    private System.Threading.Timer? syncTimer;
    private bool disposed;
    private DateTime lastSyncUtc = DateTime.MinValue;

    /// <summary>
    /// 初始化 <see cref="ThreatIntelligenceSyncService"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域設定執行個體。</param>
    /// <param name="onClusterThreatReceived">當自 Hub 收到叢集威脅時執行之回呼委派。</param>
    /// <param name="logInformation">資訊日誌委派。</param>
    /// <param name="logWarning">警告日誌委派。</param>
    /// <param name="client">可選之 ThreatHubClient 執行個體。</param>
    /// <param name="recordAudit">可選之稽核日誌回報委派。</param>
    /// <param name="database">持久化資料庫；測試可省略以使用有限記憶體儲存。</param>
    public ThreatIntelligenceSyncService(
        IddsConfig config,
        Action<ThreatIntelligenceItem> onClusterThreatReceived,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null,
        ThreatHubClient? client = null,
        Action<string, string, string, string?>? recordAudit = null, Database? database = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.onClusterThreatReceived = onClusterThreatReceived ?? throw new ArgumentNullException(nameof(onClusterThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logWarning = logWarning ?? ((msg, ex) => System.Diagnostics.Trace.TraceWarning("{0}: {1}", msg, ex.Message));
        this.client = client ?? new ThreatHubClient();
        this.recordAudit = recordAudit;
        this.database = database;
        localStore = new ThreatHubStore(database);
        if (database is not null && database.IsConfigured)
        {
            try
            {
                var saved = database.Query<CursorRow>("SELECT Endpoint, Cursor, Generation, LocalCursor FROM ThreatHubCursors");
                foreach (var row in saved)
                {
                    cursors[row.Endpoint] = (row.Cursor, row.Generation, row.LocalCursor);
                }
            }
            catch (Exception ex)
            {
                this.logWarning("Failed to load ThreatHub cursors from database", ex);
            }
        }
    }

    private readonly Database? database;

    private sealed class CursorRow
    {
        public string Endpoint { get; set; } = string.Empty;
        public long Cursor { get; set; }
        public string Generation { get; set; } = string.Empty;
        public long LocalCursor { get; set; }
    }

    /// <summary>
    /// 將本機產生之硬封鎖威脅推入待同步佇列。
    /// </summary>
    /// <param name="item">本機威脅情資項目。</param>
    public void EnqueueLocalThreat(ThreatIntelligenceItem item)
    {
        if (item is null || !IPAddress.TryParse(item.SourceIp, out var ip) || BogonIpFilter.IsBogonOrReserved(ip)
            || config.IsInSafeNetwork(ip.ToString())) return;
        var copy = System.Text.Json.JsonSerializer.Deserialize<ThreatIntelligenceItem>(System.Text.Json.JsonSerializer.Serialize(item))!;
        copy.SourceIp = IpAddressCanonicalizer.Canonicalize(ip).ToString();
        int ttlDays = Math.Clamp(config.ThreatFeedTtlDays, 1, 365);
        if (copy.ExpiresUtc == default || copy.ExpiresUtc <= DateTime.UtcNow || copy.ExpiresUtc == DateTime.MaxValue)
        {
            copy.ExpiresUtc = (copy.ReportedUtc > DateTime.MinValue ? copy.ReportedUtc : DateTime.UtcNow).AddDays(ttlDays);
        }
        if (!localStore.Upsert(copy)) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Pending threat capacity has been reached."));
    }

    /// <summary>
    /// 啟動與 Threat Hub 之定時同步排程。
    /// </summary>
    public void Start()
    {
        if (disposed) return;
        int intervalSeconds = Math.Max(5, config.ThreatHubSyncIntervalSeconds);
        syncTimer = new System.Threading.Timer(
            async _ => await SynchronizeNowAsync().ConfigureAwait(false),
            null,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(intervalSeconds));
    }

    /// <summary>
    /// 立即執行一次與 Threat Hub 的雙向威脅同步。
    /// </summary>
    /// <returns>表示非同步作業完成之 Task。</returns>
    public async Task SynchronizeNowAsync()
    {
        if (disposed || !await syncGate.WaitAsync(0).ConfigureAwait(false)) return;
        try
        {
            if (stopping.IsCancellationRequested || config.ThreatHubRole != ThreatHubRole.EdgeNode || string.IsNullOrWhiteSpace(config.ThreatHubEndpoint)) return;
            string[] endpoints = config.ThreatHubEndpoint.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (endpoints.Length > 8) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("At most eight Threat Hub endpoints are supported."));
            foreach (string stale in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(cursors.Keys, key => !System.Linq.Enumerable.Contains(endpoints, key)))) cursors.Remove(stale);
            foreach (string endpoint in endpoints)
            {
                try
                {
                    var previous = cursors.GetValueOrDefault(endpoint, (0L, string.Empty, 0L));
                    ThreatHubSyncResponse localPage = localStore.ReadPage(previous.Item3, localStore.Generation);
                    using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    deadline.CancelAfter(TimeSpan.FromSeconds(15));
                    var payload = new ThreatHubSyncPayload
                    {
                        NodeId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Environment.MachineName))),
                        NodeName = Environment.MachineName,
                        LastSyncUtc = lastSyncUtc,
                        Cursor = previous.Item1,
                        Generation = previous.Item2,
                        NewThreats = localPage.ActiveThreats
                    };
                    ThreatHubSyncResponse response = await client.SynchronizeAsync(endpoint, config.ThreatHubApiKey, payload, deadline.Token).ConfigureAwait(false);
                    if (!response.Success) continue;
                    if (response.ActiveThreats is null || response.ActiveThreats.Count > ThreatHubStore.PageSize || string.IsNullOrWhiteSpace(response.Generation)
                        || response.Generation.Length > 128 || response.NextCursor < 0
                        || (response.Generation == previous.Item2 && response.NextCursor < previous.Item1)) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Invalid Threat Hub page response."));
                    foreach (ThreatIntelligenceItem threat in response.ActiveThreats)
                    {
                        if (threat is null || !IPAddress.TryParse(threat.SourceIp, out var ip) || BogonIpFilter.IsBogonOrReserved(ip)
                            || config.IsInSafeNetwork(ip.ToString()) || threat.ExpiresUtc <= DateTime.UtcNow
                            || threat.ExpiresUtc > DateTime.UtcNow.AddDays(365) || !double.IsFinite(threat.ConfidenceScore)
                            || threat.ConfidenceScore < 0.8 || threat.ConfidenceScore > 1) continue;
                        onClusterThreatReceived(threat);
                    }
                    cursors[endpoint] = (response.NextCursor, response.Generation, localPage.NextCursor);
                    if (database is not null && database.IsConfigured)
                    {
                        try
                        {
                            database.ExecuteNonQuery(
                                "INSERT INTO ThreatHubCursors(Endpoint, Cursor, Generation, LocalCursor, UpdatedUtc) VALUES(@p0, @p1, @p2, @p3, @p4) ON CONFLICT(Endpoint) DO UPDATE SET Cursor=excluded.Cursor, Generation=excluded.Generation, LocalCursor=excluded.LocalCursor, UpdatedUtc=excluded.UpdatedUtc",
                                endpoint, response.NextCursor, response.Generation, localPage.NextCursor, DateTime.UtcNow.ToString("O"));
                        }
                        catch (Exception ex)
                        {
                            logWarning("Failed to persist ThreatHub cursor to database", ex);
                        }
                    }
                    lastSyncUtc = response.ServerTimeUtc;
                    recordAudit?.Invoke("Cluster.Sync", "Succeeded", endpoint, $"Pushed: {localPage.ActiveThreats.Count}, Pulled: {response.ActiveThreats.Count}");
                    return;
                }
                catch (OperationCanceledException) when (stopping.IsCancellationRequested) { return; }
                catch (Exception ex) { logWarning($"Threat Hub endpoint '{endpoint}' unavailable.", ex); }
            }
            recordAudit?.Invoke("Cluster.Sync", "Failed", string.Empty, "No endpoint accepted the pending page");
        }
        finally { syncGate.Release(); }
    }
    /// <summary>
    /// 停止同步排程。
    /// </summary>
    public void Stop()
    {
        stopping.Cancel();
        syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        syncTimer?.Dispose();
        syncTimer = null;
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
            if (syncGate.Wait(TimeSpan.FromSeconds(5)))
            {
                try { client.Dispose(); } finally { syncGate.Release(); }
            }
            else
            {
                client.Dispose();
            }
            syncGate.Dispose();
            stopping.Dispose();
        }
    }
}
