using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供安全性代理程式事件篩選與門檻判斷之基礎實作。
/// </summary>
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
        /// <summary>
    /// 取得或設定 Id。
    /// </summary>
public Guid Id { get; set; }
        /// <summary>
    /// 取得或設定 本地化顯示名稱。
    /// </summary>
public string DisplayName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString() => DisplayName;
}
