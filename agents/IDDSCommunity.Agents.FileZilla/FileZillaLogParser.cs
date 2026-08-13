using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla;

[SupportedOSPlatform("windows7.0")]
public static class FileZillaLogParser
{
    private static readonly Regex FailureRegex = new(
        @"^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?\s+(?:\[[^\]]*\]\s+)?(?<ip>[0-9a-fA-F:\.]+)\s+-\s+.*(?:authentication failed|login failed|password incorrect|530 login incorrect|530 authentication failed).*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        Match match = FailureRegex.Match(line);
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 530, "FileZilla", string.Empty, "FileZilla authentication failed");
    }
}
