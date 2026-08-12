using System.Diagnostics.Metrics;

namespace IDDSCommunity.IntrusionDetection.Shared;

internal static class IDDSCommunityMetrics
{
    internal const string MeterName = "IDDSCommunity.IntrusionDetection";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PacketsReceived = Meter.CreateCounter<long>("iddscommunity.packets.received");
    private static readonly Counter<long> PacketsDispatched = Meter.CreateCounter<long>("iddscommunity.packets.dispatched");
    private static readonly Counter<long> PacketsDropped = Meter.CreateCounter<long>("iddscommunity.packets.dropped");
    private static readonly Counter<long> PacketsMalformed = Meter.CreateCounter<long>("iddscommunity.packets.malformed");
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
    /// <summary>
    /// 記錄一個未通過 IPv4 或 TCP 格式驗證的封包。
    /// </summary>
    internal static void RecordMalformed() => PacketsMalformed.Add(1);
}
