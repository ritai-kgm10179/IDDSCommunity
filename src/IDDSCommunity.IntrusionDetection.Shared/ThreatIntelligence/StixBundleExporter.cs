using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供將 IDDS Community 入侵偵測資料匯出為符合 OASIS STIX 2.1 (Structured Threat Information Expression) 國際標準之威脅情報交換 Bundle 工具。
/// </summary>
public static class StixBundleExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 將封鎖與入侵記錄集合匯出為 STIX 2.1 JSON Bundle 字串。
    /// </summary>
    /// <param name="entries">入侵記錄項目。</param>
    /// <param name="bundleId">可選之 Bundle GUID。</param>
    /// <returns>STIX 2.1 JSON 格式字串。</returns>
    public static string ExportBundle(IEnumerable<StixExportItem> entries, Guid? bundleId = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        Guid actualBundleGuid = bundleId ?? Guid.NewGuid();
        string bundleIdString = $"bundle--{actualBundleGuid}";
        List<object> stixObjects = [];
        List<string> objectRefs = [];
        DateTime nowUtc = DateTime.UtcNow;

        // 1. 建立 Identity 物件 (IDDS Community Producer)
        string identityId = $"identity--{WellKnownAgentIds.ClusterThreatHub}";
        var identityObject = new Dictionary<string, object?>
        {
            ["type"] = "identity",
            ["spec_version"] = "2.1",
            ["id"] = identityId,
            ["created"] = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["modified"] = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["name"] = "IDDS Community Intrusion Detection System",
            ["identity_class"] = "system",
            ["description"] = "Automated Host-Based & Edge Intrusion Detection Platform"
        };
        stixObjects.Add(identityObject);
        objectRefs.Add(identityId);

        // 2. 為每個 IP 建立 Indicator 與 ObservedData
        foreach (var item in entries)
        {
            if (string.IsNullOrWhiteSpace(item.IpAddress))
                continue;

            string ip = item.IpAddress.Trim();
            bool isIPv6 = ip.Contains(':');
            string patternType = isIPv6 ? "ipv6-addr" : "ipv4-addr";
            string pattern = $"[{patternType}:value = '{ip}']";

            string indicatorId = $"indicator--{Guid.NewGuid()}";
            var indicatorObject = new Dictionary<string, object?>
            {
                ["type"] = "indicator",
                ["spec_version"] = "2.1",
                ["id"] = indicatorId,
                ["created"] = item.EventTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["modified"] = item.EventTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["name"] = $"Malicious Network Traffic from {ip}",
                ["description"] = item.Description ?? "Unauthorized authentication attempt or port scanning probe.",
                ["indicator_types"] = new[] { "malicious-activity", "anomalous-activity" },
                ["pattern"] = pattern,
                ["pattern_type"] = "stix",
                ["pattern_version"] = "2.1",
                ["valid_from"] = item.EventTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["confidence"] = item.ConfidenceScore > 0 ? item.ConfidenceScore : 85,
                ["created_by_ref"] = identityId
            };
            stixObjects.Add(indicatorObject);
            objectRefs.Add(indicatorId);
        }

        // 3. 建立 STIX 2.1 Report 物件
        string reportId = $"report--{Guid.NewGuid()}";
        var reportObject = new Dictionary<string, object?>
        {
            ["type"] = "report",
            ["spec_version"] = "2.1",
            ["id"] = reportId,
            ["created"] = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["modified"] = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["name"] = "IDDS Community Threat Intelligence Bundle",
            ["description"] = "Aggregated security indicators exported according to OASIS STIX 2.1 specification.",
            ["report_types"] = new[] { "threat-actor", "indicator" },
            ["published"] = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["object_refs"] = objectRefs,
            ["created_by_ref"] = identityId
        };
        stixObjects.Add(reportObject);

        // 4. 組裝 STIX 2.1 Bundle
        var bundle = new Dictionary<string, object?>
        {
            ["type"] = "bundle",
            ["id"] = bundleIdString,
            ["objects"] = stixObjects
        };

        return JsonSerializer.Serialize(bundle, JsonOptions);
    }
}

/// <summary>
/// 代表供 STIX 2.1 匯出之單一威脅資料項目。
/// </summary>
public sealed class StixExportItem
{
    /// <summary>
    /// 取得或設定威脅來源 IP 位址。
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定事件發生時間 (UTC)。
    /// </summary>
    public DateTime EventTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 取得或設定入侵描述資訊。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 取得或設定信心度分數 (0~100)。
    /// </summary>
    public int ConfidenceScore { get; set; } = 85;

    /// <summary>
    /// 取得或設定報告此事件之安全性代理程式名稱。
    /// </summary>
    public string? AgentName { get; set; }
}
