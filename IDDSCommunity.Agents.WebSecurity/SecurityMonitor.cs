using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.Agents.WebSecurity;

//  [PluginAttribute("Intrusion Detection Base Windows Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]

public class WebSecurityAgent : AgentPlugin, IExtendedInformation
{


    private EventLogQuery? query;
    private EventLogWatcher? watcher;
    private const string SEARCH_PATTERN_BEGIN = "[IP = '";
    private const string SEARCH_PATTERN_END = "']";

    internal const string EVENT_LOG_QUERY_IDDSCOMMUNITY_IIS_SECURITY_MONITOR_ACCESS_DENIED = @"<QueryList>
                  <Query Id=""4625"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=4625) and
                        TimeCreated[timediff(@SystemTime) &lt;= 864000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public WebSecurityAgent()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_IDDSCOMMUNITY_IIS_SECURITY_MONITOR_ACCESS_DENIED));
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
                // extract ip address from event log entry
                // format: <clientname> [IP = 'x.x.x.x']
                string? propertyValue = prop.Value?.ToString();
                if (propertyValue?.Contains(SEARCH_PATTERN_BEGIN, StringComparison.Ordinal) == true)
                {
                    int start = propertyValue.IndexOf(SEARCH_PATTERN_BEGIN, StringComparison.Ordinal) + SEARCH_PATTERN_BEGIN.Length;
                    int end = propertyValue.IndexOf(SEARCH_PATTERN_END, start, StringComparison.Ordinal);
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
                    if (System.Net.IPAddress.TryParse(ipAddress, out System.Net.IPAddress? probe))
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
            EventLog.WriteEntry("IDDSCommunity.Agents.WebSecurity.WebSecurityAgent", ex.Message);
        }
    }

    public string DisplayName
    {
        get => "Web Security Agent";
        set
        {

        }
    }

    public Image? Icon { get; set; }
    public Image? SelectedIcon { get; set; }
    public Image? UnselectedIcon { get; set; }



    public Guid Id => new("{63F5567C-7A75-4870-A842-E981855DA3E9}");


}
