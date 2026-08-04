using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Api.Plugin;

namespace Cyberarms.IntrusionDetection.Service;

/// <summary>
/// Decouples synchronous Agent callbacks from protection work through a bounded, single-reader channel.
/// </summary>
internal sealed class SecurityEventPipeline
{
    internal const string MeterName = "Cyberarms.SecurityEvents";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Accepted = Meter.CreateCounter<long>("cyberarms.security_events.accepted");
    private static readonly Counter<long> Processed = Meter.CreateCounter<long>("cyberarms.security_events.processed");
    private static readonly Counter<long> Rejected = Meter.CreateCounter<long>("cyberarms.security_events.rejected");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("cyberarms.security_events.failures");
    private static readonly UpDownCounter<long> Queued = Meter.CreateUpDownCounter<long>("cyberarms.security_events.queued");
    private readonly Channel<SecurityEventEnvelope> channel;
    private readonly Action<object, INotificationEventArgs> process;
    private readonly Action<Exception> reportFailure;
    private long queueDepth;
    private int accepting = 1;

    /// <summary>
    /// Initializes and starts one bounded security-event consumer.
    /// </summary>
    /// <param name="capacity">The maximum number of queued security events.</param>
    /// <param name="process">The synchronous protection operation executed by the dedicated consumer.</param>
    /// <param name="reportFailure">The isolated failure observer.</param>
    internal SecurityEventPipeline(int capacity, Action<object, INotificationEventArgs> process, Action<Exception> reportFailure)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(reportFailure);
        this.process = process;
        this.reportFailure = reportFailure;
        channel = Channel.CreateBounded<SecurityEventEnvelope>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        Completion = ConsumeAsync();
    }

    /// <summary>
    /// Gets the number of security events currently waiting for protection processing.
    /// </summary>
    internal long QueueDepth => Interlocked.Read(ref queueDepth);

    /// <summary>
    /// Gets the task that completes after the writer closes and the queue drains.
    /// </summary>
    internal Task Completion { get; }

    /// <summary>
    /// Attempts to publish without blocking an Event Log or packet-capture callback.
    /// </summary>
    /// <param name="sender">The reporting Agent.</param>
    /// <param name="eventArgs">The immutable-by-contract detection information.</param>
    /// <returns><see langword="true"/> when accepted; otherwise, <see langword="false"/> when stopping or saturated.</returns>
    internal bool TryPublish(object sender, INotificationEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);
        NotificationEventArgs snapshot = CreateSnapshot(eventArgs);
        if (Volatile.Read(ref accepting) == 0)
        {
            Rejected.Add(1);
            return false;
        }
        Interlocked.Increment(ref queueDepth);
        Queued.Add(1);
        if (!channel.Writer.TryWrite(new SecurityEventEnvelope(sender, snapshot)))
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            Rejected.Add(1);
            return false;
        }
        Accepted.Add(1);
        return true;
    }

    /// <summary>
    /// Publishes with bounded-channel backpressure, waiting only while the runtime is accepting work.
    /// </summary>
    /// <param name="sender">The reporting Agent.</param>
    /// <param name="eventArgs">The detection information copied before the wait.</param>
    /// <returns><see langword="true"/> when accepted; otherwise, <see langword="false"/> after shutdown closes the writer.</returns>
    internal bool Publish(object sender, INotificationEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);
        if (Volatile.Read(ref accepting) == 0)
        {
            Rejected.Add(1);
            return false;
        }
        Interlocked.Increment(ref queueDepth);
        Queued.Add(1);
        try
        {
            channel.Writer.WriteAsync(new SecurityEventEnvelope(sender, CreateSnapshot(eventArgs))).AsTask().GetAwaiter().GetResult();
            Accepted.Add(1);
            return true;
        }
        catch (ChannelClosedException)
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            Rejected.Add(1);
            return false;
        }
    }

    /// <summary>
    /// Stops accepting events and allows the single consumer to drain accepted work.
    /// </summary>
    internal void Complete()
    {
        Interlocked.Exchange(ref accepting, 0);
        channel.Writer.TryComplete();
    }

    /// <summary>
    /// Processes accepted events sequentially and isolates one failure from later work.
    /// </summary>
    /// <returns>A task representing the consumer lifetime.</returns>
    private async Task ConsumeAsync()
    {
        await foreach (SecurityEventEnvelope item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            try
            {
                process(item.Sender, item.EventArgs);
                Processed.Add(1);
            }
            catch (Exception ex)
            {
                Failures.Add(1);
                try
                {
                    reportFailure(ex);
                }
                catch (Exception)
                {
                    // Failure observers must never terminate the protection consumer.
                }
            }
        }
    }

    /// <summary>
    /// Copies mutable plug-in event arguments before crossing the asynchronous boundary.
    /// </summary>
    /// <param name="eventArgs">The plug-in supplied event arguments.</param>
    /// <returns>An independently owned event snapshot.</returns>
    private static NotificationEventArgs CreateSnapshot(INotificationEventArgs eventArgs) => new()
    {
        IpAddress = eventArgs.IpAddress,
        CreateDate = eventArgs.CreateDate,
        EventId = eventArgs.EventId,
        EventMessage = eventArgs.EventMessage
    };

    private sealed record SecurityEventEnvelope(object Sender, INotificationEventArgs EventArgs);
}
