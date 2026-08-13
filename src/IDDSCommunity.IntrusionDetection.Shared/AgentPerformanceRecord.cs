using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class AgentPerformanceRecord
{
    public DateTime DateTime { get; set; }
    public long MemoryValue { get; set; }
    public TimeSpan CpuUsage { get; set; }
    public long Packets { get; set; }
}
