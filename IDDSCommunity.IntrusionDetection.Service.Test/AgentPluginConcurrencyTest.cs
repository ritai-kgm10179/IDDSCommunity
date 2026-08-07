using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class AgentPluginConcurrencyTest
{
    /// <summary>
    /// Verifies concurrent starts invoke the Agent start hook exactly once.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task Start_ConcurrentCallers_StartsExactlyOnceAsync()
    {
        ConcurrentAgent agent = new();
        ConcurrentBag<Exception> failures = [];

        await Task.WhenAll(Task.Run(Start), Task.Run(Start));

        Assert.AreEqual(1, agent.StartCount);
        Assert.HasCount(1, failures);
        Assert.IsInstanceOfType<InvalidOperationException>(failures.Single());

        void Start()
        {
            try
            {
                agent.Start();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }

    /// <summary>
    /// Verifies one faulty detection subscriber does not suppress later subscribers.
    /// </summary>
    [TestMethod]
    public void Detection_FaultySubscriber_IsolatedFromLaterSubscriber()
    {
        ConcurrentAgent agent = new();
        int delivered = 0;
        agent.AttackDetected += (_, _) => throw new InvalidOperationException("expected");
        agent.AttackDetected += (_, _) => delivered++;

        agent.Raise();

        Assert.AreEqual(1, delivered);
    }

    /// <summary>
    /// Verifies lifecycle state remains coherent across pause, continue, and stop.
    /// </summary>
    [TestMethod]
    public void Lifecycle_PauseContinueStop_PreservesValidState()
    {
        ConcurrentAgent agent = new();

        agent.Start();
        agent.Pause();
        Assert.IsTrue(agent.IsRunning);
        Assert.IsTrue(agent.IsPaused);
        agent.Continue();
        Assert.IsTrue(agent.IsRunning);
        Assert.IsFalse(agent.IsPaused);
        agent.Stop();
        Assert.IsFalse(agent.IsRunning);
        Assert.IsFalse(agent.IsPaused);
    }

    private sealed class ConcurrentAgent : AgentPlugin
    {
        internal int StartCount { get; private set; }

        internal void Raise() => OnAttackDetected(this, new NotificationEventArgs
        {
            CreateDate = DateTime.UtcNow,
            EventId = 1,
            EventMessage = "test",
            IpAddress = "192.0.2.1"
        });

        protected override void OnStartAgent() => StartCount++;
    }
}
