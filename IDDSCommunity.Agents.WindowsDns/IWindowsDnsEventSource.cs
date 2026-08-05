using System;

namespace IDDSCommunity.Agents.WindowsDns;

internal interface IWindowsDnsEventSource : IDisposable
{
    event EventHandler<DnsEventRecord>? EventReceived;
    event Action<Exception>? Error;
    void Start();
    void Pause();
    void Resume();
    void Stop();
}
