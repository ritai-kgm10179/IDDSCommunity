using System;
using System.Net;

namespace IDDSCommunity.Agents.WindowsDns;

internal enum DnsActivityKind
{
    Query,
    DynamicUpdate,
    ZoneTransfer
}

internal sealed record DnsEventRecord(
    int EventId,
    DateTimeOffset OccurredAt,
    IPAddress SourceAddress,
    DnsActivityKind Kind,
    string QueryName,
    string QueryType,
    string ResponseCode)
{
    internal bool IsNxDomain => ResponseCode.Equals("3", StringComparison.OrdinalIgnoreCase) || ResponseCode.Equals("NXDOMAIN", StringComparison.OrdinalIgnoreCase);
    internal bool IsAnyQuery => QueryType.Equals("255", StringComparison.OrdinalIgnoreCase) || QueryType.Equals("ANY", StringComparison.OrdinalIgnoreCase);
}

internal enum DnsDetectionType
{
    QueryRate,
    NxDomainRate,
    AnyQueryRate,
    DynamicUpdateRate,
    ZoneTransfer
}

internal sealed record DnsDetection(DnsDetectionType Type, DnsEventRecord SourceEvent);
