using System.Diagnostics.Metrics;

namespace Cyberarms.Agents.WindowsDns;

internal static class WindowsDnsMetrics
{
    internal const string MeterName = "Cyberarms.WindowsDns";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> EventsObserved = Meter.CreateCounter<long>("cyberarms.dns.events.observed");
    private static readonly Counter<long> ThreatsDetected = Meter.CreateCounter<long>("cyberarms.dns.threats.detected");
    private static readonly Counter<long> ParseFailures = Meter.CreateCounter<long>("cyberarms.dns.events.parse_failures");

    /// <summary>
    /// Records one normalized DNS event accepted by the Agent.
    /// </summary>
    internal static void RecordObserved() => EventsObserved.Add(1);

    /// <summary>
    /// Records one DNS threshold crossing emitted to the Cyberarms protection pipeline.
    /// </summary>
    internal static void RecordDetected() => ThreatsDetected.Add(1);

    /// <summary>
    /// Records one supported Windows DNS event whose payload could not be normalized.
    /// </summary>
    internal static void RecordParseFailure() => ParseFailures.Add(1);
}
