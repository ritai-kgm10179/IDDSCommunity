using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class AgentFilter : IAgentFilter
{
    /// <summary>
    /// 初始化 <see cref="AgentFilter"/> class的新執行個體。
    /// </summary>

    public AgentFilter()
    {
    }
    /// <summary>
    /// 初始化 <see cref="AgentFilter"/> class的新執行個體。
    /// </summary>
    /// <param name="id">id參數。</param>
    /// <param name="displayName">display name參數。</param>

    public AgentFilter(Guid id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
