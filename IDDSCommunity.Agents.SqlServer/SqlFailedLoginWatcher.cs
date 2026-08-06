using System;
using System.Text.RegularExpressions;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.Agents.SqlServer;

//  [PluginAttribute("Intrusion Detection Base Windows Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public partial class SqlFailedLoginWatcher : AgentPlugin, IExtendedInformation
{


    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_SQL_SERVER_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""18456"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=18456) and
                        TimeCreated[timediff(@SystemTime) &lt;= 864000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public SqlFailedLoginWatcher()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_SQL_SERVER_LOGIN_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(Watcher_EventRecordWritten);
        watcher.Enabled = true;
    }

    /// <summary>
    /// Resume from Pause
    /// </summary>
    protected override void OnContinueAgent() => SetWatcherEnabled(true);

    /// <summary>
    /// Pause the agent
    /// </summary>
    protected override void OnPauseAgent() => SetWatcherEnabled(false);

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

    /// <summary>
    /// Sets watcher enabled.
    /// </summary>
    /// <param name="enabled">The enabled value.</param>

    private void SetWatcherEnabled(bool enabled)
    {
        if (watcher is not null)
        {
            watcher.Enabled = enabled;
        }
    }

    /// <summary>
    /// Handles the event record written event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
                if (propertyValue is not null && MyRegex().IsMatch(propertyValue))
                {
                    Match ipAddress = MyRegex().Match(propertyValue);
                    NotificationEventArgs args = new()
                    {
                        CreateDate = e.EventRecord.TimeCreated ?? DateTime.Now,
                        EventId = e.EventRecord.Id,
                        IpAddress = ipAddress.Value
                    };
                    if (System.Net.IPAddress.TryParse(ipAddress.Value, out System.Net.IPAddress? probe))
                    {
                        if (probe.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || probe.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            OnAttackDetected(this, args);
                        }
                    }
                }

            }

        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher", ex.Message);
        }
    }

    public string DisplayName
    {
        get => "SQL Server Security Agent";
        set
        {

        }
    }

    public Image? Icon { get; set; }
    public Image? SelectedIcon { get; set; }
    public Image? UnselectedIcon { get; set; }



    public Guid Id => new("{0F470A49-594D-4895-ADE1-46B48B9B8A58}");

    /// <summary>
    /// Executes the my regex operation.
    /// </summary>
    /// <returns>The my regex result.</returns>

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
