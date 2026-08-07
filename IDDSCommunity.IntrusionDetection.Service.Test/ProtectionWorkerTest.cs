using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class ProtectionWorkerTest
{
    /// <summary>
    /// Verifies that the worker starts and stops the runtime exactly once.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task StartAndStopAsync_ControlsRuntimeExactlyOnce()
    {
        FakeRuntime runtime = new();
        ProtectionWorker worker = new(runtime);

        await worker.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.AreEqual(1, runtime.StartCount);
        Assert.AreEqual(1, runtime.StopCount);
    }

    /// <summary>
    /// Verifies that a runtime startup failure propagates to the Generic Host.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task StartAsync_WhenRuntimeFails_PropagatesFailure()
    {
        InvalidOperationException expected = new("startup failed");
        FakeRuntime runtime = new() { StartException = expected };
        ProtectionWorker worker = new(runtime);

        InvalidOperationException actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => worker.StartAsync(CancellationToken.None)).ConfigureAwait(false);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, runtime.StartCount);
        Assert.AreEqual(0, runtime.StopCount);
    }

    private sealed class FakeRuntime : IIntrusionDetectionRuntime
    {
        internal int StartCount { get; private set; }

        internal int StopCount { get; private set; }

        internal Exception? StartException { get; init; }

        /// <summary>
        /// Records runtime startup and optionally throws the configured failure.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation of startup.</param>
        /// <returns>啟動成功時完成的 Task。</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
            if (StartException is not null)
                throw StartException;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Records runtime shutdown.
        /// </summary>
        /// <param name="cancellationToken">Signals cancellation of shutdown.</param>
        /// <returns>已完成之 Task。</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            return Task.CompletedTask;
        }
    }
}
