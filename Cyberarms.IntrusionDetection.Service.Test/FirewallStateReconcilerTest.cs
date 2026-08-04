using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Service.Test;

[TestClass]
public sealed class FirewallStateReconcilerTest
{
    /// <summary>
    /// Verifies missing desired locks are applied, pending states finalize, and stale addresses are removed.
    /// </summary>
    [TestMethod]
    public void Reconcile_DivergentState_ConvergesToDatabaseIntent()
    {
        FakeFirewallPolicy firewall = new(["198.51.100.9"]);
        Lock pending = new() { IpAddress = "192.0.2.10", Status = Lock.LOCK_STATUS_SOFTLOCK_REQUESTED };
        List<Lock> saved = [];
        List<string> audits = [];
        FirewallStateReconciler reconciler = new(
            firewall,
            () => [pending],
            saved.Add,
            (_, outcome, subject, details) => audits.Add($"{outcome}:{subject}:{details}"),
            (_, exception) => Assert.Fail(exception.Message));

        reconciler.Reconcile();

        CollectionAssert.AreEquivalent(new[] { "192.0.2.10" }, new List<string>(firewall.GetBlockedAddresses()));
        Assert.AreEqual(Lock.LOCK_STATUS_SOFTLOCK, pending.Status);
        Assert.HasCount(1, saved);
        CollectionAssert.Contains(audits, "Succeeded:192.0.2.10:AddOrVerify");
        CollectionAssert.Contains(audits, "Succeeded:198.51.100.9:RemoveStale");
    }

    /// <summary>
    /// Verifies a firewall failure preserves the requested state for a later reconciliation attempt.
    /// </summary>
    [TestMethod]
    public void Reconcile_BlockFailure_PreservesPendingState()
    {
        FakeFirewallPolicy firewall = new([]) { FailBlock = true };
        Lock pending = new() { IpAddress = "192.0.2.20", Status = Lock.LOCK_STATUS_HARDLOCK_REQUESTED };
        int failures = 0;
        FirewallStateReconciler reconciler = new(
            firewall,
            () => [pending],
            _ => Assert.Fail("A failed firewall operation must not finalize the lock."),
            (_, _, _, _) => { },
            (_, _) => failures++);

        reconciler.Reconcile();

        Assert.AreEqual(Lock.LOCK_STATUS_HARDLOCK_REQUESTED, pending.Status);
        Assert.AreEqual(1, failures);
    }

    private sealed class FakeFirewallPolicy(IEnumerable<string> initialAddresses) : IFirewallPolicy
    {
        private readonly HashSet<string> addresses = new(initialAddresses, StringComparer.Ordinal);
        internal bool FailBlock { get; init; }

        public void Block(string ipAddress)
        {
            if (FailBlock)
                throw new InvalidOperationException("expected");
            addresses.Add(ipAddress);
        }

        public bool IsLocked(string ipAddress) => addresses.Contains(ipAddress);
        public IReadOnlyCollection<string> GetBlockedAddresses() => addresses;
        public void RemoveIpAddressFromBlockList(string ipAddress) => addresses.Remove(ipAddress);
    }
}
