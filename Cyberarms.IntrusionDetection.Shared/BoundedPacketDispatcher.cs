using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Cyberarms.IntrusionDetection.Shared;

internal sealed class BoundedPacketDispatcher
{
    private readonly Channel<RawPacketEventArgs> channel;
    private readonly Action<RawPacketEventArgs> dispatch;
    private long receivedCount;
    private long dispatchedCount;
    private long droppedCount;

    /// <summary>
    /// Initializes a bounded single-reader packet dispatcher.
    /// </summary>
    /// <param name="capacity">The maximum number of queued packets.</param>
    /// <param name="dispatch">The packet consumer callback.</param>
    internal BoundedPacketDispatcher(int capacity, Action<RawPacketEventArgs> dispatch)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentNullException.ThrowIfNull(dispatch);
        this.dispatch = dispatch;
        channel = Channel.CreateBounded<RawPacketEventArgs>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
        Completion = DispatchAsync();
    }

    internal long ReceivedCount => Interlocked.Read(ref receivedCount);

    internal long DispatchedCount => Interlocked.Read(ref dispatchedCount);

    internal long DroppedCount => Interlocked.Read(ref droppedCount);

    internal Task Completion { get; }

    /// <summary>
    /// Attempts to enqueue a packet without blocking the socket receive loop.
    /// </summary>
    /// <param name="packet">The packet owned by the dispatcher.</param>
    /// <returns><see langword="true"/> when queued; otherwise, <see langword="false"/> when capacity is exhausted.</returns>
    internal bool TryEnqueue(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        Interlocked.Increment(ref receivedCount);
        CyberarmsMetrics.RecordReceived();
        if (channel.Writer.TryWrite(new RawPacketEventArgs(packet)))
            return true;
        Interlocked.Increment(ref droppedCount);
        CyberarmsMetrics.RecordDropped();
        return false;
    }

    /// <summary>
    /// Completes the producer side and lets the consumer drain queued packets.
    /// </summary>
    internal void Complete() => channel.Writer.TryComplete();

    /// <summary>
    /// Dispatches queued packets sequentially on a worker task.
    /// </summary>
    /// <returns>A task that completes when the channel is drained.</returns>
    private async Task DispatchAsync()
    {
        await foreach (RawPacketEventArgs packet in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            dispatch(packet);
            Interlocked.Increment(ref dispatchedCount);
            CyberarmsMetrics.RecordDispatched();
        }
    }
}
