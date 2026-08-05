using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.WindowsNetworkLogon;

[Plugin("Windows Network Logon Security Agent", "Detects repeated high-confidence Windows network logon failures.", "1.0")]
public sealed class WindowsNetworkLogonSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    public WindowsNetworkLogonSecurityAgent() : base(new WindowsEventLogFailureSource("Security", "*[System[(EventID=4625)]] and *[EventData[Data[@Name='LogonType']='3']]", Parse)) { }
    internal WindowsNetworkLogonSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    protected override Color AgentColor => Color.FromArgb(40, 132, 155);
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("Windows Network Logon Security Agent"); set { } }
    public override Guid Id => new("{61F99E76-4C53-4D88-8C4A-1AF5D1A0C219}");

    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        IReadOnlyDictionary<string, string> fields = EventRecordFields.Read(record);
        return TryParseFields(fields, record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow, record.Id);
    }

    internal static AuthenticationFailureEvent? TryParseFields(IReadOnlyDictionary<string, string> fields, DateTimeOffset occurredAt, int eventId = 4625)
    {
        if (!string.Equals(EventRecordFields.Get(fields, "LogonType"), "3", StringComparison.Ordinal)) return null;
        string status = EventRecordFields.Get(fields, "Status");
        string subStatus = EventRecordFields.Get(fields, "SubStatus");
        if (status is not ("0xC000006D" or "0xc000006d") && subStatus is not ("0xC0000064" or "0xC000006A" or "0xc0000064" or "0xc000006a")) return null;
        if (!IPAddress.TryParse(EventRecordFields.Get(fields, "IpAddress", "SourceNetworkAddress").Trim('[', ']'), out IPAddress? address) || IPAddress.IsLoopback(address)) return null;
        return new AuthenticationFailureEvent(occurredAt, address, eventId, "WindowsNetworkLogon", EventRecordFields.Get(fields, "TargetUserName"), $"{status}/{subStatus}");
    }
}
