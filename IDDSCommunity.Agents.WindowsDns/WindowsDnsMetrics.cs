using System.Diagnostics.Metrics;

namespace IDDSCommunity.Agents.WindowsDns;

internal static class WindowsDnsMetrics
{
    internal const string MeterName = "IDDSCommunity.WindowsDns";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> EventsObserved = Meter.CreateCounter<long>("iddscommunity.dns.events.observed");
    private static readonly Counter<long> ThreatsDetected = Meter.CreateCounter<long>("iddscommunity.dns.threats.detected");
    private static readonly Counter<long> ParseFailures = Meter.CreateCounter<long>("iddscommunity.dns.events.parse_failures");

    /// <summary>
    /// 紀錄此 Agent 接受的一個標準化 DNS 事件。
    /// </summary>
    internal static void RecordObserved() => EventsObserved.Add(1);

    /// <summary>
    /// 紀錄一個已引發至 IDDSCommunity 保護管線的 DNS 門檻值超越事件。
    /// </summary>
    internal static void RecordDetected() => ThreatsDetected.Add(1);

    /// <summary>
    /// 紀錄一個無法進行負載標準化的已知 Windows DNS 事件。
    /// </summary>
    internal static void RecordParseFailure() => ParseFailures.Add(1);
}
