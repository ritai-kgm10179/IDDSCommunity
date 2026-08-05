using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Radius;

[Plugin("Windows NPS RADIUS Security Agent", "Detects repeated credential-rejection events from Windows NPS.", "1.0")]
public sealed class RadiusSecurityAgent : AuthenticationAgentBase<AuthenticationAgentConfiguration>
{
    public RadiusSecurityAgent() : base(new WindowsEventLogFailureSource("Security", "*[System[(EventID=6273)]]", Parse)) { }
    internal RadiusSecurityAgent(IAuthenticationEventSource source) : base(source) { }
    protected override Color AgentColor => Color.FromArgb(45, 135, 156);
    public override string DisplayName { get => IntrusionDetection.Api.Localization.Strings.Get("NPS RADIUS Security Agent"); set { } }
    public override Guid Id => new("{981D2895-B343-477B-A2BD-21832FDD1305}");

    internal static AuthenticationFailureEvent? Parse(EventRecord record)
    {
        IReadOnlyDictionary<string, string> fields = EventRecordFields.Read(record);
        return TryParseFields(fields, record.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow, record.Id);
    }

    internal static AuthenticationFailureEvent? TryParseFields(IReadOnlyDictionary<string, string> fields, DateTimeOffset occurredAt, int eventId = 6273)
    {
        if (!string.Equals(EventRecordFields.Get(fields, "ReasonCode"), "16", StringComparison.Ordinal)) return null;
        string source = EventRecordFields.Get(fields, "ClientIPAddress", "NASIPv4Address", "CallingStationID");
        if (!IPAddress.TryParse(source.Trim('[', ']'), out IPAddress? address)) return null;
        return new AuthenticationFailureEvent(occurredAt, address, eventId, "NPS/RADIUS", EventRecordFields.Get(fields, "UserName", "AccountName"), "Credential mismatch");
    }
}
