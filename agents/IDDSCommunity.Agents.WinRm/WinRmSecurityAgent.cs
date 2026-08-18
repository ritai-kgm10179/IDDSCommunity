namespace IDDSCommunity.Agents.WinRm;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 監看 Windows Remote Management (WinRM)、PowerShell Remoting 與 Windows Admin Center (WAC) 之安全性與操作事件記錄，
/// 偵測遠端管理連線驗證失敗攻擊。
/// </summary>
[Plugin("Windows Remote Management (WinRM / WAC) Security Agent", "Detects repeated authentication failures from WinRM, PowerShell Remoting, and Windows Admin Center.", "1.0")]
public sealed class WinRmSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    private static readonly string WinRmOpQuery = "*[System[(EventID=142 or EventID=161 or EventID=192)]]";

    /// <summary>
    /// 初始化 <see cref="WinRmSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public WinRmSecurityAgent() : base(CreateDefaultEventSource()) { }

    /// <summary>
    /// 以自訂事件來源初始化 <see cref="WinRmSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal WinRmSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的名稱。
    /// </summary>
    public override string DisplayName
    {
        get => IntrusionDetection.Api.Localization.Strings.Get("Windows Remote Management (WinRM / WAC) Security Agent");
        set { }
    }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{47F3B59B-C578-4D05-8E1E-2B6B6D197F4A}");

    /// <summary>
    /// 取得一個值，指出此 Agent 是否使用內部本機門檻計數器。
    /// WinRM Agent 統一由 Phase 0 跨來源關聯引擎集中評估門檻，停用本機重複計數以確保每筆事件只計算一次。
    /// </summary>
    protected override bool UseLocalThresholdDetector => false;

    private static IAuthenticationEventSource CreateDefaultEventSource()
    {
        IEnumerable<string> trustedProxies = IddsConfig.Instance.GetTrustedProxyList();

        return new WindowsEventLogFailureSource(
            "Microsoft-Windows-WinRM/Operational",
            WinRmOpQuery,
            record => WinRmEventParser.Parse(record, trustedProxies));
    }
}
