using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供純文字、IPsum 分級格式與 AbuseIPDB JSON 格式之外部威脅情報解析與 Bogon 安全過濾器。
/// </summary>
public static class ThreatFeedParser
{
    /// <summary>
    /// 單一來源最大允許解析與匯入之 IP 上限筆數（防止記憶體與規則容量耗盡）。
    /// </summary>
    public const int DefaultMaxEntriesPerFeed = 10000;

    /// <summary>
    /// 解析威脅情資內容字串並傳回通過 Bogon 與信心度篩選之標準化 IP 清單。
    /// </summary>
    /// <param name="content">外部情報原始文字內容。</param>
    /// <param name="format">情報格式類型。</param>
    /// <param name="minConfidenceOrLevel">最低信心度或分級門檻。</param>
    /// <param name="maxEntries">最大解析數量上限。</param>
    /// <returns>通過驗證之有效惡意 IP 清單。</returns>
    public static List<string> ParseFeed(
        string content,
        ThreatFeedFormat format,
        int minConfidenceOrLevel = 1,
        int maxEntries = DefaultMaxEntriesPerFeed)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        return format switch
        {
            ThreatFeedFormat.IPsumTabDelimited => ParseIPsum(content, minConfidenceOrLevel, maxEntries),
            ThreatFeedFormat.AbuseIpDbJson => ParseAbuseIpDbJson(content, minConfidenceOrLevel, maxEntries),
            _ => ParsePlainTextLines(content, maxEntries)
        };
    }

    private static List<string> ParsePlainTextLines(string content, int maxEntries)
    {
        HashSet<string> results = new(StringComparer.OrdinalIgnoreCase);
        using StringReader reader = new(content);
        string? line;

        while ((line = reader.ReadLine()) != null && results.Count < maxEntries)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';') || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            // 處理含有註解或附屬欄位之行 (例如: 198.51.100.1 ; Description)
            int commentIndex = line.IndexOfAny([';', '#', ' ']);
            string candidate = commentIndex > 0 ? line[..commentIndex].Trim() : line;

            // 處理 CIDR (例如: 198.51.100.0/24 取 198.51.100.0)
            int slashIndex = candidate.IndexOf('/');
            if (slashIndex > 0)
            {
                candidate = candidate[..slashIndex].Trim();
            }

            if (IPAddress.TryParse(candidate, out IPAddress? address) && !BogonIpFilter.IsBogonOrReserved(address))
            {
                results.Add(IpAddressCanonicalizer.Canonicalize(address).ToString());
            }
        }

        return [.. results];
    }

    private static List<string> ParseIPsum(string content, int minLevel, int maxEntries)
    {
        HashSet<string> results = new(StringComparer.OrdinalIgnoreCase);
        using StringReader reader = new(content);
        string? line;

        while ((line = reader.ReadLine()) != null && results.Count < maxEntries)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            string[] parts = line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            string ipCandidate = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out int level) || level < minLevel)
                continue;

            if (IPAddress.TryParse(ipCandidate, out IPAddress? address) && !BogonIpFilter.IsBogonOrReserved(address))
            {
                results.Add(IpAddressCanonicalizer.Canonicalize(address).ToString());
            }
        }

        return [.. results];
    }

    private static List<string> ParseAbuseIpDbJson(string content, int minConfidence, int maxEntries)
    {
        HashSet<string> results = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            using JsonDocument doc = JsonDocument.Parse(content);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("data", out JsonElement dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in dataElement.EnumerateArray())
                {
                    if (results.Count >= maxEntries) break;

                    string? ip = item.TryGetProperty("ipAddress", out JsonElement ipElem) ? ipElem.GetString() : null;
                    int score = item.TryGetProperty("abuseConfidenceScore", out JsonElement scoreElem) && scoreElem.TryGetInt32(out int s) ? s : 0;

                    if (string.IsNullOrWhiteSpace(ip) || score < minConfidence)
                        continue;

                    if (IPAddress.TryParse(ip.Trim(), out IPAddress? address) && !BogonIpFilter.IsBogonOrReserved(address))
                    {
                        results.Add(IpAddressCanonicalizer.Canonicalize(address).ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("AbuseIPDB JSON parsing encountered error: {0}", ex.Message);
        }

        return [.. results];
    }
}
