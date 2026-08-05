using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class AgentFilter : IAgentFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFilter"/> class.
    /// </summary>

    public AgentFilter()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentFilter"/> class.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="displayName">The display name value.</param>

    public AgentFilter(Guid id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
