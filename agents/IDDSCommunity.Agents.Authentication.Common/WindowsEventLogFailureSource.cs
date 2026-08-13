using System;
using System.Diagnostics.Eventing.Reader;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class WindowsEventLogFailureSource : IAuthenticationEventSource
{
    private readonly string channel;
    private readonly string query;
    private readonly Func<EventRecord, AuthenticationFailureEvent?> parser;
    private EventLogWatcher? watcher;

    public WindowsEventLogFailureSource(string channel, string query, Func<EventRecord, AuthenticationFailureEvent?> parser)
    {
        this.channel = channel;
        this.query = query;
        this.parser = parser;
    }

    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    public event Action<Exception>? Error;

    public void Start()
    {
        if (watcher is not null) return;
        watcher = new EventLogWatcher(new EventLogQuery(channel, PathType.LogName, query), null, false);
        watcher.EventRecordWritten += OnEvent;
        watcher.Enabled = true;
    }

    public void Pause() { if (watcher is not null) watcher.Enabled = false; }
    public void Resume() { if (watcher is not null) watcher.Enabled = true; }
    public void Stop()
    {
        if (watcher is null) return;
        watcher.Enabled = false;
        watcher.EventRecordWritten -= OnEvent;
        watcher.Dispose();
        watcher = null;
    }
    public void Dispose() => Stop();

    private void OnEvent(object? sender, EventRecordWrittenEventArgs args)
    {
        if (args.EventException is not null) { Error?.Invoke(args.EventException); return; }
        using EventRecord? record = args.EventRecord;
        if (record is null) return;
        try
        {
            AuthenticationFailureEvent? failure = parser(record);
            if (failure is not null) EventReceived?.Invoke(this, failure);
        }
        catch (Exception exception) { Error?.Invoke(exception); }
    }
}
