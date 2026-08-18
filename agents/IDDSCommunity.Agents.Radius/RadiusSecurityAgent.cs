using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Radius;

/// <summary>
/// 監看 Windows NPS/RADIUS 之安全性事件記錄，偵測重複的憑證拒絕事件。
/// </summary>
[Plugin("Windows NPS RADIUS Security Agent", "Detects repeated credential-rejection events from Windows NPS.", "1.0")]
public sealed class RadiusSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    /// <summary>
    /// 初始化 <see cref="RadiusSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public RadiusSecurityAgent() : base(new WindowsEventLogFailureSource("Security", "*[System[(EventID=6273)]]", Parse)) { }
    /// <summary>
    /// 以自訂事件來源初始化 <see cref="RadiusSecurityAgent"/> 類別的新執行個體，供單元測試使用。
    /// </summary>
    /// <param name="source">自訂驗證失敗事件來源。</param>
    internal RadiusSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("NPS RADIUS Security Agent"); set { } }
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public override Guid Id => new("{981D2895-B343-477B-A2BD-21832FDD1305}");

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

    internal static AuthenticationFailureEvent? TryParseFields(IReadOnlyDictionary<string, string> fields, DateTimeOffset occurredAt, int eventId = 6273)
    {
        if (!string.Equals(EventRecordFields.Get(fields, "ReasonCode"), "16", StringComparison.Ordinal)) return null;
        string source = EventRecordFields.Get(fields, "ClientIPAddress", "NASIPv4Address", "CallingStationID");
        if (!IPAddress.TryParse(source.Trim('[', ']'), out IPAddress? address)) return null;
        return new AuthenticationFailureEvent(
            occurredAt,
            address,
            eventId,
            "NPS/RADIUS",
            EventRecordFields.Get(fields, "UserName", "AccountName"),
            "Credential mismatch",
            ProviderOrChannel: "Security",
            ErrorCode: "16",
            AccountDomain: EventRecordFields.Get(fields, "DomainName"),
            AccountSid: EventRecordFields.Get(fields, "UserSid"));
    }
}
