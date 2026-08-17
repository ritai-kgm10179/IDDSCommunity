using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 提供共用驗證失敗偵測框架所需之滑動時間窗、門檻值與追蹤容量等基礎設定。
/// </summary>
public class AuthenticationAgentConfiguration : PluginConfiguration
{
    /// <summary>
    /// 取得或設定滑動偵測時間窗的長度，單位為秒。
    /// </summary>
    public int WindowSeconds { get; set; } = 300;
    /// <summary>
    /// 取得或設定於偵測時間窗內判定達到攻擊門檻所需的失敗次數。
    /// </summary>
    public int FailureThreshold { get; set; } = 10;
    /// <summary>
    /// 取得或設定同時追蹤之來源位址數量上限。
    /// </summary>
    public int MaximumTrackedSources { get; set; } = 10000;
    /// <summary>
    /// 取得或設定閒置來源狀態於清除前的保留時間，單位為秒。
    /// </summary>
    public int SourceStateRetentionSeconds { get; set; } = 1800;

    /// <summary>
    /// 驗證目前設定值是否落於允許範圍內。
    /// </summary>
    /// <exception cref="InvalidOperationException">任一設定值超出允許範圍。</exception>
    public virtual void Validate()
    {
        if (WindowSeconds is < 10 or > 86400)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Detection window must be between 10 and 86400 seconds."));
        if (FailureThreshold is < 2 or > 100000)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Failure threshold must be between 2 and 100000."));
        if (MaximumTrackedSources is < 100 or > 1000000)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Tracked source capacity must be between 100 and 1000000."));
        if (SourceStateRetentionSeconds < WindowSeconds || SourceStateRetentionSeconds > 604800)
            throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Source state retention must be at least the detection window and no more than 604800 seconds."));
    }
}
