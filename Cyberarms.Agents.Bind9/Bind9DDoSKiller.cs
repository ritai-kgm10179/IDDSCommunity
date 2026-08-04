using System;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace Cyberarms.Agents.Bind9;

public class Bind9DDoSKiller : AgentPlugin
{

    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_BIND_RECURSION_DENIED = @"<QueryList>
                  <Query Id=""3"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=3) and
                        *[System/Provider/@Name=""named""]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public Bind9DDoSKiller() => Configuration = new Bind9DDoSConfig();



    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_BIND_RECURSION_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(Watcher_EventRecordWritten);
        watcher.Enabled = true;
    }

    /// <summary>
    /// Resume from Pause
    /// </summary>
    protected override void OnContinueAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = true;
        }
    }

    /// <summary>
    /// Pause the agent
    /// </summary>
    protected override void OnPauseAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = false;
        }
    }

    /// <summary>
    /// Stop the agent
    /// </summary>
    protected override void OnStopAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = false;
            watcher.Dispose();
        }
        watcher = null;
        query = null;
    }

    private void Watcher_EventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            // (new System.Collections.Generic.Mscorlib_CollectionDebugView<System.Diagnostics.Eventing.Reader.EventProperty>(e.EventRecord.Properties)).Items[0]
            if (e.EventRecord is null)
            {
                return;
            }

            foreach (EventProperty prop in e.EventRecord.Properties)
            {
                string? propertyValue = prop.Value?.ToString();
                if (propertyValue?.Contains("CLIENT:", StringComparison.Ordinal) == true)
                {
                    int start = propertyValue.IndexOf("CLIENT:", StringComparison.Ordinal) + 7;
                    int end = propertyValue.LastIndexOf(']');
                    if (end <= start)
                    {
                        continue;
                    }

                    string ipAddress = propertyValue[start..end];
                    NotificationEventArgs args = new()
                    {
                        CreateDate = e.EventRecord.TimeCreated ?? DateTime.Now,
                        EventId = e.EventRecord.Id,
                        IpAddress = ipAddress
                    };
                    OnAttackDetected(this, args);
                }
            }

        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("Cyberarms.Agents.Bind9.Bind9DDoSKiller", ex.Message);
        }
    }

}
