using System;

namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// 代表供營運審查使用的一個持久化保護控制動作或失敗紀錄。
/// </summary>
public sealed class ProtectionAuditEvent
{
    /// <summary>
    /// 取得 database sequence number.
    /// </summary>
    public long Id { get; init; }
    /// <summary>
    /// 取得 UTC occurrence time.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; }
    /// <summary>
    /// 取得 stable machine-readable event type.
    /// </summary>
    public string EventType { get; init; } = string.Empty;
    /// <summary>
    /// 取得 stable outcome code, such as Succeeded or Failed.
    /// </summary>
    public string Outcome { get; init; } = string.Empty;
    /// <summary>
    /// 取得 service or user identity that initiated the action.
    /// </summary>
    public string Actor { get; init; } = string.Empty;
    /// <summary>
    /// 取得 protected resource or address affected by the action.
    /// </summary>
    public string Subject { get; init; } = string.Empty;
    /// <summary>
    /// 取得非機密的診斷詳細資訊。
    /// </summary>
    public string Details { get; init; } = string.Empty;
}
