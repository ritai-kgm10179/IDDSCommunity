using System.Diagnostics.Metrics;

namespace Cyberarms.IntrusionDetection.Shared;

internal static class CyberarmsMetrics
{
    internal const string MeterName = "Cyberarms.IntrusionDetection";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PacketsReceived = Meter.CreateCounter<long>("cyberarms.packets.received");
    private static readonly Counter<long> PacketsDispatched = Meter.CreateCounter<long>("cyberarms.packets.dispatched");
    private static readonly Counter<long> PacketsDropped = Meter.CreateCounter<long>("cyberarms.packets.dropped");

    /// <summary>
    /// Records one packet presented to the bounded dispatch pipeline.
    /// </summary>
    internal static void RecordReceived() => PacketsReceived.Add(1);

    /// <summary>
    /// Records one packet delivered to a consumer.
    /// </summary>
    internal static void RecordDispatched() => PacketsDispatched.Add(1);

    /// <summary>
    /// Records one packet rejected because the bounded queue is full or closed.
    /// </summary>
    internal static void RecordDropped() => PacketsDropped.Add(1);
}
