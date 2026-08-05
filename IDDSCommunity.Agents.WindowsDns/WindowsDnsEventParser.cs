using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IDDSCommunity.Agents.WindowsDns;

internal static class WindowsDnsEventParser
{
    /// <summary>
    /// Maps documented Windows DNS event payload positions to a stable internal event.
    /// </summary>
    /// <param name="eventId">The Windows DNS event identifier.</param>
    /// <param name="values">The ordered event payload values.</param>
    /// <param name="occurredAt">The event occurrence time.</param>
    /// <param name="record">Receives the parsed event when successful.</param>
    /// <returns><see langword="true"/> when the event is supported and contains a valid source address.</returns>
    internal static bool TryParse(int eventId, IReadOnlyList<object?> values, DateTimeOffset occurredAt, out DnsEventRecord? record)
    {
        record = null;
        return eventId switch
        {
            257 => TryCreate(eventId, values, occurredAt, 2, DnsActivityKind.Query, 5, 6, 9, out record),
            258 => TryCreate(eventId, values, occurredAt, 3, DnsActivityKind.Query, 4, 5, 7, out record),
            263 => TryCreate(eventId, values, occurredAt, 2, DnsActivityKind.DynamicUpdate, 3, -1, -1, out record),
            266 => TryCreate(eventId, values, occurredAt, 2, DnsActivityKind.ZoneTransfer, 3, -1, -1, out record),
            270 => TryCreate(eventId, values, occurredAt, 1, DnsActivityKind.ZoneTransfer, 3, -1, -1, out record),
            519 or 520 => TryCreate(eventId, values, occurredAt, 7, DnsActivityKind.DynamicUpdate, 1, 0, -1, out record),
            _ => false
        };
    }

    private static bool TryCreate(
        int eventId,
        IReadOnlyList<object?> values,
        DateTimeOffset occurredAt,
        int sourceIndex,
        DnsActivityKind kind,
        int queryNameIndex,
        int queryTypeIndex,
        int responseCodeIndex,
        out DnsEventRecord? record)
    {
        record = null;
        if (!TryGet(values, sourceIndex, out string source) || !IPAddress.TryParse(RemovePort(source), out IPAddress? address))
            return false;
        record = new DnsEventRecord(
            eventId,
            occurredAt,
            address,
            kind,
            Get(values, queryNameIndex),
            Get(values, queryTypeIndex),
            Get(values, responseCodeIndex));
        return true;
    }

    private static string RemovePort(string value)
    {
        string candidate = value.Trim().Trim('[', ']');
        if (IPAddress.TryParse(candidate, out _))
            return candidate;
        int separator = candidate.LastIndexOf(':');
        return separator > 0 && candidate.Count(character => character == ':') == 1 ? candidate[..separator] : candidate;
    }

    private static bool TryGet(IReadOnlyList<object?> values, int index, out string value)
    {
        value = Get(values, index);
        return value.Length > 0;
    }

    private static string Get(IReadOnlyList<object?> values, int index) =>
        index >= 0 && index < values.Count ? values[index]?.ToString() ?? string.Empty : string.Empty;
}
