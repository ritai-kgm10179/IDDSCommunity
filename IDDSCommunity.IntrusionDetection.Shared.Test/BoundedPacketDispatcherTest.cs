using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class BoundedPacketDispatcherTest
{
    /// <summary>
    /// Verifies that queue capacity bounds memory and drops a new packet instead of blocking its producer.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task TryEnqueue_WhenQueueIsFull_DropsNewestPacket()
    {
        TaskCompletionSource firstDispatchStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseFirstDispatch = new(false);
        BoundedPacketDispatcher dispatcher = new(1, _ =>
        {
            firstDispatchStarted.TrySetResult();
            releaseFirstDispatch.Wait(TimeSpan.FromSeconds(5));
        });

        Assert.IsTrue(dispatcher.TryEnqueue([1]));
        await firstDispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.IsTrue(dispatcher.TryEnqueue([2]));
        Assert.IsFalse(dispatcher.TryEnqueue([3]));
        releaseFirstDispatch.Set();
        dispatcher.Complete();
        await dispatcher.Completion.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.AreEqual(3L, dispatcher.ReceivedCount);
        Assert.AreEqual(2L, dispatcher.DispatchedCount);
        Assert.AreEqual(1L, dispatcher.DroppedCount);
    }
    /// <summary>
    /// Verifies that every accepted packet is delivered in FIFO order.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task DispatchAsync_PreservesAcceptedPacketOrder()
    {
        System.Collections.Generic.List<byte> values = [];
        BoundedPacketDispatcher dispatcher = new(4, packet => values.Add(packet.Packet[0]));

        Assert.IsTrue(dispatcher.TryEnqueue([1]));
        Assert.IsTrue(dispatcher.TryEnqueue([2]));
        Assert.IsTrue(dispatcher.TryEnqueue([3]));
        dispatcher.Complete();
        await dispatcher.Completion.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, values);
    }
}
