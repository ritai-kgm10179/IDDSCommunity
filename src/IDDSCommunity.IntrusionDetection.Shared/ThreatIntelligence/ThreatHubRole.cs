namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 定義 IDDS Community 叢集威脅情資中繼架構中的主機節點角色。
/// </summary>
public enum ThreatHubRole
{
    /// <summary>
    /// 獨立單機模式（預設），不參與跨主機威脅情資同步。
    /// </summary>
    Standalone = 0,

    /// <summary>
    /// 邊緣節點模式，主動向集中式 Threat Hub 回報本機威脅並拉取全網聯防黑名單。
    /// </summary>
    EdgeNode = 1,

    /// <summary>
    /// 集中式威脅中繼中心模式，負責接收邊緣節點回報、彙整並分發全網威脅黑名單。
    /// </summary>
    ThreatHub = 2
}
