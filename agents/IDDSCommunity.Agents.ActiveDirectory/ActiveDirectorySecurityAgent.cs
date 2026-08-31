using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.ActiveDirectory;

/// <summary>
/// 監看 Windows Active Directory 安全性事件記錄，偵測 Kerberoasting、AS-REP Roasting、Kerberos 預先驗證失敗與網域帳號鎖定攻擊。
/// </summary>
[Plugin("Active Directory & Kerberos Security Agent", "Detects Kerberoasting, AS-REP Roasting, and AD Account Lockout DoS attacks.", "1.0")]
public sealed class ActiveDirectorySecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    private const string EventQuery = "*[System[(EventID=4768 or EventID=4769 or EventID=4771 or EventID=4740)]]";

    /// <summary>
    /// 初始化 <see cref="ActiveDirectorySecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public ActiveDirectorySecurityAgent() : base(new WindowsEventLogFailureSource("Security", EventQuery, Parse)) { }

    /// <summary>
    /// 以自訂事件來源初始化 <see cref="ActiveDirectorySecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal ActiveDirectorySecurityAgent(IAuthenticationEventSource source) : base(source) { }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Active Directory & Kerberos Security Agent"); set { } }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => WellKnownAgentIds.AdCredentialValidation;

    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        IReadOnlyDictionary<string, string> fields = EventRecordFields.Read(record);
        AuthenticationFailureEvent? failure = TryParseFields(fields, record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow, record.Id);
        return failure is null
            ? null
            : failure with
            {
                ProviderOrChannel = record.LogName ?? "Security",
                ComputerName = record.MachineName ?? string.Empty,
                SourceEventRecordId = record.RecordId
            };
    }

    internal static AuthenticationFailureEvent? TryParseFields(IReadOnlyDictionary<string, string> fields, DateTimeOffset occurredAt, int eventId)
    {
        string rawIp = EventRecordFields.Get(fields, "IpAddress", "ClientAddress", "SourceNetworkAddress").Trim('[', ']', ':', 'f', 'F');
        // Handle IPv6 mapped format or port suffix e.g. ::ffff:192.168.1.1 or 192.168.1.1:54321
        if (rawIp.Contains(':'))
        {
            int lastColon = rawIp.LastIndexOf(':');
            if (rawIp.IndexOf(':') == lastColon && IPAddress.TryParse(rawIp[..lastColon], out _))
            {
                rawIp = rawIp[..lastColon];
            }
        }

        if (!IPAddress.TryParse(rawIp, out IPAddress? address) || IPAddress.IsLoopback(address))
        {
            return null;
        }

        string targetUser = EventRecordFields.Get(fields, "TargetUserName", "TargetName");
        string serviceName = EventRecordFields.Get(fields, "ServiceName");
        string ticketOptions = EventRecordFields.Get(fields, "TicketOptions");
        string ticketEncryptionType = EventRecordFields.Get(fields, "TicketEncryptionType");
        string status = EventRecordFields.Get(fields, "Status", "FailureCode", "ResultCode");

        switch (eventId)
        {
            case 4769:
                // Kerberoasting 偵測：請求 TGS 票證且加密演算法為弱加密 RC4-HMAC (0x17 / 0x18)
                if (ticketEncryptionType is "0x17" or "0x18" && !string.IsNullOrWhiteSpace(serviceName) && !serviceName.EndsWith("$", StringComparison.Ordinal))
                {
                    return new AuthenticationFailureEvent(
                        occurredAt,
                        address,
                        eventId,
                        "ActiveDirectory.Kerberoasting",
                        targetUser,
                        $"Kerberoasting RC4 request for SPN {serviceName}",
                        ProviderOrChannel: "Security",
                        ErrorCode: ticketEncryptionType,
                        AccountDomain: EventRecordFields.Get(fields, "TargetDomainName"));
                }
                return null;

            case 4771:
                // Kerberos Pre-Authentication 失敗 (AS-REP Roasting 或暴力猜解)
                if (status is not ("0x0" or "0"))
                {
                    return new AuthenticationFailureEvent(
                        occurredAt,
                        address,
                        eventId,
                        "ActiveDirectory.PreAuthFailed",
                        targetUser,
                        $"Kerberos Pre-Auth failed ({status})",
                        ProviderOrChannel: "Security",
                        ErrorCode: status,
                        AccountDomain: EventRecordFields.Get(fields, "TargetDomainName"));
                }
                return null;

            case 4768:
                // Kerberos TGT 請求失敗
                if (status is not ("0x0" or "0"))
                {
                    return new AuthenticationFailureEvent(
                        occurredAt,
                        address,
                        eventId,
                        "ActiveDirectory.TgtFailed",
                        targetUser,
                        $"Kerberos TGT request failed ({status})",
                        ProviderOrChannel: "Security",
                        ErrorCode: status,
                        AccountDomain: EventRecordFields.Get(fields, "TargetDomainName"));
                }
                return null;

            case 4740:
                // 帳號遭鎖定 (Account Lockout DoS 防護)
                return new AuthenticationFailureEvent(
                    occurredAt,
                    address,
                    eventId,
                    "ActiveDirectory.AccountLockout",
                    targetUser,
                    $"AD User account lockout triggered for {targetUser}",
                    ProviderOrChannel: "Security",
                    ErrorCode: "LockedOut",
                    AccountDomain: EventRecordFields.Get(fields, "TargetDomainName"));

            default:
                return null;
        }
    }
}
