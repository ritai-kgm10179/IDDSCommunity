using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace IDDSCommunity.Agents.MySql;

internal static partial class MySqlMariaDbAuthenticationParser
{
    internal static bool TryParse(string? providerName, IEnumerable<string?> messages, out IPAddress address)
    {
        address = null!;
        if (!IsSupportedProvider(providerName)) return false;

        foreach (string? message in messages)
        {
            if (string.IsNullOrWhiteSpace(message)) continue;
            Match match = AccessDeniedPattern().Match(message);
            if (!match.Success) continue;
            string host = match.Groups["host"].Value.Trim('[', ']');
            if (IPAddress.TryParse(host, out IPAddress? parsedAddress) && parsedAddress is not null)
            {
                address = parsedAddress;
                return true;
            }
        }
        return false;
    }

    private static bool IsSupportedProvider(string? providerName) =>
        string.Equals(providerName, "MySQL", StringComparison.OrdinalIgnoreCase)
        || string.Equals(providerName, "MariaDB", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("Access denied for user\\s+'[^']*'@'(?<host>[^']+)'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AccessDeniedPattern();
}
