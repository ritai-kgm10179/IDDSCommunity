using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

public interface IAgentFilter
{
    Guid Id { get; set; }
    string DisplayName { get; set; }
}
