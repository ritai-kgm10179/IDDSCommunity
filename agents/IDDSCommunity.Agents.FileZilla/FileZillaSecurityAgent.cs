using System;
using System.IO;
using System.Runtime.Versioning;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.FileZilla;

/// <summary>
/// 監看 FileZilla Server 驗證記錄檔，偵測重複的登入驗證失敗事件。
/// </summary>
[SupportedOSPlatform("windows7.0")]
[Plugin("FileZilla Security Agent", "Detects repeated FileZilla Server authentication failures.", "1.0")]
public sealed class FileZillaSecurityAgent : AuthenticationAgentBase<FileZillaConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="FileZillaSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public FileZillaSecurityAgent() : this(new FileZillaConfiguration()) { }
    private FileZillaSecurityAgent(FileZillaConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="FileZillaSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal FileZillaSecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("FileZilla Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{88B67B54-9E7D-4F7B-8A5F-4E90B0F33A11}");

    private static IAuthenticationEventSource CreateSource(FileZillaConfiguration configuration)
    {
        return new PollingLogFileFailureSource(
            configuration.EnumerateLogFiles,
            line => FileZillaLogParser.TryParseMessage(line, DateTimeOffset.UtcNow));
    }
}
