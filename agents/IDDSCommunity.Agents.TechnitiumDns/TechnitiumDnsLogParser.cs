using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.TechnitiumDns;

/// <summary>
/// 解析 Technitium DNS Server 記錄檔的單行文字，辨識遭拒絕/封鎖之查詢並擷取來源 IP 位址。
/// </summary>
[SupportedOSPlatform("windows7.0")]
public static partial class TechnitiumDnsLogParser
{
    [GeneratedRegex(@"(?:Client\s+|ip=)(?<ip>[0-9a-fA-F:\.]+?)(?::\d+)?\s+.*(?:Refused|Blocked|exceeded QPM limit|Rate limit|Dropped|Denied)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex GetThreatRegex();

    /// <summary>
    /// 嘗試將單行記錄文字解析為驗證失敗事件。
    /// </summary>
    /// <param name="line">待解析之單行記錄文字。</param>
    /// <param name="occurredAt">事件發生時間。</param>
    /// <returns>解析成功時傳回驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        if (line.IndexOf("Client", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("ip=", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return null;
        }
        if (line.IndexOf("Refused", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("Blocked", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("exceeded QPM limit", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("Rate limit", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("Dropped", StringComparison.OrdinalIgnoreCase) < 0 &&
            line.IndexOf("Denied", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return null;
        }

        Match match;
        try
        {
            match = GetThreatRegex().Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 53, "TechnitiumDNS", string.Empty, "Technitium DNS query refused or blocked");
    }
}
