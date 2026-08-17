using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared;

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
        IDDSCommunityMetrics.RecordReceived();
        if (channel.Writer.TryWrite(new RawPacketEventArgs(packet)))
            return true;
        Interlocked.Increment(ref droppedCount);
        IDDSCommunityMetrics.RecordDropped();
        return false;
    }
    /// <summary>
    /// Completes the producer side and lets the consumer drain queued packets.
    /// </summary>
    internal void Complete() => channel.Writer.TryComplete();
    /// <summary>
    /// Dispatches queued packets sequentially on a worker task.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    private async Task DispatchAsync()
    {
        await foreach (RawPacketEventArgs packet in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                dispatch(packet);
            }
            catch (Exception exception)
            {
                // 隔離消費者例外，避免單一故障的委派終止整個分派迴圈並讓後續封包被靜默丟棄。
                System.Diagnostics.Trace.TraceError("Packet dispatch callback threw and was isolated: {0}", exception.Message);
            }
            Interlocked.Increment(ref dispatchedCount);
            IDDSCommunityMetrics.RecordDispatched();
        }
    }
}
