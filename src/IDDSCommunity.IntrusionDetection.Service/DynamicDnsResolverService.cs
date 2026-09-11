using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供安全網路中動態主機名稱 (DDNS FQDN) 背景非同步定時解析與白名單同步服務。
/// </summary>
internal sealed class DynamicDnsResolverService : IDisposable
{
    private readonly IddsConfig config;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logWarning;
    private readonly Action<string, string, string, string?>? recordAudit;
    private readonly CancellationTokenSource stopping = new();
    private System.Threading.Timer? timer;
    private int resolving;
    private bool disposed;

    /// <summary>
    /// 初始化 <see cref="DynamicDnsResolverService"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">全域設定執行個體。</param>
    /// <param name="logInformation">資訊日誌回報委派。</param>
    /// <param name="logWarning">警告日誌回報委派。</param>
    /// <param name="recordAudit">可選之稽核日誌回報委派。</param>
    public DynamicDnsResolverService(
        IddsConfig config,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null,
        Action<string, string, string, string?>? recordAudit = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.logInformation = logInformation ?? (msg => Trace.TraceInformation(msg));
        this.logWarning = logWarning ?? ((msg, ex) => Trace.TraceWarning("{0}: {1}", msg, ex.Message));
        this.recordAudit = recordAudit;
    }

    /// <summary>
    /// 啟動動態 DNS 背景解析排程。
    /// </summary>
    public void Start()
    {
        if (disposed) return;
        int intervalMinutes = Math.Max(1, config.DynamicDnsIntervalMinutes);
        timer = new System.Threading.Timer(
            async _ => await RefreshAsync().ConfigureAwait(false),
            null,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMinutes(intervalMinutes));
    }

    /// <summary>
    /// 立即非同步執行一次動態主機名稱解析與快取更新。
    /// </summary>
    /// <returns>表示非同步作業完成之 Task。</returns>
    public async Task RefreshAsync()
    {
        if (disposed || stopping.IsCancellationRequested)
            return;

        if (Interlocked.Exchange(ref resolving, 1) != 0)
            return;

        try
        {
            if (!config.UseSafeNetworkList || config.SafeNetworks.Count == 0)
            {
                DynamicDnsCache.Clear();
                return;
            }

            List<IddsConfig.CSafeNetwork> snapshot;
            try
            {
                snapshot = [.. config.SafeNetworks];
            }
            catch
            {
                return;
            }

            List<string> activeHosts = [];
            foreach (IddsConfig.CSafeNetwork item in snapshot)
            {
                if (stopping.IsCancellationRequested) break;

                string host = item.IpAddress?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(host) || IPAddress.TryParse(host, out _) || Uri.CheckHostName(host) != UriHostNameType.Dns)
                    continue;

                activeHosts.Add(host);
            }

            if (activeHosts.Count > 0)
            {
                List<Task> resolveTasks = [];
                foreach (string host in activeHosts)
                {
                    resolveTasks.Add(ResolveHostAsync(host));
                }
                await Task.WhenAll(resolveTasks).ConfigureAwait(false);
            }

            DynamicDnsCache.PruneExcept(activeHosts);
        }
        catch (Exception ex)
        {
            logWarning("Error occurred during dynamic DNS resolution cycle", ex);
        }
        finally
        {
            Interlocked.Exchange(ref resolving, 0);
        }
    }

    private async Task ResolveHostAsync(string host)
    {
        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cts.Token).ConfigureAwait(false);
            if (addresses != null && addresses.Length > 0)
            {
                bool hasPrior = DynamicDnsCache.TryGetResolvedIps(host, out HashSet<IPAddress> priorIps);
                bool changed = !hasPrior || !priorIps.SetEquals(addresses);

                DynamicDnsCache.Update(host, addresses);

                if (changed)
                {
                    recordAudit?.Invoke("DynamicDns.Resolve", "Succeeded", host, $"{addresses.Length} IPs: {string.Join(", ", (IEnumerable<IPAddress>)addresses)}");
                }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // 系統關閉中，略過
        }
        catch (Exception ex)
        {
            logWarning($"Failed to resolve dynamic safe-network host '{host}'", ex);
            recordAudit?.Invoke("DynamicDns.Resolve", "Failed", host, ex.Message);
        }
    }

    /// <summary>
    /// 停止解析服務並釋放定時器資源。
    /// </summary>
    public void Stop()
    {
        stopping.Cancel();
        timer?.Change(Timeout.Infinite, Timeout.Infinite);
        timer?.Dispose();
        timer = null;
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
            stopping.Dispose();
        }
    }
}
