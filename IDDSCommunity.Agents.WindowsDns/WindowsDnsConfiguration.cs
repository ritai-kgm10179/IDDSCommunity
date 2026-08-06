using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsDns;

/// <summary>
/// 定義 Windows DNS 伺服器活動的界限偵測視窗與門檻值。
/// </summary>
public sealed class WindowsDnsConfiguration : PluginConfiguration
{
    public int WindowSeconds { get; set; } = 60;
    public int QueryRateThreshold { get; set; } = 1000;
    public int NxDomainThreshold { get; set; } = 100;
    public int AnyQueryThreshold { get; set; } = 25;
    public int DynamicUpdateThreshold { get; set; } = 10;
    public int ZoneTransferThreshold { get; set; } = 1;
    public int MaximumTrackedClients { get; set; } = 10000;

    /// <summary>
    /// 驗證每個偵測界限均為正數且記憶體使用量維持在界限內。
    /// </summary>
    internal void Validate()
    {
        if (WindowSeconds is < 1 or > 3600)
            throw new InvalidOperationException(DnsStrings.Get("DNS detection window must be between 1 and 3600 seconds."));
        if (QueryRateThreshold < 1 || NxDomainThreshold < 1 || AnyQueryThreshold < 1 || DynamicUpdateThreshold < 1 || ZoneTransferThreshold < 1)
            throw new InvalidOperationException(DnsStrings.Get("DNS detection thresholds must be greater than zero."));
        if (MaximumTrackedClients is < 100 or > 1000000)
            throw new InvalidOperationException(DnsStrings.Get("DNS tracked client capacity must be between 100 and 1000000."));
    }
}
