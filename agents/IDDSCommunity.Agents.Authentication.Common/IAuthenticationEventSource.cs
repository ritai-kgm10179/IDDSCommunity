using System;

namespace IDDSCommunity.Agents.Authentication.Common;

public interface IAuthenticationEventSource : IDisposable
{
    event EventHandler<AuthenticationFailureEvent>? EventReceived;
    event Action<Exception>? Error;
    void Start();
    void Pause();
    void Resume();
    void Stop();
}
