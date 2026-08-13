using System;

namespace IDDSCommunity.IntrusionDetection.Service;

internal class ClientOperationInformation
{
    internal string IpAddress { get; set; } = string.Empty;
    internal Exception? Exception { get; set; }
    internal string Message { get; set; } = string.Empty;
    internal bool HasError { get; set; }
    internal Guid AgentId { get; set; }
}
