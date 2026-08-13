using System;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.OpenSsh;

[Plugin("Windows OpenSSH Security Agent", "Detects repeated Windows OpenSSH authentication failures.", "1.0")]
public sealed partial class OpenSshSecurityAgent : AuthenticationAgentBase<OpenSshConfiguration>
{
    public OpenSshSecurityAgent() : this(new OpenSshConfiguration()) { }
    private OpenSshSecurityAgent(OpenSshConfiguration configuration) : base(CreateSource(configuration)) => Configuration.AgentSettings = configuration;
    internal OpenSshSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Windows OpenSSH Security Agent"); set { } }
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
