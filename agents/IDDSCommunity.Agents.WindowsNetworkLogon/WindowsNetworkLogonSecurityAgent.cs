using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsNetworkLogon;

/// <summary>
/// 監看 Windows 安全性事件記錄，偵測高可信度之網路登入（事件 4625、登入類型 3）驗證失敗事件。
/// </summary>
[Plugin("Windows Network Logon Security Agent", "Detects repeated high-confidence Windows network logon failures.", "1.0")]
public sealed class WindowsNetworkLogonSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="WindowsNetworkLogonSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public WindowsNetworkLogonSecurityAgent() : base(new WindowsEventLogFailureSource("Security", "*[System[(EventID=4625)]] and *[EventData[Data[@Name='LogonType']='3']]", Parse)) { }
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="WindowsNetworkLogonSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal WindowsNetworkLogonSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Windows Network Logon Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{61F99E76-4C53-4D88-8C4A-1AF5D1A0C219}");

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

    internal static AuthenticationFailureEvent? TryParseFields(IReadOnlyDictionary<string, string> fields, DateTimeOffset occurredAt, int eventId = 4625)
    {
        if (!string.Equals(EventRecordFields.Get(fields, "LogonType"), "3", StringComparison.Ordinal)) return null;
        string status = EventRecordFields.Get(fields, "Status");
        string subStatus = EventRecordFields.Get(fields, "SubStatus");
        if (status is not ("0xC000006D" or "0xc000006d") && subStatus is not ("0xC0000064" or "0xC000006A" or "0xc0000064" or "0xc000006a")) return null;
        if (!IPAddress.TryParse(EventRecordFields.Get(fields, "IpAddress", "SourceNetworkAddress").Trim('[', ']'), out IPAddress? address) || IPAddress.IsLoopback(address)) return null;
        return new AuthenticationFailureEvent(
            occurredAt,
            address,
            eventId,
            "WindowsNetworkLogon",
            EventRecordFields.Get(fields, "TargetUserName"),
            $"{status}/{subStatus}",
            ProviderOrChannel: "Security",
            ErrorCode: string.IsNullOrWhiteSpace(subStatus) ? status : subStatus,
            AccountDomain: EventRecordFields.Get(fields, "TargetDomainName"),
            AccountSid: EventRecordFields.Get(fields, "TargetUserSid"));
    }
}
