using System;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.IntrusionDetection.Service;
/// <summary>
/// Decouples synchronous Agent callbacks from protection work through a bounded, single-reader channel.
/// </summary>
internal sealed class SecurityEventPipeline
{
    internal const string MeterName = "IDDSCommunity.SecurityEvents";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Accepted = Meter.CreateCounter<long>("iddscommunity.security_events.accepted");
    private static readonly Counter<long> Processed = Meter.CreateCounter<long>("iddscommunity.security_events.processed");
    private static readonly Counter<long> Rejected = Meter.CreateCounter<long>("iddscommunity.security_events.rejected");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("iddscommunity.security_events.failures");
    private static readonly Counter<long> Recovered = Meter.CreateCounter<long>("iddscommunity.security_events.recovered");
    private static readonly UpDownCounter<long> Queued = Meter.CreateUpDownCounter<long>("iddscommunity.security_events.queued");
    private static readonly Histogram<double> QueueDelay = Meter.CreateHistogram<double>("iddscommunity.security_events.queue_delay", "ms");
    private static readonly Histogram<double> ProcessingDuration = Meter.CreateHistogram<double>("iddscommunity.security_events.processing_duration", "ms");
    private static readonly Histogram<double> RecoveryAge = Meter.CreateHistogram<double>("iddscommunity.security_events.recovery_age", "s");
    private static readonly Counter<long> DrainTimeouts = Meter.CreateCounter<long>("iddscommunity.security_events.drain_timeouts");
    private readonly Channel<SecurityEventEnvelope> channel;
    private readonly Action<object, INotificationEventArgs> process;
    private readonly Action<Exception> reportFailure;
    private readonly SecurityEventInbox inbox;
    private readonly Func<string, object?> resolveAgent;
    private long queueDepth;
    private int accepting = 1;
    /// <summary>
    /// Initializes and starts one bounded security-event consumer.
    /// </summary>
    /// <param name="capacity">The maximum number of queued security events.</param>
    /// <param name="process">The synchronous protection operation executed by the dedicated consumer.</param>
    /// <param name="reportFailure">The isolated failure observer.</param>
    internal SecurityEventPipeline(
        int capacity,
        Action<object, INotificationEventArgs> process,
        Action<Exception> reportFailure,
        SecurityEventInbox inbox,
        Func<string, object?> resolveAgent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(reportFailure);
        ArgumentNullException.ThrowIfNull(inbox);
        ArgumentNullException.ThrowIfNull(resolveAgent);
        this.process = process;
        this.reportFailure = reportFailure;
        this.inbox = inbox;
        this.resolveAgent = resolveAgent;
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
        Guid id = inbox.Add(GetAgentName(sender), snapshot);
        Interlocked.Increment(ref queueDepth);
        Queued.Add(1);
        if (!channel.Writer.TryWrite(new SecurityEventEnvelope(id, sender, snapshot, Stopwatch.GetTimestamp())))
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            inbox.RemovePending(id);
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
        NotificationEventArgs snapshot = CreateSnapshot(eventArgs);
        Guid id = inbox.Add(GetAgentName(sender), snapshot);
        Interlocked.Increment(ref queueDepth);
        Queued.Add(1);
        SecurityEventEnvelope envelope = new(id, sender, snapshot, Stopwatch.GetTimestamp());
        if (channel.Writer.TryWrite(envelope))
        {
            Accepted.Add(1);
            return true;
        }
        try
        {
            channel.Writer.WriteAsync(envelope).AsTask().GetAwaiter().GetResult();
            Accepted.Add(1);
            return true;
        }
        catch (ChannelClosedException)
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            inbox.RemovePending(id);
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
    /// Waits for accepted work to drain and records timeout evidence.
    /// </summary>
    /// <param name="timeout">The maximum graceful-drain duration.</param>
    internal void Drain(TimeSpan timeout)
    {
        try
        {
            Completion.WaitAsync(timeout).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            DrainTimeouts.Add(1);
            throw;
        }
    }
    /// <summary>
    /// Requeues durable unfinished events whose Agents are loaded in the current runtime.
    /// </summary>
    /// <param name="maximumCount">The maximum number of persisted events to recover during startup.</param>
    internal void RecoverPending(int maximumCount)
    {
        foreach (SecurityEventInboxItem item in inbox.ReadPending(maximumCount))
        {
            object? sender = resolveAgent(item.AgentName);
            if (sender is null)
                continue;
            Interlocked.Increment(ref queueDepth);
            Queued.Add(1);
            try
            {
                channel.Writer.WriteAsync(new SecurityEventEnvelope(item.Id, sender, item.EventArgs, Stopwatch.GetTimestamp())).AsTask().GetAwaiter().GetResult();
                Recovered.Add(1);
                RecoveryAge.Record(Math.Max(0, (DateTimeOffset.UtcNow - item.ReceivedUtc).TotalSeconds));
            }
            catch (ChannelClosedException)
            {
                Interlocked.Decrement(ref queueDepth);
                Queued.Add(-1);
                return;
            }
        }
    }
    /// <summary>
    /// Processes accepted events sequentially and isolates one failure from later work.
    /// </summary>
    /// <returns>表示非同步執行的 Task。</returns>
    private async Task ConsumeAsync()
    {
        await foreach (SecurityEventEnvelope item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            Interlocked.Decrement(ref queueDepth);
            Queued.Add(-1);
            long processingStarted = 0;
            try
            {
                QueueDelay.Record(Stopwatch.GetElapsedTime(item.EnqueuedTimestamp).TotalMilliseconds);
                processingStarted = Stopwatch.GetTimestamp();
                inbox.MarkProcessing(item.Id);
                process(item.Sender, item.EventArgs);
                inbox.MarkCompleted(item.Id);
                Processed.Add(1);
            }
            catch (Exception ex)
            {
                Failures.Add(1);
                try
                {
                    inbox.MarkFailed(item.Id, ex);
                }
                catch (Exception inboxException)
                {
                    reportFailure(inboxException);
                }
                try
                {
                    reportFailure(ex);
                }
                catch (Exception)
                {
                    // Failure observers must never terminate the protection consumer.
                }
            }
            finally
            {
                if (processingStarted != 0)
                    ProcessingDuration.Record(Stopwatch.GetElapsedTime(processingStarted).TotalMilliseconds);
            }
        }
    }
    /// <summary>
    /// Copies mutable plug-in event arguments before crossing the asynchronous boundary.
    /// </summary>
    /// <param name="eventArgs">The plug-in supplied event arguments.</param>
    /// <returns>獨立擁有之事件快照物件。</returns>
    private static NotificationEventArgs CreateSnapshot(INotificationEventArgs eventArgs) => new()
    {
        IpAddress = eventArgs.IpAddress,
        CreateDate = eventArgs.CreateDate,
        EventId = eventArgs.EventId,
        EventMessage = eventArgs.EventMessage
    };
    /// <summary>
    /// Obtains the stable Agent identity required to resolve a durable event after restart.
    /// </summary>
    /// <param name="sender">The event-producing Agent instance.</param>
    /// <returns>已設定之 Agent 名稱或決定性型別名稱。</returns>
    private static string GetAgentName(object sender) => sender is IAgentPlugin plugin
        ? plugin.Configuration.AgentName
        : sender.GetType().FullName ?? sender.GetType().Name;

    private sealed record SecurityEventEnvelope(Guid Id, object Sender, INotificationEventArgs EventArgs, long EnqueuedTimestamp);
}
