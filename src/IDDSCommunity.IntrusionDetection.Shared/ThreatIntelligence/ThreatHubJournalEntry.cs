using System;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 定義威脅情資中繼中心異動日誌事件型別常數。
/// </summary>
public static class ThreatHubJournalEventType
{
    /// <summary>
    /// 封鎖事件（新增或更新黑名單威脅）。
    /// </summary>
    public const int Blocked = 1;

    /// <summary>
    /// 假釋觀察事件。
    /// </summary>
    public const int Probation = 2;

    /// <summary>
    /// 撤銷事件（墓碑標記）。
    /// </summary>
    public const int Revoked = 3;
}

/// <summary>
/// 代表威脅情資異動日誌項目（含序號、事件型別、來源 IP、載體與建立時間戳記）。
/// </summary>
public sealed class ThreatHubJournalEntry
{
    /// <summary>
    /// 取得或設定 異動日誌遞增序號。
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// 取得或設定 事件型別（1=封鎖 Blocked, 2=假釋 Probation, 3=撤銷墓碑 Revoked）。
    /// </summary>
    public int EventType { get; set; }

    /// <summary>
    /// 取得或設定 威脅來源 IP 位址。
    /// </summary>
    public string SourceIp { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 情資資料載體或撤銷原因。
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 事件建立時間之 UTC 刻度數。
    /// </summary>
    public long CreatedTicks { get; set; }

    /// <summary>
    /// 初始化 <see cref="ThreatHubJournalEntry"/> 類別之新執行個體。
    /// </summary>
    public ThreatHubJournalEntry() { }

    /// <summary>
    /// 初始化 <see cref="ThreatHubJournalEntry"/> 類別之新執行個體並設定初始值。
    /// </summary>
    /// <param name="sequence">異動日誌遞增序號。</param>
    /// <param name="eventType">事件型別。</param>
    /// <param name="sourceIp">威脅來源 IP 位址。</param>
    /// <param name="payload">情資資料載體。</param>
    /// <param name="createdTicks">事件建立時間之 UTC 刻度數。</param>
    public ThreatHubJournalEntry(long sequence, int eventType, string sourceIp, string payload, long createdTicks)
    {
        Sequence = sequence;
        EventType = eventType;
        SourceIp = sourceIp;
        Payload = payload;
        CreatedTicks = createdTicks;
    }
}
