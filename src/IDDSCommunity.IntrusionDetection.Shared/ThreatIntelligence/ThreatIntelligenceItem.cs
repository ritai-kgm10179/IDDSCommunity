using System;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 代表跨主機威脅情資交換之標準資料合約模型。
/// </summary>
public sealed class ThreatIntelligenceItem
{
    /// <summary>
    /// 取得或設定 攻擊來源之標準化 IP 位址。
    /// </summary>
    public string SourceIp { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 威脅分類標籤（例如 RDP_BRUTE_FORCE、SSH_SPRAY、SQL_INJECTION）。
    /// </summary>
    public string ThreatCategory { get; set; } = "BRUTE_FORCE";

    /// <summary>
    /// 取得或設定 威脅置信度評分（介於 0.0 至 1.0 之間）。
    /// </summary>
    public double ConfidenceScore { get; set; } = 1.0;

    /// <summary>
    /// 取得或設定 回報本威脅之邊緣節點唯一識別碼。
    /// </summary>
    public string ReporterNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 回報本威脅之邊緣節點主機名稱。
    /// </summary>
    public string ReporterNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 威脅回報之 UTC 時間戳記。
    /// </summary>
    public DateTime ReportedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 取得或設定 建議封鎖到期時間（若為永久硬封鎖則為 DateTime.MaxValue）。
    /// </summary>
    public DateTime ExpiresUtc { get; set; } = DateTime.MaxValue;

    /// <summary>
    /// 取得或設定 附加備註或攻擊特徵描述（不含私密資料）。
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
