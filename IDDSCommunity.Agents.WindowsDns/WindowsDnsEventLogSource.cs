using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace IDDSCommunity.Agents.WindowsDns;

internal sealed class WindowsDnsEventLogSource : IWindowsDnsEventSource
{
    internal const string AnalyticalChannel = "Microsoft-Windows-DNSServer/Analytical";
    internal const string AuditChannel = "Microsoft-Windows-DNSServer/Audit";
    private EventLogWatcher? analyticalWatcher;
    private EventLogWatcher? auditWatcher;

    public event EventHandler<DnsEventRecord>? EventReceived;
    public event Action<Exception>? Error;

    /// <summary>
    /// Starts live subscriptions to the documented DNS analytical and audit channels.
    /// </summary>
    public void Start()
    {
        if (analyticalWatcher is not null || auditWatcher is not null)
            return;
        analyticalWatcher = CreateWatcher(AnalyticalChannel, "*[System[(EventID=257 or EventID=258 or EventID=263 or EventID=266 or EventID=270)]]");
        auditWatcher = CreateWatcher(AuditChannel, "*[System[(EventID=519 or EventID=520)]]");
        analyticalWatcher.Enabled = true;
        auditWatcher.Enabled = true;
    }

    /// <summary>
    /// Temporarily disables both live subscriptions.
    /// </summary>
    public void Pause() => SetEnabled(false);

    /// <summary>
    /// Resumes both live subscriptions.
    /// </summary>
    public void Resume() => SetEnabled(true);

    /// <summary>
    /// Stops and releases both live subscriptions.
    /// </summary>
    public void Stop()
    {
        DisposeWatcher(ref analyticalWatcher);
        DisposeWatcher(ref auditWatcher);
    }

    /// <summary>
    /// Releases event subscriptions and operating-system handles.
    /// </summary>
    public void Dispose() => Stop();

    private EventLogWatcher CreateWatcher(string channel, string queryText)
    {
        EventLogQuery query = new(channel, PathType.LogName, queryText)
        {
            ReverseDirection = false,
            TolerateQueryErrors = false
        };
        EventLogWatcher watcher = new(query, bookmark: null, readExistingEvents: false);
        watcher.EventRecordWritten += OnEventRecordWritten;
        return watcher;
    }

    private void OnEventRecordWritten(object? sender, EventRecordWrittenEventArgs args)
    {
        if (args.EventException is not null)
        {
            Error?.Invoke(new InvalidOperationException(DnsStrings.Get("Windows DNS event subscription failed."), args.EventException));
            return;
        }
        using EventRecord? eventRecord = args.EventRecord;
        if (eventRecord is null)
            return;
        IReadOnlyList<object?> values = eventRecord.Properties.Select(property => property.Value).ToList();
        DateTimeOffset occurredAt = eventRecord.TimeCreated is DateTime time ? new DateTimeOffset(time) : DateTimeOffset.UtcNow;
        if (WindowsDnsEventParser.TryParse(eventRecord.Id, values, occurredAt, out DnsEventRecord? record) && record is not null)
            EventReceived?.Invoke(this, record);
        else
            WindowsDnsMetrics.RecordParseFailure();
    }

    private void SetEnabled(bool enabled)
    {
        if (analyticalWatcher is not null)
            analyticalWatcher.Enabled = enabled;
        if (auditWatcher is not null)
            auditWatcher.Enabled = enabled;
    }

    private void DisposeWatcher(ref EventLogWatcher? watcher)
    {
        if (watcher is null)
            return;
        watcher.Enabled = false;
        watcher.EventRecordWritten -= OnEventRecordWritten;
        watcher.Dispose();
        watcher = null;
    }
}
