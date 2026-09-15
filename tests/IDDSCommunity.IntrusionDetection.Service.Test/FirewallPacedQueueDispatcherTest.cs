using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class FirewallPacedQueueDispatcherTest
{
    [TestMethod]
    public void Block_SingleAddress_AddsToTrackerAndFlushesToInnerPolicy()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10, MinPacingIntervalMs = 0 });

        Assert.IsFalse(dispatcher.IsLocked("192.0.2.1"));

        dispatcher.Block("192.0.2.1");

        // 記憶體快取應立即可見（微秒級無鎖確認）
        Assert.IsTrue(dispatcher.IsLocked("192.0.2.1"));

        dispatcher.Flush();

        // 經 Flush 後底層 COM 策略必須確認已收到
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.1"));
        Assert.AreEqual(1, dispatcher.TotalBlocksCommitted);
    }

    [TestMethod]
    public void Block_DuplicateAddresses_CoalescesWithoutDuplicateInnerCalls()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 20, MinPacingIntervalMs = 0 });

        for (int i = 0; i < 20; i++)
        {
            dispatcher.Block("192.0.2.100");
        }

        Assert.IsTrue(dispatcher.IsLocked("192.0.2.100"));
        dispatcher.Flush();

        // 應去重聚合，底層只收到 1 次該位址
        Assert.AreEqual(1, inner.BlockedAddresses.Count(a => a == "192.0.2.100"));
        Assert.IsTrue(dispatcher.CoalescedDuplicatesCount > 0);
    }

    [TestMethod]
    public async Task Block_HighConcurrencyBurst_DispatchesInBatches()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions
            {
                MaxBatchSize = 250,
                CoalescingWindowMs = 30,
                MinPacingIntervalMs = 5
            });

        const int threadCount = 20;
        const int itemsPerThread = 50;
        int totalExpected = threadCount * itemsPerThread; // 1,000 IPs

        Task[] tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            int threadIndex = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < itemsPerThread; i++)
                {
                    int index = threadIndex * itemsPerThread + i;
                    string ip = $"10.{(index >> 16) & 255}.{(index >> 8) & 255}.{index & 255}";
                    dispatcher.Block(ip);
                    Assert.IsTrue(dispatcher.IsLocked(ip));
                }
            });
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        dispatcher.Flush();

        Assert.AreEqual(totalExpected, inner.BlockedAddresses.Count);
        // 批次派發次數應遠少於 1,000 次（以 250 筆/批次計，通常在 4~8 批次內完成）
        Assert.IsTrue(dispatcher.TotalBatchesDispatched <= 10);
        Assert.AreEqual(totalExpected, dispatcher.TotalBlocksCommitted);
    }

    [TestMethod]
    public async Task PacingRateLimiter_EnforcesMinimumIntervalBetweenBatches()
    {
        FakeInnerPolicy inner = new();
        const int minIntervalMs = 60;
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions
            {
                MaxBatchSize = 10,
                CoalescingWindowMs = 5,
                MinPacingIntervalMs = minIntervalMs
            });

        // 提交第一批
        for (int i = 0; i < 10; i++)
        {
            dispatcher.Block($"192.0.2.{i + 1}");
        }

        // 短暫延遲後提交第二批，觸發第二個批次
        await Task.Delay(10).ConfigureAwait(false);
        for (int i = 10; i < 20; i++)
        {
            dispatcher.Block($"192.0.2.{i + 1}");
        }

        dispatcher.Flush();

        Assert.IsTrue(inner.BatchTimestamps.Count >= 2);
        if (inner.BatchTimestamps.Count >= 2)
        {
            TimeSpan delta = inner.BatchTimestamps[1] - inner.BatchTimestamps[0];
            Assert.IsTrue(delta.TotalMilliseconds >= minIntervalMs - 15,
                $"Expected pacing interval >= {minIntervalMs}ms, got {delta.TotalMilliseconds}ms");
        }
    }

    [TestMethod]
    public void RemoveIpAddressFromBlockList_CancelsPendingBlock()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 50, MinPacingIntervalMs = 0 });

        // 快速先阻擋再移除（在批次排空前合流）
        dispatcher.Block("192.0.2.77");
        dispatcher.RemoveIpAddressFromBlockList("192.0.2.77");

        Assert.IsFalse(dispatcher.IsLocked("192.0.2.77"));

        dispatcher.Flush();

        // 合流取消後，底層 COM 不應存在該位址
        Assert.IsFalse(inner.BlockedAddresses.Contains("192.0.2.77"));
    }

    [TestMethod]
    public void FilterLockedIps_UsesInMemoryTrackerFastPath()
    {
        FakeInnerPolicy inner = new();
        inner.BlockedAddresses.Add("198.51.100.1");
        inner.BlockedAddresses.Add("198.51.100.2");

        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("198.51.100.3");

        HashSet<string> locked = dispatcher.FilterLockedIps(["198.51.100.1", "198.51.100.2", "198.51.100.3", "198.51.100.4"]);

        Assert.IsTrue(locked.Contains("198.51.100.1"));
        Assert.IsTrue(locked.Contains("198.51.100.2"));
        Assert.IsTrue(locked.Contains("198.51.100.3"));
        Assert.IsFalse(locked.Contains("198.51.100.4"));
    }

    [TestMethod]
    public void SubnetAndCidr_MatchesCorrectly()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("192.0.2.0/24");

        Assert.IsTrue(dispatcher.IsLocked("192.0.2.42"));
        Assert.IsTrue(dispatcher.IsLocked("192.0.2.1"));
        Assert.IsFalse(dispatcher.IsLocked("192.0.3.1"));

        dispatcher.Flush();
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.0/24"));
    }

    [TestMethod]
    public void Subnet_IsLocked_MatchesCidrNotationAndCoalescesDuplicates()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("10.0.0.0/24");

        // 測試 CIDR 格式本身的 IsLocked 比對
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.0/24"));

        // 重複阻擋相同 CIDR 應成功合流去重
        dispatcher.Block("10.0.0.0/24");
        Assert.IsTrue(dispatcher.CoalescedDuplicatesCount >= 1);
    }

    [TestMethod]
    public void Subnet_RemoveSubnet_RemovesCorrectly()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("10.0.0.0/24");
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.1"));

        dispatcher.RemoveIpAddressFromBlockList("10.0.0.0/24");
        Assert.IsFalse(dispatcher.IsLocked("10.0.0.1"));
        Assert.IsFalse(dispatcher.IsLocked("10.0.0.0/24"));
    }

    [TestMethod]
    public void Subnet_CarveOutIndividualIp_ExcludesIpWhileRetainingSubnet()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("10.0.0.0/24");
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.1"));
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.2"));

        // 自網段中單獨剔除 10.0.0.1 (Carve-out)
        dispatcher.RemoveIpAddressFromBlockList("10.0.0.1");

        // 10.0.0.1 應放行，但同網段之 10.0.0.2 仍須處於阻擋中
        Assert.IsFalse(dispatcher.IsLocked("10.0.0.1"));
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.2"));

        // 重新阻擋 10.0.0.1
        dispatcher.Block("10.0.0.1");
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.1"));
    }

    [TestMethod]
    public void Wildcard_BlockAndRemove_OperatesCorrectly()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("*");
        Assert.IsTrue(dispatcher.IsLocked("192.168.1.1"));
        Assert.IsTrue(dispatcher.IsLocked("2001:db8::1"));

        dispatcher.RemoveIpAddressFromBlockList("*");
        Assert.IsFalse(dispatcher.IsLocked("192.168.1.1"));
        Assert.IsFalse(dispatcher.IsLocked("2001:db8::1"));
    }

    [TestMethod]
    public void Ipv6_FormatNormalization_MatchesRegardlessOfCompression()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("2001:0db8::1");

        // 以全展開/含前導零之格式查詢，應精確比對成功
        Assert.IsTrue(dispatcher.IsLocked("2001:db8:0000:0000:0000:0000:0000:0001"));
        Assert.IsTrue(dispatcher.IsLocked("2001:db8::1"));
    }

    [TestMethod]
    public void BatchBlock_DeduplicatesAndFiltersAlreadyLockedAtIngress()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.Block("192.0.2.1");
        Assert.AreEqual(0, dispatcher.CoalescedDuplicatesCount);

        // 提交包含已阻擋與批次內重複位址之清單
        dispatcher.BatchBlock(["192.0.2.1", "192.0.2.2", "192.0.2.2", "192.0.2.3"]);

        // 192.0.2.1（已鎖定）+ 192.0.2.2（批次內重複 1 次）= 2 次合流去重
        Assert.AreEqual(2, dispatcher.CoalescedDuplicatesCount);

        dispatcher.Flush();
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.1"));
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.2"));
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.3"));
    }

    [TestMethod]
    public async Task EmptyFlush_DoesNotTriggerPacingDelayOrIncrementBatchesDispatched()
    {
        FakeInnerPolicy inner = new();
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10, MinPacingIntervalMs = 100 });

        Stopwatch sw = Stopwatch.StartNew();
        await dispatcher.FlushAsync();
        sw.Stop();

        // 佇列為空時，Flush 不應消耗 100ms 之調步間隔，亦不應累計 TotalBatchesDispatched
        Assert.IsTrue(sw.ElapsedMilliseconds < 80, $"Empty flush took {sw.ElapsedMilliseconds}ms, expected < 80ms");
        Assert.AreEqual(0, dispatcher.TotalBatchesDispatched);
    }

    [TestMethod]
    public async Task BatchRemove_Failure_RollsBackTrackerState()
    {
        FakeInnerPolicy inner = new();
        inner.BlockedAddresses.Add("192.0.2.50");

        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10, MinPacingIntervalMs = 0 });

        Assert.IsTrue(dispatcher.IsLocked("192.0.2.50"));

        inner.FailOnBatchRemove = true;
        ValueTask removeTask = dispatcher.BatchRemoveAsync(["192.0.2.50"]);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await removeTask).ConfigureAwait(false);

        // 底層移除失敗後，記憶體快取應回滾，維持阻擋狀態
        Assert.IsTrue(dispatcher.IsLocked("192.0.2.50"));
    }

    [TestMethod]
    public async Task InnerPolicyException_RevertsTrackerAndPropagatesException()
    {
        FakeInnerPolicy inner = new() { FailOnBatchBlock = true };
        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions
            {
                CoalescingWindowMs = 10,
                MinPacingIntervalMs = 0
            });

        ValueTask blockTask = dispatcher.BlockAsync("192.0.2.99");

        // 當底層拋出例外狀況時，await 必須接收到該例外狀況
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await blockTask).ConfigureAwait(false);

        // 且記憶體快取追蹤器必須回滾該位址
        Assert.IsFalse(dispatcher.IsLocked("192.0.2.99"));
    }

    [TestMethod]
    public void Dispose_FlushesPendingQueueCleanly()
    {
        FakeInnerPolicy inner = new();
        FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 100 });

        dispatcher.Block("192.0.2.200");
        dispatcher.Dispose();

        // 處置時應自動排空殘留佇列並處置底層
        Assert.IsTrue(inner.BlockedAddresses.Contains("192.0.2.200"));
        Assert.IsTrue(inner.IsDisposed);
    }

    [TestMethod]
    public void BatchRemove_RemovesMultipleAddresses()
    {
        FakeInnerPolicy inner = new();
        inner.BlockedAddresses.Add("10.0.0.1");
        inner.BlockedAddresses.Add("10.0.0.2");
        inner.BlockedAddresses.Add("10.0.0.3");

        using FirewallPacedQueueDispatcher dispatcher = new(
            inner,
            new TestRuntimeLog(),
            new FirewallPacingOptions { CoalescingWindowMs = 10 });

        dispatcher.BatchRemove(["10.0.0.1", "10.0.0.3"]);

        Assert.IsFalse(dispatcher.IsLocked("10.0.0.1"));
        Assert.IsTrue(dispatcher.IsLocked("10.0.0.2"));
        Assert.IsFalse(dispatcher.IsLocked("10.0.0.3"));

        dispatcher.Flush();

        Assert.IsFalse(inner.BlockedAddresses.Contains("10.0.0.1"));
        Assert.IsTrue(inner.BlockedAddresses.Contains("10.0.0.2"));
        Assert.IsFalse(inner.BlockedAddresses.Contains("10.0.0.3"));
    }

    private sealed class FakeInnerPolicy : IFirewallPolicy, IDisposable
    {
        public HashSet<string> BlockedAddresses { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<DateTime> BatchTimestamps { get; } = [];
        public bool FailOnBatchBlock { get; set; }
        public bool FailOnBatchRemove { get; set; }
        public bool IsDisposed { get; private set; }

        public void Block(string ipAddress)
        {
            if (FailOnBatchBlock) throw new InvalidOperationException("Simulated COM block failure");
            BlockedAddresses.Add(ipAddress);
        }

        public void BatchBlock(IReadOnlyCollection<string> ipAddresses)
        {
            BatchTimestamps.Add(DateTime.UtcNow);
            if (FailOnBatchBlock) throw new InvalidOperationException("Simulated COM batch block failure");
            foreach (string ip in ipAddresses)
            {
                BlockedAddresses.Add(ip);
            }
        }

        public bool IsLocked(string ipAddress) => BlockedAddresses.Contains(ipAddress);

        public HashSet<string> FilterLockedIps(IEnumerable<string> ipAddresses)
        {
            HashSet<string> result = [];
            foreach (string ip in ipAddresses)
            {
                if (BlockedAddresses.Contains(ip))
                    result.Add(ip);
            }
            return result;
        }

        public IReadOnlyCollection<string> GetBlockedAddresses() => BlockedAddresses.ToList();

        public FirewallBlockState GetBlockState() => new(BlockedAddresses.ToList(), BlockedAddresses.ToList());

        public void RemoveIpAddressFromBlockList(string ipAddress) => BlockedAddresses.Remove(ipAddress);

        public void BatchRemove(IReadOnlyCollection<string> ipAddresses)
        {
            if (FailOnBatchRemove) throw new InvalidOperationException("Simulated COM batch remove failure");
            foreach (string ip in ipAddresses)
            {
                BlockedAddresses.Remove(ip);
            }
        }

        public void CompactBlockRules(IEnumerable<string>? safeNetworks = null) { }

        public void ReconcileInboundAllowRules(
            IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules,
            Action<string, string, string, string?>? auditRecorder = null) { }

        public void RemoveAllInboundAllowRules(Action<string, string, string, string?>? auditRecorder = null) { }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class TestRuntimeLog : IRuntimeLog
    {
        public void WriteEntry(string text, System.Diagnostics.EventLogEntryType type, int eventId, short category)
        {
        }
    }
}
