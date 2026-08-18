namespace IDDSCommunity.Agents.RemoteDesktopGateway;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Xml.Linq;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 提供 Remote Desktop Gateway (RD Gateway) 與關聯 NPS/IIS 來源之具型別事件剖析器。
/// </summary>
public static class RdGatewayEventParser
{
    /// <summary>
    /// 將 Windows EventRecord 解析為標準化之 RD Gateway 驗證失敗事件。
    /// </summary>
    /// <param name="record">Windows 事件記錄物件。</param>
    /// <param name="trustedProxyCidrs">受信任反向代理 CIDR 清單。</param>
    /// <returns>解析成功之 <see cref="AuthenticationFailureEvent"/>；若為授權成功、非目標事件或回呼 IP 則傳回 <see langword="null"/>。</returns>
    public static AuthenticationFailureEvent? Parse(EventRecord record, IEnumerable<string>? trustedProxyCidrs = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        IReadOnlyDictionary<string, string> fields = ReadNamedAndPositionalFields(record.ToXml());
        DateTimeOffset occurredAt = record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow;
        int eventId = record.Id;
        string channel = record.LogName ?? string.Empty;

        return TryParseFields(fields, occurredAt, eventId, channel, trustedProxyCidrs);
    }

    /// <summary>
    /// 依據欄位字典與事件來源資訊解析 RD Gateway / NPS 驗證失敗事件。
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
        // 1. 處理 TerminalServices-Gateway 頻道之事件 (201=CAP拒絕, 202=RAP拒絕, 304=原則讀取失敗, 200/300=成功)
        if (channel.Equals("Microsoft-Windows-TerminalServices-Gateway/Operational", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGatewayOperational(fields, occurredAt, eventId, trustedProxyCidrs);
        }

        // 2. 處理 Security 頻道之 NPS / RADIUS Event 6273 (NPS 拒絕連線)
        if (eventId == 6273 && channel.Equals("Security", StringComparison.OrdinalIgnoreCase))
        {
            return ParseNpsFailure(fields, occurredAt, eventId, trustedProxyCidrs);
        }

        return null;
    }

    /// <summary>
    /// 判斷指定事件是否為登入/連線成功事件（Event 200 或 300），可用於重置或強化身分可信度。
    /// </summary>
    /// <param name="eventId">事件識別碼。</param>
    /// <returns>若為成功事件傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsSuccessEvent(int eventId) => eventId is 200 or 300 or 6272;

    private static AuthenticationFailureEvent? ParseGatewayOperational(
        IReadOnlyDictionary<string, string> fields,
        DateTimeOffset occurredAt,
        int eventId,
        IEnumerable<string>? trustedProxyCidrs)
    {
        // 成功事件不作為失敗事件輸出
        if (IsSuccessEvent(eventId))
        {
            return null;
        }

        // Event 201: CAP Policy Denied
        // Event 202: RAP Policy Denied
        // Event 304: Policy Failure
        if (eventId is not (201 or 202 or 304))
        {
            return null;
        }

        string rawIp = EventRecordFields.Get(fields, "IpAddress", "ipAddress", "ClientIP", "clientIP", "param2", "param1");
        if (!TrustedProxyParser.TryParseCleanIp(rawIp, out IPAddress? directPeer) || directPeer is null || IPAddress.IsLoopback(directPeer))
        {
            return null;
        }

        // 支援 X-Forwarded-For / Forwarded 標頭解析（若 RD Gateway 部署於 Reverse Proxy / Load Balancer 後端）
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

        string account = EventRecordFields.Get(fields, "Username", "username", "UserName", "User", "param0");
        string resource = EventRecordFields.Get(fields, "Resource", "resource", "param3");
        string errorCode = EventRecordFields.Get(fields, "ErrorCode", "errorCode", "param4");

        string reason = eventId switch
        {
            201 => $"RD Gateway CAP Policy Denied (Code: {errorCode})",
            202 => $"RD Gateway RAP Policy Denied (Resource: {resource}, Code: {errorCode})",
            304 => $"RD Gateway Policy Read Error (Code: {errorCode})",
            _ => $"RD Gateway Failure ({eventId})"
        };

        // RD Gateway 201 (CAP), 202 (RAP), 304 (Policy Error) 為授權與存取原則拒絕，非密碼驗證失敗；
        // 明確標示 IsCredentialFailure = false，僅供 Telemetry 與關聯 Provenance 記錄，不得計入密碼噴灑門檻。
        return new AuthenticationFailureEvent(
            occurredAt,
            resolvedIp,
            eventId,
            "RDGateway",
            account,
            reason,
            IsCredentialFailure: false,
            ConfidenceScore: 0.5);
    }

    /// <summary>
    /// NPS / RADIUS 失敗原因代碼分類類別。
    /// </summary>
    public enum NpsReasonClassification
    {
        /// <summary>
        /// 認證憑證無效或密碼錯誤（如 ReasonCode 16: 認證失敗），計入暴力嘗試與密碼噴灑。
        /// </summary>
        CredentialFailure,

        /// <summary>
        /// EAP 驗證失敗（ReasonCode 23），需依據 EAP Type 判斷是否為密碼型驗證（如 PEAP/MS-CHAPv2）。
        /// </summary>
        EapMethodFailure,

        /// <summary>
        /// 帳號狀態限制（如 ReasonCode 18: 帳號停用、19: 帳號過期、22: 帳號已鎖定），作為診斷參考。
        /// </summary>
        AccountRestriction,

        /// <summary>
        /// 原則不符或基礎設施故障（如 ReasonCode 34: 找不到相符網路原則、65: 密鑰錯誤、RADIUS 逾時），不計入攻擊門檻。
        /// </summary>
        PolicyOrInfrastructureFailure,

        /// <summary>
        /// 未知或未指定之原因代碼。
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 依據 Microsoft 官方 NPS 規範對 Event 6273 ReasonCode 進行語意分類。
    /// </summary>
    /// <param name="reasonCode">NPS 失敗原因代碼字串。</param>
    /// <returns>NPS 原因分類結果。</returns>
    public static NpsReasonClassification ClassifyNpsReason(string? reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode)) return NpsReasonClassification.Unknown;

        return reasonCode.Trim() switch
        {
            "16" => NpsReasonClassification.CredentialFailure,
            "23" => NpsReasonClassification.EapMethodFailure,
            "18" or "19" or "22" => NpsReasonClassification.AccountRestriction,
            "34" or "35" or "48" or "49" or "65" or "66" or "80" => NpsReasonClassification.PolicyOrInfrastructureFailure,
            _ => NpsReasonClassification.PolicyOrInfrastructureFailure
        };
    }

    private static AuthenticationFailureEvent? ParseNpsFailure(
        IReadOnlyDictionary<string, string> fields,
        DateTimeOffset occurredAt,
        int eventId,
        IEnumerable<string>? trustedProxyCidrs)
    {
        string reasonCode = EventRecordFields.Get(fields, "ReasonCode");
        NpsReasonClassification classification = ClassifyNpsReason(reasonCode);

        bool isCredentialFailure = false;
        double confidence = 0.5;

        if (classification == NpsReasonClassification.CredentialFailure)
        {
            // 官方確認 ReasonCode 16 為明確憑證失敗
            isCredentialFailure = true;
            confidence = 1.0;
        }
        else if (classification == NpsReasonClassification.EapMethodFailure)
        {
            // ReasonCode 23 預設為 Telemetry-only，僅在官方欄位證明為密碼型 EAP (如 PEAP / MS-CHAPv2) 時給予低信心關聯
            string eapType = EventRecordFields.Get(fields, "EAPType", "AuthenticationType", "EapFriendlyName");
            if (eapType.Contains("26", StringComparison.Ordinal) ||
                eapType.Contains("MS-CHAP", StringComparison.OrdinalIgnoreCase) ||
                eapType.Contains("MSCHAP", StringComparison.OrdinalIgnoreCase))
            {
                isCredentialFailure = true;
                confidence = 0.5; // 低信心關聯
            }
            else
            {
                // EAP-TLS (13) 或憑證信任鏈問題，歸為 Telemetry-only
                isCredentialFailure = false;
                confidence = 0.3;
            }
        }
        else
        {
            // 原則不符、帳號鎖定或基礎設施故障，僅作為 Telemetry
            isCredentialFailure = false;
            confidence = 0.2;
        }

        string rawIp = EventRecordFields.Get(fields, "CallingStationID", "ClientIPAddress", "NASIPv4Address", "FramedIPAddress");
        if (!TrustedProxyParser.TryParseCleanIp(rawIp, out IPAddress? directPeer) || directPeer is null || IPAddress.IsLoopback(directPeer))
        {
            return null;
        }

        IPAddress resolvedIp = directPeer;
        string forwarded = EventRecordFields.Get(fields, "Forwarded", "forwarded");
        string xForwardedFor = EventRecordFields.Get(fields, "X-Forwarded-For", "x-forwarded-for");
        if ((!string.IsNullOrWhiteSpace(forwarded) || !string.IsNullOrWhiteSpace(xForwardedFor)) && trustedProxyCidrs is not null)
        {
            resolvedIp = TrustedProxyParser.ResolveClientIp(directPeer, forwarded, xForwardedFor, trustedProxyCidrs);
        }

        if (IPAddress.IsLoopback(resolvedIp))
        {
            return null;
        }

        string account = EventRecordFields.Get(fields, "AccountName", "UserName", "User");
        return new AuthenticationFailureEvent(
            occurredAt,
            resolvedIp,
            eventId,
            "RDGateway-NPS",
            account,
            $"NPS Denied (ReasonCode: {reasonCode})",
            IsCredentialFailure: isCredentialFailure,
            ConfidenceScore: confidence);
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
            }
        }
        catch (Exception)
        {
            // 忽略非格式化 XML 錯誤，維持安全降級
        }

        return values;
    }
}
