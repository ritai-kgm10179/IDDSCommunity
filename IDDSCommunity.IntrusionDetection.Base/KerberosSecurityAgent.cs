using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;

[Plugin("Intrusion Detection Base Windows (Kerberos) Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public class KerberosSecurityAgent : AgentPlugin, IExtendedInformation
{


    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""0"" Path=""Security"">
                    <Select Path=""Security"">
                        *[System[(EventID=4771) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public KerberosSecurityAgent()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Security", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
        watcher.Enabled = true;
    }

    /// <summary>
    /// Resume from Pause
    /// </summary>
    protected override void OnContinueAgent() => watcher!.Enabled = true;

    /// <summary>
    /// Pause the agent
    /// </summary>
    protected override void OnPauseAgent() => watcher!.Enabled = false;

    /// <summary>
    /// Stop the agent
    /// </summary>
    protected override void OnStopAgent()
    {
        watcher?.Dispose();
        watcher = null;
        query = null;
    }

    /// <summary>
    /// Handles the event record written event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void watcher_EventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            string[] xPathProperties = [@"Event/EventData/Data[@Name=""Client Address""]"];
            EventLogPropertySelector props = new(xPathProperties);
            if (e.EventRecord is not EventLogRecord record)
                return;
            string ipAddress = record.GetPropertyValues(props)[0]?.ToString() ?? string.Empty;
            NotificationEventArgs args = new()
            {
                CreateDate = record.TimeCreated ?? DateTime.Now,
                EventId = record.Id,
                IpAddress = ipAddress
            };
            OnAttackDetected(this, args);
        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase.Kerberos", ex.Message);
        }
    }


    public string DisplayName
    {
        get => Api.Localization.Strings.Get("Kerberos pre-authentication Security Agent"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
    }

    public Image? Icon { get; set; }
    public Image? SelectedIcon { get; set; }
    public Image? UnselectedIcon { get; set; }


    public Guid Id => new("{880435D7-AB31-4498-B872-1512E7D723F0}");

}
