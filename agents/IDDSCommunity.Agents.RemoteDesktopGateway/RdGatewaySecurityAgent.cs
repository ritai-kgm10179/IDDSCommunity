namespace IDDSCommunity.Agents.RemoteDesktopGateway;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 監看 Remote Desktop Gateway (RD Gateway)、TerminalServices-Gateway 與 Network Policy Server (NPS) 記錄，
/// 偵測透過遠端桌面閘道之未授權連線與暴力密碼嘗試。
/// </summary>
[Plugin("Remote Desktop Gateway Security Agent", "Detects repeated connection authorization and authentication failures from RD Gateway and NPS.", "1.0")]
public sealed class RdGatewaySecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    private static readonly string TsGatewayQuery = "*[System[(EventID=201 or EventID=202 or EventID=304)]]";
    private static readonly string NpsQuery = "*[System[(EventID=6273)]]";

    /// <summary>
    /// 初始化 <see cref="RdGatewaySecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public RdGatewaySecurityAgent() : base(CreateDefaultCompositeSource()) { }

    /// <summary>
    /// 以自訂事件來源初始化 <see cref="RdGatewaySecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal RdGatewaySecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的名稱。
    /// </summary>
    public override string DisplayName
    {
        get => IntrusionDetection.Api.Localization.Strings.Get("Remote Desktop Gateway Security Agent");
        set { }
    }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{B8E49A12-87C2-4FE5-912A-18F7D034E59C}");

    /// <summary>
    /// 取得一個值，指出此 Agent 是否使用內部本機門檻計數器。
    /// RD Gateway Agent 統一由 Phase 0 跨來源關聯引擎集中評估門檻，停用本機重複計數以確保每筆事件只計算一次。
    /// </summary>
    protected override bool UseLocalThresholdDetector => false;

    private static CompositeAuthenticationEventSource CreateDefaultCompositeSource()
    {
        IEnumerable<string> trustedProxies = IddsConfig.Instance.GetTrustedProxyList();

        WindowsEventLogFailureSource gatewayOpSource = new(
            "Microsoft-Windows-TerminalServices-Gateway/Operational",
            TsGatewayQuery,
            record => RdGatewayEventParser.Parse(record, trustedProxies));

        WindowsEventLogFailureSource gatewayAdminSource = new(
            "Microsoft-Windows-TerminalServices-Gateway/Admin",
            TsGatewayQuery,
            record => RdGatewayEventParser.Parse(record, trustedProxies));

        WindowsEventLogFailureSource npsSource = new(
            "Security",
            NpsQuery,
            record => RdGatewayEventParser.Parse(record, trustedProxies));

        return new CompositeAuthenticationEventSource(gatewayOpSource, gatewayAdminSource, npsSource);
    }
}
