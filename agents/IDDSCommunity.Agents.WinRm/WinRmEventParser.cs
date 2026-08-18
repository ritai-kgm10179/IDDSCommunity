namespace IDDSCommunity.Agents.WinRm;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Xml.Linq;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 提供 Windows Remote Management (WinRM)、PowerShell Remoting 與 Windows Admin Center (WAC) 之具型別事件剖析器。
/// </summary>
public static class WinRmEventParser
{
    /// <summary>
    /// 將 Windows EventRecord 解析為標準化之 WinRM 驗證失敗事件。
    /// </summary>
    /// <param name="record">Windows 事件記錄物件。</param>
    /// <param name="trustedProxyCidrs">受信任反向代理 CIDR 清單（若有設定）。</param>
    /// <returns>解析成功之 <see cref="AuthenticationFailureEvent"/>；若非相關失敗事件或為回呼 IP 則傳回 <see langword="null"/>。</returns>
    public static AuthenticationFailureEvent? Parse(EventRecord record, IEnumerable<string>? trustedProxyCidrs = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        IReadOnlyDictionary<string, string> fields = ReadNamedAndPositionalFields(record.ToXml());
        DateTimeOffset occurredAt = record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow;
        int eventId = record.Id;
        string channel = record.LogName ?? string.Empty;

        AuthenticationFailureEvent? failure = TryParseFields(fields, occurredAt, eventId, channel, trustedProxyCidrs);
        return failure is null
            ? null
            : failure with
            {
                ProviderOrChannel = channel,
                ComputerName = record.MachineName ?? string.Empty,
                SourceEventRecordId = record.RecordId
            };
    }

    /// <summary>
    /// 依據欄位字典與事件來源資訊解析 WinRM 驗證失敗事件。
    /// 依據 Windows Server 2016-2025 / Windows 10-11 之 WinRM Provider Manifest (Microsoft-Windows-WinRM) 驗證：
    /// Operational 頻道事件 142 (存取失敗)、161 (認證失敗)、192 (驗證失敗) 為權威性 WinRM 來源。
    /// 傳統安全性事件 4625 (LogonType 3) 因 ProcessName 於網路登入時常為 '-'，故不以程序字串硬篩，以避免漏報與誤歸因。
    /// </summary>
    /// <param name="fields">事件資料欄位字典。</param>
    /// <param name="occurredAt">事件發生時間。</param>
    /// <param name="eventId">事件識別碼。</param>
    /// <param name="channel">事件記錄通道名稱。</param>
    /// <param name="trustedProxyCidrs">受信任反向代理清單。</param>
    /// <returns>解析成功之驗證失敗事件，否則傳回 <see langword="null"/>。</returns>
    public static AuthenticationFailureEvent? TryParseFields(
        IReadOnlyDictionary<string, string> fields,
        DateTimeOffset occurredAt,
        int eventId,
        string channel,
        IEnumerable<string>? trustedProxyCidrs = null)
    {
        // 1. 隔離並忽略 PowerShell 腳本區塊事件 (Event 4104)，絕不剖析為 IP 且不持久化敏感腳本
        if (eventId == 4104 || channel.Contains("PowerShell", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // 2. 處理 Microsoft-Windows-WinRM/Operational 頻道之權威事件 (142, 161, 192)
        if (eventId is 142 or 161 or 192 &&
            channel.Equals("Microsoft-Windows-WinRM/Operational", StringComparison.OrdinalIgnoreCase))
        {
            return ParseWinRmOperational(fields, occurredAt, eventId, trustedProxyCidrs);
        }

        return null;
    }

    private static AuthenticationFailureEvent? ParseWinRmOperational(
        IReadOnlyDictionary<string, string> fields,
        DateTimeOffset occurredAt,
        int eventId,
        IEnumerable<string>? trustedProxyCidrs)
    {
        // 取得使用者帳號（排除指令碼內容與命令參數，絕不持久化敏感指令碼）
        string account = EventRecordFields.Get(fields, "userName", "Username", "User", "AccountName", "TargetUserName");
        if (string.Equals(account, "-", StringComparison.Ordinal) || string.Equals(account, "anonymous", StringComparison.OrdinalIgnoreCase))
        {
            account = string.Empty;
        }

        // 取得直接連線 IP 或遠端端點
        string rawDirectIp = EventRecordFields.Get(fields, "ipAddress", "IpAddress", "clientIP", "ClientIP", "sourceAddress", "SourceAddress", "machine", "Machine");
        if (!TrustedProxyParser.TryParseCleanIp(rawDirectIp, out IPAddress? directPeer) || directPeer is null || IPAddress.IsLoopback(directPeer))
        {
            return null;
        }

        // 取得 HTTP 轉發標頭（若存在於 WAC / HTTP 反向代理傳遞之事件屬性中）
        string forwarded = EventRecordFields.Get(fields, "Forwarded", "forwarded");
        string xForwardedFor = EventRecordFields.Get(fields, "X-Forwarded-For", "x-forwarded-for");
        IPAddress resolvedIp = directPeer;

        if ((!string.IsNullOrWhiteSpace(forwarded) || !string.IsNullOrWhiteSpace(xForwardedFor)) && trustedProxyCidrs is not null)
        {
            resolvedIp = TrustedProxyParser.ResolveClientIp(directPeer, forwarded, xForwardedFor, trustedProxyCidrs);
        }

        if (IPAddress.IsLoopback(resolvedIp))
        {
            return null;
        }

        string errorCode = EventRecordFields.Get(fields, "errorCode", "ErrorCode", "error", "Error", "status", "Status");
        if (string.IsNullOrWhiteSpace(errorCode)) errorCode = "0x80338000";

        bool isCredentialFailure = false;
        double confidence = 0.5;

        // Event 161 或錯誤碼明確為使用者密碼/憑證無效 (如 0x80338012, 0x8007052E, 0xC000006D 等) 判定為明確憑證失敗
        if (eventId == 161 ||
            errorCode.Contains("80338012", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("8007052E", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("C000006D", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("C0000064", StringComparison.OrdinalIgnoreCase) ||
            errorCode.Contains("C000006A", StringComparison.OrdinalIgnoreCase))
        {
            isCredentialFailure = true;
            confidence = 1.0;
        }
        else
        {
            // Event 142 (0x80070005 存取拒絕)、192 (授權失敗) 屬權限/原則問題，非密碼錯誤，僅作為 Telemetry
            isCredentialFailure = false;
            confidence = 0.5;
        }

        string activityId = EventRecordFields.Get(fields, "ActivityId", "CorrelationId", "activityId");

        return new AuthenticationFailureEvent(
            occurredAt,
            resolvedIp,
            eventId,
            "WinRM",
            account,
            $"WinRM Failure (Code: {errorCode})",
            IsCredentialFailure: isCredentialFailure,
            ActivityId: string.IsNullOrWhiteSpace(activityId) ? null : activityId,
            ConfidenceScore: confidence,
            ProviderOrChannel: "Microsoft-Windows-WinRM/Operational",
            ErrorCode: errorCode);
    }

    internal static IReadOnlyDictionary<string, string> ReadNamedAndPositionalFields(string xml)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (string.IsNullOrWhiteSpace(xml)) return values;

            XDocument doc = XDocument.Parse(xml, LoadOptions.None);
            int positionalIndex = 0;

            foreach (XElement element in doc.Descendants())
            {
                if (element.Name.LocalName is "Data" or "param" or "Item")
                {
                    string? name = element.Attribute("Name")?.Value;
                    string val = element.Value;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        values[name] = val;
                    }
                    else
                    {
                        values[$"param{positionalIndex}"] = val;
                        positionalIndex++;
                    }
                }
                else if (element.Name.LocalName is "Correlation" or "ActivityID")
                {
                    string? actId = element.Attribute("ActivityID")?.Value ?? element.Attribute("CorrelationId")?.Value;
                    if (!string.IsNullOrWhiteSpace(actId))
                    {
                        values["ActivityId"] = actId;
                    }
                }
            }
        }
        catch (Exception)
        {
            // 忽略非格式化 XML 錯誤，維持安全降級
        }

        return values;
    }
}
