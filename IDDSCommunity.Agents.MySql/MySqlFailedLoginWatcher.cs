using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.MySql;

public sealed class MySqlFailedLoginWatcher : AgentPlugin, IExtendedInformation
{
    internal const string EventLogQuery = @"<QueryList>
                  <Query Id=""0"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[Provider[@Name='MySQL' or @Name='MariaDB']]]
                    </Select>
                  </Query>
                </QueryList>";

    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName, EventLogQuery);
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += WatcherEventRecordWritten;
        watcher.Enabled = true;
    }

    protected override void OnContinueAgent() => SetWatcherEnabled(true);
    protected override void OnPauseAgent() => SetWatcherEnabled(false);

    protected override void OnStopAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = false;
            watcher.EventRecordWritten -= WatcherEventRecordWritten;
            watcher.Dispose();
        }
        watcher = null;
        query = null;
    }

    private void SetWatcherEnabled(bool enabled)
    {
        if (watcher is not null) watcher.Enabled = enabled;
    }

    private void WatcherEventRecordWritten(object? sender, EventRecordWrittenEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.EventException is not null) throw eventArgs.EventException;
            using EventRecord? record = eventArgs.EventRecord;
            if (record is null) return;

            List<string?> messages = [];
            foreach (EventProperty property in record.Properties) messages.Add(property.Value?.ToString());
            try { messages.Add(record.FormatDescription()); }
            catch (EventLogException exception) { Trace.TraceWarning("Unable to format MySQL/MariaDB event {0}: {1}", record.Id, exception.Message); }

            if (!MySqlMariaDbAuthenticationParser.TryParse(record.ProviderName, messages, out IPAddress address)) return;
            OnAttackDetected(this, new NotificationEventArgs
            {
                CreateDate = record.TimeCreated ?? DateTime.Now,
                EventId = record.Id,
                IpAddress = address.ToString()
            });
        }
        catch (Exception exception)
        {
            EventLog.WriteEntry("IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher", exception.ToString());
        }
    }

    public string DisplayName
    {
        get => "MySQL and MariaDB Security Agent";
        set { }
    }

    public Image Icon { get; set; } = null!;
    public Image SelectedIcon { get; set; } = null!;
    public Image UnselectedIcon { get; set; } = null!;

    public Guid Id => new("{EE4906AD-7242-4940-A3B0-81B4E3F16B71}");
}
