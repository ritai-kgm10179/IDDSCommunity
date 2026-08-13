using System;
using System.Collections.Generic;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class CompositeAuthenticationEventSource : IAuthenticationEventSource
{
    private readonly IReadOnlyList<IAuthenticationEventSource> sources;

    public CompositeAuthenticationEventSource(params IAuthenticationEventSource[] sources)
    {
        this.sources = sources;
        foreach (IAuthenticationEventSource source in sources)
        {
            source.EventReceived += Forward;
            source.Error += ForwardError;
        }
    }

    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    public event Action<Exception>? Error;
    public void Start() { foreach (IAuthenticationEventSource source in sources) source.Start(); }
    public void Pause() { foreach (IAuthenticationEventSource source in sources) source.Pause(); }
    public void Resume() { foreach (IAuthenticationEventSource source in sources) source.Resume(); }
    public void Stop() { foreach (IAuthenticationEventSource source in sources) source.Stop(); }
    public void Dispose()
    {
        foreach (IAuthenticationEventSource source in sources)
        {
            source.EventReceived -= Forward;
            source.Error -= ForwardError;
            source.Dispose();
        }
    }

    private void Forward(object? sender, AuthenticationFailureEvent failure) => EventReceived?.Invoke(this, failure);
    private void ForwardError(Exception exception) => Error?.Invoke(exception);
}
