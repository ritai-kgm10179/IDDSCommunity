using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.IisAuthentication;

internal sealed class IisW3cAuthenticationParser
{
    private sealed record FieldSchema(
        int FieldCount,
        int StatusIndex,
        int SubstatusIndex,
        int Win32StatusIndex,
        int ClientIpIndex,
        int DateIndex,
        int TimeIndex,
        int UsernameIndex,
        int UriStemIndex);

    private readonly ConcurrentDictionary<string, FieldSchema> fieldsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly string[] protectedPaths;

    internal IisW3cAuthenticationParser(params string[] protectedPaths) => this.protectedPaths = protectedPaths;

    internal AuthenticationFailureEvent? Parse(string path, string line)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (line.Length == 0) return null;
        if (line.StartsWith("#Fields:", StringComparison.OrdinalIgnoreCase))
        {
            string[] fields = line[8..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            fieldsByPath[path] = new FieldSchema(
                fields.Length,
                FindIndex(fields, "sc-status"),
                FindIndex(fields, "sc-substatus"),
                FindIndex(fields, "sc-win32-status"),
                FindIndex(fields, "c-ip"),
                FindIndex(fields, "date"),
                FindIndex(fields, "time"),
                FindIndex(fields, "cs-username"),
                FindIndex(fields, "cs-uri-stem"));
            return null;
        }
        if (line[0] == '#' || !fieldsByPath.TryGetValue(path, out FieldSchema? schema) || schema.FieldCount == 0) return null;
        if (schema.StatusIndex < 0 || schema.SubstatusIndex < 0 || schema.Win32StatusIndex < 0 || schema.ClientIpIndex < 0) return null;
        if (!line.Contains("401", StringComparison.Ordinal)) return null;

        Span<Range> ranges = stackalloc Range[Math.Max(schema.FieldCount + 1, 32)];
        ReadOnlySpan<char> span = line.AsSpan();
        int count = span.Split(ranges, ' ');
        if (count < schema.FieldCount) return null;

        if (!span[ranges[schema.StatusIndex]].Equals("401", StringComparison.Ordinal)) return null;
        if (!span[ranges[schema.SubstatusIndex]].Equals("1", StringComparison.Ordinal)) return null;
        if (!span[ranges[schema.Win32StatusIndex]].Equals("1326", StringComparison.Ordinal)) return null;
        if (!IPAddress.TryParse(span[ranges[schema.ClientIpIndex]], out IPAddress? address)) return null;

        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        if (schema.DateIndex >= 0 && schema.TimeIndex >= 0)
        {
            ReadOnlySpan<char> dateSpan = span[ranges[schema.DateIndex]];
            ReadOnlySpan<char> timeSpan = span[ranges[schema.TimeIndex]];
            if (DateOnly.TryParseExact(dateSpan, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d) &&
                TimeOnly.TryParseExact(timeSpan, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly t))
            {
                occurredAt = new DateTimeOffset(d.ToDateTime(t, DateTimeKind.Utc), TimeSpan.Zero);
            }
        }

        string account = schema.UsernameIndex >= 0 ? span[ranges[schema.UsernameIndex]].ToString() : string.Empty;
        string requestPath = schema.UriStemIndex >= 0 ? span[ranges[schema.UriStemIndex]].ToString() : string.Empty;
        if (protectedPaths.Length > 0)
        {
            bool matched = false;
            foreach (string prefix in protectedPaths)
            {
                if (requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched) return null;
        }
        return new AuthenticationFailureEvent(occurredAt, address, 401, "IIS", account, requestPath);
    }

    private static int FindIndex(string[] fields, string name)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (string.Equals(fields[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    internal AuthenticationFailureEvent? Parse(string line) => Parse(string.Empty, line);

    internal void Reset(string path) => fieldsByPath.TryRemove(path, out _);
}
