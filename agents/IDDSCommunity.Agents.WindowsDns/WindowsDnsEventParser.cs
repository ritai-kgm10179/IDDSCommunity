using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace IDDSCommunity.Agents.WindowsDns;

internal static class WindowsDnsEventParser
{
    /// <summary>
    /// 將記錄的 Windows DNS 事件負載位置對映至穩定的內部事件。
    /// </summary>
    /// <param name="eventId">Windows DNS 事件識別碼。</param>
    /// <param name="values">順序排列的事件負載數值。</param>
    /// <param name="occurredAt">事件發生時間。</param>
    /// <param name="record">解析成功時接收解析後的事件。</param>
    /// <returns>當事件受支援且包含有效的來源位址時傳回 <see langword="true"/>。</returns>
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
