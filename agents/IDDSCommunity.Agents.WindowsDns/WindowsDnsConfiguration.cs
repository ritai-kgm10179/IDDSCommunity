using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsDns;
/// <summary>
/// 定義 Windows DNS 伺服器活動的界限偵測視窗與門檻值。
/// </summary>
public sealed class WindowsDnsConfiguration : PluginConfiguration
{
    /// <summary>
    /// 取得或設定滑動偵測時間窗的長度，單位為秒。
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
    /// <summary>
    /// 取得或設定時間窗內判定為異常查詢速率所需的查詢次數門檻。
    /// </summary>
    public int QueryRateThreshold { get; set; } = 1000;
    /// <summary>
    /// 取得或設定時間窗內判定為異常 NXDOMAIN 比率所需的次數門檻。
    /// </summary>
    public int NxDomainThreshold { get; set; } = 100;
    /// <summary>
    /// 取得或設定時間窗內判定為異常 ANY 查詢速率所需的次數門檻。
    /// </summary>
    public int AnyQueryThreshold { get; set; } = 25;
    /// <summary>
    /// 取得或設定時間窗內判定為異常動態更新速率所需的次數門檻。
    /// </summary>
    public int DynamicUpdateThreshold { get; set; } = 10;
    /// <summary>
    /// 取得或設定時間窗內判定為異常區域傳送速率所需的次數門檻。
    /// </summary>
    public int ZoneTransferThreshold { get; set; } = 1;
    /// <summary>
    /// 取得或設定同時追蹤之用戶端來源位址數量上限。
    /// </summary>
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
