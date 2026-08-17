using System;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.IisAuthentication;

/// <summary>
/// 監看 IIS W3C 記錄檔，偵測重複的 401 驗證失敗事件，並可選擇性限制於受保護路徑（如 OWA）。
/// </summary>
[Plugin("IIS Authentication Security Agent", "Detects repeated IIS authentication failures, including optional OWA paths.", "1.0")]
public sealed class IisAuthenticationSecurityAgent : AuthenticationAgentBase<IisAuthenticationConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="IisAuthenticationSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public IisAuthenticationSecurityAgent() : this(new IisAuthenticationConfiguration()) { }
    private IisAuthenticationSecurityAgent(IisAuthenticationConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;

    private static PollingLogFileFailureSource CreateSource(IisAuthenticationConfiguration configuration)
    {
        IisW3cAuthenticationParser parser = new(configuration.GetProtectedPaths());
        return new PollingLogFileFailureSource(configuration.EnumerateLogFiles, parser.Parse, parser.Reset);
    }
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="IisAuthenticationSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal IisAuthenticationSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("IIS Authentication Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{6B87C539-1585-41E5-A12F-D5073EF6D631}");
}
