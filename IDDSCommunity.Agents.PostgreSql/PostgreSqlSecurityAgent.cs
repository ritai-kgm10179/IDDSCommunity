using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.PostgreSql;

[Plugin("PostgreSQL Security Agent", "Detects repeated PostgreSQL password authentication failures from server logs.", "1.0")]
public sealed partial class PostgreSqlSecurityAgent : AuthenticationAgentBase<PostgreSqlConfiguration>
{
    public PostgreSqlSecurityAgent() : this(new PostgreSqlConfiguration()) { }
    private PostgreSqlSecurityAgent(PostgreSqlConfiguration configuration) : base(new PollingLogFileFailureSource(configuration.EnumerateLogFiles, TryParseLine)) => Configuration.AgentSettings = configuration;
    internal PostgreSqlSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("PostgreSQL Security Agent"); set { } }
    public override Guid Id => new("{E4D503EE-33D9-4A79-A2E3-B19597D49D58}");

    internal static AuthenticationFailureEvent? TryParseLine(string line)
    {
        AuthenticationFailureEvent? structured = TryParseJson(line);
        if (structured is not null) return structured;
        if (!line.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase)) return null;
        Match ip = IpAddressPattern().Match(line);
        Match user = UserPattern().Match(line);
        if (!ip.Success || !IPAddress.TryParse(ip.Groups["ip"].Value.Trim('[', ']'), out IPAddress? address)) return null;
        return new AuthenticationFailureEvent(DateTimeOffset.UtcNow, address, 0, "PostgreSQL", user.Success ? user.Groups["user"].Value : string.Empty, "Password authentication failed");
    }

    private static AuthenticationFailureEvent? TryParseJson(string line)
    {
        if (!line.AsSpan().TrimStart().StartsWith("{")) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;
            string message = GetString(root, "message");
            if (!message.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase)) return null;
            string source = GetString(root, "remote_host");
            if (!IPAddress.TryParse(source.Trim('[', ']'), out IPAddress? address)) return null;
            string account = GetString(root, "user");
            DateTimeOffset occurredAt = DateTimeOffset.TryParse(GetString(root, "timestamp"), out DateTimeOffset parsed) ? parsed : DateTimeOffset.UtcNow;
            return new AuthenticationFailureEvent(occurredAt, address, 0, "PostgreSQL", account, "Password authentication failed");
        }
        catch (JsonException) { return null; }
    }

    private static string GetString(JsonElement root, string propertyName) => root.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;

    [GeneratedRegex(@"(?:host=|client=|remote=|\[)(?<ip>\[?[0-9A-Fa-f:.]+\]?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex IpAddressPattern();
    [GeneratedRegex("password authentication failed for user [\"'](?<user>[^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex UserPattern();
}
