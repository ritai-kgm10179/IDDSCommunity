using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 定義外部威脅情資來源之格式類型。
/// </summary>
public enum ThreatFeedFormat
{
    /// <summary>
    /// 純文字以換行分隔之 IP 或 CIDR 清單（支援以 # 字符開頭之註解行）。
    /// </summary>
    PlainTextLines = 0,

    /// <summary>
    /// IPsum 分級清單格式（每行包含 IP 位址與惡意等級整數，以 Tab 或空白分隔）。
    /// </summary>
    IPsumTabDelimited = 1,

    /// <summary>
    /// AbuseIPDB Blacklist JSON 格式。
    /// </summary>
    AbuseIpDbJson = 2
}

/// <summary>
/// 代表單一外部威脅情資訂閱來源之組態資訊。
/// </summary>
/// <param name="Name">情資來源名稱。</param>
/// <param name="Url">訂閱下載 URL。</param>
/// <param name="Format">情資格式類型。</param>
/// <param name="MinConfidenceOrLevel">最低採納門檻（IPsum 代表最小重複來源數，AbuseIPDB 代表最小信心度百分比）。</param>
/// <param name="Enabled">是否啟用此來源。</param>
/// <param name="Attribution">授權與來源組織宣告。</param>
public sealed record ThreatFeedSource(
    string Name,
    string Url,
    ThreatFeedFormat Format,
    int MinConfidenceOrLevel = 3,
    bool Enabled = true,
    string Attribution = "");

/// <summary>
/// 提供內建知名且高信心度之預設威脅情報訂閱清單。
/// </summary>
public static class WellKnownThreatFeeds
{
    /// <summary>
    /// IPsum Level 3（同時被 3 個以上獨立國際威脅庫標記為惡意之 IP 清單，MIT 授權）。
    /// </summary>
    public static readonly ThreatFeedSource IPsumLevel3 = new(
        "IPsum (Level 3+ Aggregated)",
        "https://raw.githubusercontent.com/stamparm/ipsum/master/levels/3.txt",
        ThreatFeedFormat.IPsumTabDelimited,
        MinConfidenceOrLevel: 3,
        Enabled: true,
        Attribution: "IPsum by Stamparm (MIT License)");

    /// <summary>
    /// Spamhaus DROP（Don't Route Or Peer 劫持與嚴重濫用子網路清單）。
    /// </summary>
    public static readonly ThreatFeedSource SpamhausDrop = new(
        "Spamhaus DROP",
        "https://www.spamhaus.org/drop/drop.txt",
        ThreatFeedFormat.PlainTextLines,
        MinConfidenceOrLevel: 1,
        Enabled: false,
        Attribution: "The Spamhaus Project (DROP Protection Advisory)");

    /// <summary>
    /// CINS Army BadGuys（Sentinel 網路探測器每日捕獲最具威脅之 IP 清單）。
    /// </summary>
    public static readonly ThreatFeedSource CinsArmy = new(
        "CINS Army BadGuys",
        "https://cinsscore.com/list/ci-badguys.txt",
        ThreatFeedFormat.PlainTextLines,
        MinConfidenceOrLevel: 1,
        Enabled: false,
        Attribution: "CINS Army Score & Sentinel Threat Network");

    /// <summary>
    /// Blocklist.de（Fail2Ban 社群回報之即時攻擊者 IP 清單）。
    /// </summary>
    public static readonly ThreatFeedSource BlocklistDe = new(
        "Blocklist.de All Attackers",
        "https://lists.blocklist.de/lists/all.txt",
        ThreatFeedFormat.PlainTextLines,
        MinConfidenceOrLevel: 1,
        Enabled: false,
        Attribution: "Blocklist.de Fail2Ban Community Reporting");

    /// <summary>
    /// 取得所有預設支援之威脅情資來源清單。
    /// </summary>
    public static IReadOnlyList<ThreatFeedSource> DefaultFeeds =>
    [
        IPsumLevel3,
        SpamhausDrop,
        CinsArmy,
        BlocklistDe
    ];
}
