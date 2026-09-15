using System;
using System.Collections.Generic;
using System.Linq;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

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

    /// <summary>
    /// 驗證已收斂且非待處理的位址不會反覆產生稽核紀錄。
    /// </summary>
    [TestMethod]
    public void Reconcile_UnchangedState_DoesNotCreateAuditChurn()
    {
        FakeFirewallPolicy firewall = new(["192.0.2.30"]);
        Lock existing = new() { IpAddress = "192.0.2.30", Status = Lock.LOCK_STATUS_HARDLOCK };
        int audits = 0;
        FirewallStateReconciler reconciler = new(
            firewall, () => [existing], _ => Assert.Fail(), (_, _, _, _) => audits++, (_, exception) => Assert.Fail(exception.Message));

        reconciler.Reconcile();

        Assert.AreEqual(0, audits);
    }

    /// <summary>
    /// 驗證十萬筆一致狀態只讀取一次狀態，而且不執行任何防火牆寫入作業。
    /// </summary>
    [TestMethod]
    public void Reconcile_OneHundredThousandUnchanged_PerformsNoFirewallWrites()
    {
        string[] addresses = System.Linq.Enumerable.Range(0, 100_000)
            .Select(index => $"10.{index / 65536}.{index / 256 % 256}.{index % 256}")
            .ToArray();
        FakeFirewallPolicy firewall = new(addresses);
        Lock[] locks = addresses.Select(address => new Lock
        {
            IpAddress = address,
            Status = Lock.LOCK_STATUS_HARDLOCK
        }).ToArray();
        FirewallStateReconciler reconciler = new(
            firewall, () => locks, _ => Assert.Fail(), (_, _, _, _) => Assert.Fail(),
            (_, exception) => Assert.Fail(exception.Message));

        reconciler.Reconcile();

        Assert.AreEqual(1, firewall.StateReadCount);
        Assert.AreEqual(0, firewall.BatchBlockCount);
        Assert.AreEqual(0, firewall.BatchRemoveCount);
    }

    /// <summary>
    /// 驗證批次部分成功時只完成已套用位址，其餘位址保留供下次重試。
    /// </summary>
    [TestMethod]
    public void Reconcile_PartialBlockFailure_FinalizesAppliedAddressOnly()
    {
        FakeFirewallPolicy firewall = new([]) { FailAfterFirstBlock = true };
        Lock first = new() { IpAddress = "192.0.2.40", Status = Lock.LOCK_STATUS_SOFTLOCK_REQUESTED };
        Lock second = new() { IpAddress = "192.0.2.41", Status = Lock.LOCK_STATUS_SOFTLOCK_REQUESTED };
        List<Lock> saved = [];
        List<string> failures = [];
        FirewallStateReconciler reconciler = new(
            firewall, () => [first, second], saved.Add, (_, _, _, _) => { }, (address, _) => failures.Add(address));

        reconciler.Reconcile();

        Assert.AreEqual(Lock.LOCK_STATUS_SOFTLOCK, first.Status);
        Assert.AreEqual(Lock.LOCK_STATUS_SOFTLOCK_REQUESTED, second.Status);
        CollectionAssert.AreEqual(new[] { first }, saved);
        CollectionAssert.AreEqual(new[] { "192.0.2.41" }, failures);
    }

    /// <summary>
    /// 驗證只存在單一方向的過期規則仍會被清除。
    /// </summary>
    [TestMethod]
    public void Reconcile_DirectionalOrphan_RemovesStaleAddress()
    {
        FakeFirewallPolicy firewall = new([]) { AnyDirectionAddresses = ["192.0.2.50"] };
        FirewallStateReconciler reconciler = new(
            firewall, () => [], _ => Assert.Fail(), (_, _, _, _) => { }, (_, exception) => Assert.Fail(exception.Message));

        reconciler.Reconcile();

        CollectionAssert.AreEqual(new[] { "192.0.2.50" }, firewall.RemovedAddresses);
    }

    private sealed class FakeFirewallPolicy(IEnumerable<string> initialAddresses) : IFirewallPolicy
    {
        private readonly HashSet<string> addresses = new(initialAddresses, StringComparer.Ordinal);
        internal bool FailBlock { get; init; }
        internal bool FailAfterFirstBlock { get; init; }
        internal IReadOnlyCollection<string>? AnyDirectionAddresses { get; init; }
        internal List<string> RemovedAddresses { get; } = [];
        internal int StateReadCount { get; private set; }
        internal int BatchBlockCount { get; private set; }
        internal int BatchRemoveCount { get; private set; }

        public void Block(string ipAddress)
        {
            if (FailBlock)
                throw new InvalidOperationException("expected");
            addresses.Add(ipAddress);
        }

        public void BatchBlock(IReadOnlyCollection<string> ipAddresses)
        {
            BatchBlockCount++;
            if (FailBlock)
                throw new InvalidOperationException("expected");
            int applied = 0;
            foreach (string ip in ipAddresses)
            {
                addresses.Add(ip);
                applied++;
                if (FailAfterFirstBlock && applied == 1)
                    throw new InvalidOperationException("expected partial failure");
            }
        }

        public bool IsLocked(string ipAddress) => addresses.Contains(ipAddress);
        public HashSet<string> FilterLockedIps(IEnumerable<string> ipAddresses)
        {
            HashSet<string> result = [];
            foreach (string ip in ipAddresses)
            {
                if (addresses.Contains(ip))
                    result.Add(ip);
            }
            return result;
        }
        public IReadOnlyCollection<string> GetBlockedAddresses() => addresses;
        public FirewallBlockState GetBlockState()
        {
            StateReadCount++;
            return new(addresses, AnyDirectionAddresses ?? addresses);
        }
        public void RemoveIpAddressFromBlockList(string ipAddress) => addresses.Remove(ipAddress);
        public void BatchRemove(IReadOnlyCollection<string> ipAddresses)
        {
            BatchRemoveCount++;
            foreach (string ip in ipAddresses)
            {
                addresses.Remove(ip);
                RemovedAddresses.Add(ip);
            }
        }
        public void CompactBlockRules(IEnumerable<string>? safeNetworks = null) { }
        public List<FirewallInboundRuleDefinition> ReconciledRules { get; } = [];
        public void ReconcileInboundAllowRules(IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules, Action<string, string, string, string?>? auditRecorder = null)
        {
            ReconciledRules.Clear();
            ReconciledRules.AddRange(targetRules);
        }
        public void RemoveAllInboundAllowRules(Action<string, string, string, string?>? auditRecorder = null)
        {
            ReconciledRules.Clear();
        }
    }
}
