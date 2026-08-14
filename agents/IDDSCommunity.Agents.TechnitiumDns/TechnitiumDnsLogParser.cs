using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.TechnitiumDns;

[SupportedOSPlatform("windows7.0")]
public static partial class TechnitiumDnsLogParser
{
    [GeneratedRegex(@"(?:Client\s+|ip=)(?<ip>[0-9a-fA-F:\.]+?)(?::\d+)?\s+.*(?:Refused|Blocked|exceeded QPM limit|Rate limit|Dropped|Denied)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex GetThreatRegex();

    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        Match match;
        try
        {
            match = GetThreatRegex().Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 53, "TechnitiumDNS", string.Empty, "Technitium DNS query refused or blocked");
    }
}
