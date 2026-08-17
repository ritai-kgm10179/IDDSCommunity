using System;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.TechnitiumDns;

/// <summary>
/// 監看 Technitium DNS Server 查詢/拒絕記錄檔，偵測遭封鎖或濫用的 DNS 查詢事件。
/// </summary>
[SupportedOSPlatform("windows7.0")]
[Plugin("Technitium DNS Security Agent", "Detects blocked or abusive Technitium DNS queries.", "1.0")]
public sealed class TechnitiumDnsSecurityAgent : AuthenticationAgentBase<TechnitiumDnsConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="TechnitiumDnsSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public TechnitiumDnsSecurityAgent() : this(new TechnitiumDnsConfiguration()) { }
    private TechnitiumDnsSecurityAgent(TechnitiumDnsConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="TechnitiumDnsSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal TechnitiumDnsSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Technitium DNS Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{99C71D22-830E-4E76-A83B-7D831C2442FE}");

    private static IAuthenticationEventSource CreateSource(TechnitiumDnsConfiguration configuration)
    {
        return new PollingLogFileFailureSource(
            configuration.EnumerateLogFiles,
            line => TechnitiumDnsLogParser.TryParseMessage(line, DateTimeOffset.UtcNow));
    }
}
