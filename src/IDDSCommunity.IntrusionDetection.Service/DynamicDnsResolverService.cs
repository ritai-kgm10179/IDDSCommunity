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
        if (Interlocked.Exchange(ref resolving, 1) != 0)
            return;

        try
        {
            if (!config.UseSafeNetworkList || config.SafeNetworks.Count == 0)
                return;

            List<IddsConfig.CSafeNetwork> snapshot;
            try
            {
                snapshot = [.. config.SafeNetworks];
            }
            catch
            {
                return;
            }

            foreach (IddsConfig.CSafeNetwork item in snapshot)
            {
                string host = item.IpAddress?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(host) || IPAddress.TryParse(host, out _))
                    continue;

                try
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cts.Token).ConfigureAwait(false);
                    if (addresses != null && addresses.Length > 0)
                    {
                        DynamicDnsCache.Update(host, addresses);
                        recordAudit?.Invoke("DynamicDns.Resolve", "Succeeded", host, $"{addresses.Length} IPs: {string.Join(", ", (IEnumerable<IPAddress>)addresses)}");
                    }
                }
                catch (Exception ex)
                {
                    logWarning($"Failed to resolve dynamic safe-network host '{host}'", ex);
                    recordAudit?.Invoke("DynamicDns.Resolve", "Failed", host, ex.Message);
                }
            }
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

    /// <summary>
    /// 停止解析服務並釋放定時器資源。
    /// </summary>
    public void Stop()
    {
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
        }
    }
}
