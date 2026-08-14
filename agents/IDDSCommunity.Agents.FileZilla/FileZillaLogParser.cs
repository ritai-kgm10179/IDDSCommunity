using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla;

[SupportedOSPlatform("windows7.0")]
public static partial class FileZillaLogParser
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?\s+(?:\[[^\]]*\]\s+)?(?<ip>[0-9a-fA-F:\.]+)\s+-\s+.*(?:authentication failed|login failed|password incorrect|530 login incorrect|530 authentication failed)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex GetFailureRegex();

    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        Match match;
        try
        {
            match = GetFailureRegex().Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 530, "FileZilla", string.Empty, "FileZilla authentication failed");
    }
}
