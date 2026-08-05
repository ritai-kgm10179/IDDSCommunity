using System;
using System.Net;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed record AuthenticationFailureEvent(
    DateTimeOffset OccurredAt,
    IPAddress SourceAddress,
    int EventId,
    string Category,
    string AccountName,
    string Reason);
