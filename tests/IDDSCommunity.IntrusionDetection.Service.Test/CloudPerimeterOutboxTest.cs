using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;
using IDDSCommunity.IntrusionDetection.Service.CloudPerimeter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證雲端待送匣重試持久性與同一 IP 處置順序。
/// </summary>
[TestClass]
public sealed class CloudPerimeterOutboxTest
{
    /// <summary>
    /// 舊封鎖完成不會刪除同一 IP 後來排入的解鎖。
    /// </summary>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    public async Task InFlightBlockDoesNotEraseNewerUnblock()
    {
        using var fixture = new Fixture();
        var fake = new FakeProvider { HoldBlock = true };
        using var service = new CloudPerimeterService(new CloudPerimeterSettings { EnableCloudPerimeter = true }, fixture.Database, fake);
        Assert.IsTrue(await service.NotifyBlockAsync("8.8.8.8", "test"));
        await fake.BlockStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsTrue(await service.NotifyUnblockAsync("8.8.8.8"));
        fake.ReleaseBlock.TrySetResult();
        await fake.Unblocked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(1, fake.BlockCalls);
        Assert.AreEqual(1, fake.UnblockCalls);
    }

    /// <summary>
    /// 失敗要求在重啟後由新工作者重試，且不需重新排入。
    /// </summary>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    public async Task FailedDeliverySurvivesRestart()
    {
        using var fixture = new Fixture();
        var failed = new FakeProvider { FailBlock = true };
        using (var service = new CloudPerimeterService(new CloudPerimeterSettings { EnableCloudPerimeter = true }, fixture.Database, failed))
        {
            await service.NotifyBlockAsync("8.8.8.8", "test");
            await failed.BlockStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        Assert.AreEqual(1, Convert.ToInt32(fixture.Database.ExecuteScalar("SELECT COUNT(*) FROM CloudPerimeterOutbox")));
        var recovered = new FakeProvider();
        using var restarted = new CloudPerimeterService(new CloudPerimeterSettings { EnableCloudPerimeter = true }, fixture.Database, recovered);
        await recovered.BlockStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        for (int i = 0; i < 100 && Convert.ToInt32(fixture.Database.ExecuteScalar("SELECT COUNT(*) FROM CloudPerimeterOutbox")) != 0; i++) await Task.Delay(20);
        Assert.AreEqual(0, Convert.ToInt32(fixture.Database.ExecuteScalar("SELECT COUNT(*) FROM CloudPerimeterOutbox")));
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Database Database = new();
        private readonly string directory = Path.Combine(Path.GetTempPath(), "IDDS-Outbox-Test-" + Guid.NewGuid().ToString("N"));
        internal Fixture() { Directory.CreateDirectory(directory); Database.Configure(directory, "outbox.db"); }
        public void Dispose() { Database.Close(); Directory.Delete(directory, true); }
    }

    private sealed class FakeProvider : ICloudPerimeterProvider
    {
        public CloudPerimeterType ProviderType => CloudPerimeterType.GenericWebhook;
        public string Name => "Test";
        internal bool HoldBlock;
        internal bool FailBlock;
        internal int BlockCalls;
        internal int UnblockCalls;
        internal readonly TaskCompletionSource BlockStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource ReleaseBlock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Unblocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<bool> BlockIpAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref BlockCalls);
            BlockStarted.TrySetResult();
            if (HoldBlock) await ReleaseBlock.Task.WaitAsync(cancellationToken);
            return !FailBlock;
        }
        public Task<bool> UnblockIpAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref UnblockCalls);
            Unblocked.TrySetResult();
            return Task.FromResult(true);
        }
        public Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default) => Task.FromResult((true, "test"));
    }
}