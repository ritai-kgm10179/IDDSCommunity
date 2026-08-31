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
    private readonly ThreatHubClient client;
    private readonly ConcurrentQueue<ThreatIntelligenceItem> pendingLocalThreats = new();

    private System.Threading.Timer? syncTimer;
    private int syncing;
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
    public ThreatIntelligenceSyncService(
        IddsConfig config,
        Action<ThreatIntelligenceItem> onClusterThreatReceived,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null,
        ThreatHubClient? client = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.onClusterThreatReceived = onClusterThreatReceived ?? throw new ArgumentNullException(nameof(onClusterThreatReceived));
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logWarning = logWarning ?? ((msg, ex) => System.Diagnostics.Trace.TraceWarning("{0}: {1}", msg, ex.Message));
        this.client = client ?? new ThreatHubClient();
    }

    /// <summary>
    /// 將本機產生之硬封鎖威脅推入待同步佇列。
    /// </summary>
    /// <param name="item">本機威脅情資項目。</param>
    public void EnqueueLocalThreat(ThreatIntelligenceItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.SourceIp)) return;
        pendingLocalThreats.Enqueue(item);
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
        if (Interlocked.Exchange(ref syncing, 1) != 0)
            return;

        try
        {
            if (config.ThreatHubRole != ThreatHubRole.EdgeNode || string.IsNullOrWhiteSpace(config.ThreatHubEndpoint))
                return;

            string[] endpoints = config.ThreatHubEndpoint.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (endpoints.Length == 0) return;

            List<ThreatIntelligenceItem> localBatch = [];
            while (pendingLocalThreats.TryDequeue(out ThreatIntelligenceItem? threat))
            {
                if (threat != null) localBatch.Add(threat);
            }

            ThreatHubSyncPayload payload = new()
            {
                NodeId = Environment.MachineName + "_" + config.ThreatHubApiKey[..Math.Min(8, config.ThreatHubApiKey.Length)],
                NodeName = Environment.MachineName,
                NodeIp = string.Empty,
                LastSyncUtc = lastSyncUtc,
                NewThreats = localBatch
            };

            bool syncSucceeded = false;
            foreach (string endpoint in endpoints)
            {
                try
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
                    ThreatHubSyncResponse response = await client.SynchronizeAsync(
                        endpoint,
                        config.ThreatHubApiKey,
                        payload,
                        cts.Token).ConfigureAwait(false);

                    if (response.Success)
                    {
                        lastSyncUtc = response.ServerTimeUtc;
                        foreach (ThreatIntelligenceItem clusterThreat in response.ActiveThreats)
                        {
                            if (string.IsNullOrWhiteSpace(clusterThreat.SourceIp)) continue;
                            onClusterThreatReceived(clusterThreat);
                        }
                        syncSucceeded = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logWarning($"Threat Hub failover: endpoint '{endpoint}' unavailable, trying next.", ex);
                }
            }

            if (!syncSucceeded)
            {
                // 將未成功同步之本機威脅重新放回佇列
                foreach (ThreatIntelligenceItem item in localBatch)
                {
                    pendingLocalThreats.Enqueue(item);
                }
            }
        }
        catch (Exception ex)
        {
            logWarning("Failed to synchronize with Threat Hub", ex);
        }
        finally
        {
            Interlocked.Exchange(ref syncing, 0);
        }
    }

    /// <summary>
    /// 停止同步排程。
    /// </summary>
    public void Stop()
    {
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
            client.Dispose();
        }
    }
}
