using System;

namespace Cyberarms.IntrusionDetection.Service;

internal class ClientOperationInformation
{
    internal string IpAddress { get; set; }
    internal Exception Exception { get; set; }
    internal string Message { get; set; }
    internal bool HasError { get; set; }
    internal Guid AgentId { get; set; }
}
