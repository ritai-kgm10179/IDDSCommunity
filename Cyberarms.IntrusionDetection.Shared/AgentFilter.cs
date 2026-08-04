using System;

namespace Cyberarms.IntrusionDetection.Shared;

public class AgentFilter : IAgentFilter
{
    public AgentFilter()
    {
    }
    public AgentFilter(Guid id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
}
