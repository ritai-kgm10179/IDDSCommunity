using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.TechnitiumDns;

[SupportedOSPlatform("windows7.0")]
public static class TechnitiumDnsLogParser
{
    private static readonly Regex ThreatRegex = new(
        @"(?:Client\s+|ip=)(?<ip>[0-9a-fA-F:\.]+?)(?::\d+)?\s+.*(?:Refused|Blocked|exceeded QPM limit|Rate limit|Dropped|Denied).*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        Match match = ThreatRegex.Match(line);
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 53, "TechnitiumDNS", string.Empty, "Technitium DNS query refused or blocked");
    }
}
