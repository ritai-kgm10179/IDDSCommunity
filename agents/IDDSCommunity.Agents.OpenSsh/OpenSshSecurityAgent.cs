using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.OpenSsh;

/// <summary>
/// 監看 Windows OpenSSH 之事件記錄或文字記錄檔，偵測重複的驗證失敗事件。
/// </summary>
[Plugin("Windows OpenSSH Security Agent", "Detects repeated Windows OpenSSH authentication failures.", "1.0")]
public sealed partial class OpenSshSecurityAgent : AuthenticationAgentBase<OpenSshConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="OpenSshSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public OpenSshSecurityAgent() : this(new OpenSshConfiguration()) { }
    private OpenSshSecurityAgent(OpenSshConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="OpenSshSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal OpenSshSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Windows OpenSSH Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{FA68919B-6D0B-4508-9659-3CD1E160235C}");

    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        string description = record.FormatDescription() ?? string.Empty;
        return TryParseMessage(description, record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow, record.Id);
    }

    internal static AuthenticationFailureEvent? TryParseMessage(string message, DateTimeOffset occurredAt, int eventId = 4)
    {
        Match match = FailedPassword().Match(message);
        if (!match.Success || !IPAddress.TryParse(match.Groups["ip"].Value.Trim('[', ']'), out IPAddress? address)) return null;
        return new AuthenticationFailureEvent(occurredAt, address, eventId, "OpenSSH", match.Groups["user"].Value, "Password authentication failed");
    }

    [GeneratedRegex(@"Failed password for (?:invalid user )?(?<user>\S+) from (?<ip>\[?[0-9A-Fa-f:.]+\]?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FailedPassword();

    private static IAuthenticationEventSource CreateSource(OpenSshConfiguration configuration)
    {
        IAuthenticationEventSource eventLog = configuration.ReadEventLog
            ? new WindowsEventLogFailureSource("OpenSSH/Operational", "*[System[(EventID=4)]]", Parse)
            : new CompositeAuthenticationEventSource();
        if (string.IsNullOrWhiteSpace(configuration.LogFilePath)) return eventLog;
        return new CompositeAuthenticationEventSource(eventLog, new PollingLogFileFailureSource(configuration.EnumerateLogFiles, line => TryParseMessage(line, DateTimeOffset.UtcNow)));
    }
}
