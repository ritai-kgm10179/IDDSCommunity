using System;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla;

/// <summary>
/// 解析 FileZilla Server 驗證記錄檔的單行文字，辨識驗證失敗訊息並擷取來源 IP 位址。
/// </summary>
[SupportedOSPlatform("windows7.0")]
public static partial class FileZillaLogParser
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?\s+(?:\[[^\]]*\]\s+)?(?<ip>[0-9a-fA-F:\.]+)\s+-\s+.*(?:authentication failed|login failed|password incorrect|530 login incorrect|530 authentication failed)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 250)]
    private static partial Regex GetFailureRegex();

    /// <summary>
    /// 嘗試將單行記錄文字解析為驗證失敗事件。
    /// </summary>
    /// <param name="line">待解析之單行記錄文字。</param>
    /// <param name="occurredAt">事件發生時間。</param>
    /// <returns>解析成功時傳回驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    public static AuthenticationFailureEvent? TryParseMessage(string line, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        Match match;
        try
        {
            match = GetFailureRegex().Match(line);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
        if (!match.Success) return null;

        string ipText = match.Groups["ip"].Value.Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? address))
            return null;

        return new AuthenticationFailureEvent(occurredAt, address, 530, "FileZilla", string.Empty, "FileZilla authentication failed");
    }
}
