using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義安全性代理程式事件篩選器必須實作之核心介面。
/// </summary>
public interface IAgentFilter
{
    /// <summary>
    /// 取得或設定篩選器之唯一識別碼。
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// 取得或設定篩選器之顯示名稱。
    /// </summary>
    string DisplayName { get; set; }
}
