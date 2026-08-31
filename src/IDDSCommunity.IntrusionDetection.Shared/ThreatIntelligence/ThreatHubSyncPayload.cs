using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 邊緣節點向威脅情資中繼中心（Threat Hub）發送之同步請求載體。
/// </summary>
public sealed class ThreatHubSyncPayload
{
    /// <summary>
    /// 取得或設定 邊緣節點之唯一識別碼。
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 邊緣節點之主機名稱。
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 邊緣節點目前本機 IP 位址。
    /// </summary>
    public string NodeIp { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 前次成功同步之時間戳記。
    /// </summary>
    public DateTime LastSyncUtc { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 取得或設定 本次由邊緣節點主動回報之新威脅清單。
    /// </summary>
    public List<ThreatIntelligenceItem> NewThreats { get; set; } = [];
}

/// <summary>
/// 威脅情資中繼中心（Threat Hub）向邊緣節點回傳之同步回應載體。
/// </summary>
public sealed class ThreatHubSyncResponse
{
    /// <summary>
    /// 取得或設定 同步作業是否成功。
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// 取得或設定 錯誤訊息（若有）。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 取得或設定 Hub 伺服器之目前 UTC 時間。
    /// </summary>
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 取得或設定 叢集目前所有生效中之聯防威脅 IP 清單。
    /// </summary>
    public List<ThreatIntelligenceItem> ActiveThreats { get; set; } = [];
}
