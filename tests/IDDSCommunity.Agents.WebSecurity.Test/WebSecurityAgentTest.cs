using System;
using System.Net;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.WebSecurity;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.WebSecurity.Test;

[TestClass]
public sealed class WebSecurityAgentTest
{
    [TestMethod]
    public void Agent_LifecycleAndDetection_UseSharedThresholdFramework()
    {
        FakeEventSource source = new();
        WebSecurityAgent agent = new(source);
        AuthenticationAgentConfiguration configuration = (AuthenticationAgentConfiguration)agent.Configuration.AgentSettings!;
        configuration.FailureThreshold = 2;
        INotificationEventArgs? notification = null;
        agent.AttackDetected += (_, args) => notification = args;

        agent.Start();
        source.Raise(Failure("192.0.2.80"));
        Assert.IsNull(notification);
        source.Raise(Failure("192.0.2.80"));
        agent.Pause();
        agent.Continue();
        agent.Stop();

        Assert.IsNotNull(notification);
        Assert.AreEqual("192.0.2.80", notification!.IpAddress);
        Assert.AreEqual(1, source.StartCount);
        Assert.AreEqual(1, source.StopCount);
    }

    [TestMethod]
    public void Agent_EventSourceError_DoesNotStopSubscription()
    {
        FakeEventSource source = new();
        WebSecurityAgent agent = new(source);

        agent.Start();
        source.RaiseError(new InvalidOperationException("expected test failure"));
        source.Raise(Failure("192.0.2.90"));
        agent.Stop();

        Assert.AreEqual(1, source.StartCount);
        Assert.AreEqual(1, source.StopCount);
    }

    private static AuthenticationFailureEvent Failure(string address) =>
        new(DateTimeOffset.UtcNow, IPAddress.Parse(address), 4625, "Web Security", string.Empty, "Access denied");

    private sealed class FakeEventSource : IAuthenticationEventSource
    {
        public event EventHandler<AuthenticationFailureEvent>? EventReceived;
        public event Action<Exception>? Error;
        internal int StartCount { get; private set; }
        internal int StopCount { get; private set; }
        public void Start() => StartCount++;
        public void Pause() { }
        public void Resume() { }
        public void Stop() => StopCount++;
        public void Dispose() { }
        internal void Raise(AuthenticationFailureEvent failure) => EventReceived?.Invoke(this, failure);
        internal void RaiseError(Exception exception) => Error?.Invoke(exception);
    }
}
