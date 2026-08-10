using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Collections.Concurrent;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.IisAuthentication;

internal sealed class IisW3cAuthenticationParser
{
    private readonly ConcurrentDictionary<string, string[]> fieldsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly string[] protectedPaths;

    internal IisW3cAuthenticationParser(params string[] protectedPaths) => this.protectedPaths = protectedPaths;

    internal AuthenticationFailureEvent? Parse(string path, string line)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (line.StartsWith("#Fields:", StringComparison.OrdinalIgnoreCase))
        {
            fieldsByPath[path] = line[8..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return null;
        }
        string[] fields = fieldsByPath.GetValueOrDefault(path, []);
        if (line.Length == 0 || line[0] == '#' || fields.Length == 0) return null;
        string[] values = line.Split(' ');
        if (values.Length < fields.Length) return null;
        Dictionary<string, string> row = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < fields.Length; index++) row[fields[index]] = values[index];
        if (!row.TryGetValue("sc-status", out string? status) || status != "401") return null;
        if (!row.TryGetValue("sc-substatus", out string? substatus) || substatus != "1") return null;
        if (!row.TryGetValue("sc-win32-status", out string? win32Status) || win32Status != "1326") return null;
        if (!row.TryGetValue("c-ip", out string? source) || !IPAddress.TryParse(source, out IPAddress? address)) return null;
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        if (row.TryGetValue("date", out string? date) && row.TryGetValue("time", out string? time) && DateTime.TryParseExact($"{date} {time}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed)) occurredAt = new DateTimeOffset(parsed, TimeSpan.Zero);
        row.TryGetValue("cs-username", out string? account);
        row.TryGetValue("cs-uri-stem", out string? requestPath);
        if (protectedPaths.Length > 0 && !protectedPaths.Any(prefix => (requestPath ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return null;
        return new AuthenticationFailureEvent(occurredAt, address, 401, "IIS", account ?? string.Empty, requestPath ?? string.Empty);
    }

    internal AuthenticationFailureEvent? Parse(string line) => Parse(string.Empty, line);

    internal void Reset(string path) => fieldsByPath.TryRemove(path, out _);
}
