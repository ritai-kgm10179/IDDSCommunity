using System;

namespace Cyberarms.IntrusionDetection.Shared
{
    public interface IAgentFilter
    {
        Guid Id { get; set; }
        string DisplayName { get; set; }
    }
}
