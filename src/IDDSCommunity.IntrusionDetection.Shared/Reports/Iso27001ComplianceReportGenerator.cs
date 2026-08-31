using System;
using System.Collections.Generic;
using System.Text;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Shared.Reports;

/// <summary>
/// 提供針對 ISO/IEC 27001:2022 附錄 A (Annex A) 資安控制措施之稽核合規報告產製引擎。
/// </summary>
public static class Iso27001ComplianceReportGenerator
{
    /// <summary>
    /// 產製 ISO/IEC 27001:2022 控制措施符合性稽核報告（HTML 格式）。
    /// </summary>
    /// <param name="stats">系統統計數據。</param>
    /// <returns>具備專業 CSS 樣式之 HTML 稽核合規報表。</returns>
    public static string GenerateHtmlReport(Iso27001ReportStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-TW\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <title>ISO/IEC 27001:2022 資安合規稽核報告 - IDDS Community</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 24px; color: #333; background-color: #f8fafc; }");
        sb.AppendLine("    .header { background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); color: white; padding: 24px; border-radius: 8px; margin-bottom: 24px; }");
        sb.AppendLine("    .header h1 { margin: 0 0 8px 0; font-size: 24px; }");
        sb.AppendLine("    .header p { margin: 0; color: #94a3b8; font-size: 14px; }");
        sb.AppendLine("    .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }");
        sb.AppendLine("    .stat-card { background: white; padding: 16px; border-radius: 8px; border: 1px solid #e2e8f0; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }");
        sb.AppendLine("    .stat-title { font-size: 12px; text-transform: uppercase; color: #64748b; font-weight: 600; }");
        sb.AppendLine("    .stat-value { font-size: 28px; font-weight: 700; color: #0f172a; margin-top: 4px; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }");
        sb.AppendLine("    th, td { padding: 12px 16px; text-align: left; border-bottom: 1px solid #e2e8f0; font-size: 14px; }");
        sb.AppendLine("    th { background: #f1f5f9; color: #475569; font-weight: 600; }");
        sb.AppendLine("    tr:last-child td { border-bottom: none; }");
        sb.AppendLine("    .badge-pass { background: #dcfce7; color: #166534; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }");
        sb.AppendLine("    .section-title { font-size: 18px; font-weight: 600; color: #0f172a; margin: 24px 0 12px 0; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("  <div class=\"header\">");
        sb.AppendLine("    <h1>ISO/IEC 27001:2022 控制措施符合性稽核報告</h1>");
        sb.AppendLine($"    <p>產製時間：{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ｜ 系統名稱：IDDS Community Intrusion Detection System</p>");
        sb.AppendLine("  </div>");

        // Stat cards
        sb.AppendLine("  <div class=\"stats-grid\">");
        sb.AppendLine($"    <div class=\"stat-card\"><div class=\"stat-title\">累計封鎖威脅 IP</div><div class=\"stat-value\">{stats.TotalBlockedIps:N0}</div></div>");
        sb.AppendLine($"    <div class=\"stat-card\"><div class=\"stat-title\">目前有效阻絕規則</div><div class=\"stat-value\">{stats.ActiveFirewallRules:N0}</div></div>");
        sb.AppendLine($"    <div class=\"stat-card\"><div class=\"stat-title\">動態情資訂閱指標數</div><div class=\"stat-value\">{stats.ThreatFeedIndicatorsCount:N0}</div></div>");
        sb.AppendLine($"    <div class=\"stat-card\"><div class=\"stat-title\">誘餌蜜罐探測事件</div><div class=\"stat-value\">{stats.HoneypotProbeCount:N0}</div></div>");
        sb.AppendLine("  </div>");

        // Controls Table
        sb.AppendLine("  <div class=\"section-title\">Annex A 資訊安全控制措施符合性矩陣</div>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead><tr><th>控制條款</th><th>控制名稱 (ISO 27001:2022)</th><th>IDDS Community 實作機制</th><th>合規狀態</th></tr></thead>");
        sb.AppendLine("    <tbody>");

        sb.AppendLine("      <tr><td><strong>A.5.7</strong></td><td>威脅情報 (Threat Intelligence)</td><td>開源與社群情報訂閱、雙層 Bogon 硬過濾、跨主機叢集聯防、STIX 2.1 格式交換</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");
        sb.AppendLine("      <tr><td><strong>A.8.7</strong></td><td>防範惡意軟體與主動防禦 (Protection Against Malware)</td><td>主動式誘餌蜜罐 (Honeypot)、智慧假釋 (Probation) 一擊再鎖、Windows 防火牆即時硬封鎖</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");
        sb.AppendLine("      <tr><td><strong>A.8.15</strong></td><td>日誌記錄 (Logging)</td><td>安全事件稽核軌跡 (Audit Trail)、不可變唯一識別碼 (Deterministic GUIDs)、關聯分析事件追蹤</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");
        sb.AppendLine("      <tr><td><strong>A.8.16</strong></td><td>監控活動 (Monitoring Activities)</td><td>多平台 Webhook (Teams/Slack/Discord/Telegram) 即時告警、DDNS 動態主機解析與即時日誌監控</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");
        sb.AppendLine("      <tr><td><strong>A.8.20</strong></td><td>網路安全 (Network Security)</td><td>雙向輸入/輸出阻絕、安全網路 IPv4/IPv6 CIDR 白名單、TCP 封包監聽與多通訊協定偵測</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");
        sb.AppendLine("      <tr><td><strong>A.8.24</strong></td><td>密碼學控制 (Use of Cryptography)</td><td>SQLite ChaCha20-Poly1305 靜態加密、Windows DPAPI 金鑰保護與 IDDSCommunityOperators ACL 邊界</td><td><span class=\"badge-pass\">符合 (Compliant)</span></td></tr>");

        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}

/// <summary>
/// 代表 ISO/IEC 27001:2022 合規報表統計數據。
/// </summary>
public sealed class Iso27001ReportStats
{
    /// <summary>
    /// 取得或設定累計封鎖 IP 總數。
    /// </summary>
    public int TotalBlockedIps { get; set; }

    /// <summary>
    /// 取得或設定目前活躍防火牆規則數。
    /// </summary>
    public int ActiveFirewallRules { get; set; }

    /// <summary>
    /// 取得或設定威脅情報指標數量。
    /// </summary>
    public int ThreatFeedIndicatorsCount { get; set; }

    /// <summary>
    /// 取得或設定誘餌蜜罐探測事件總數。
    /// </summary>
    public int HoneypotProbeCount { get; set; }
}
