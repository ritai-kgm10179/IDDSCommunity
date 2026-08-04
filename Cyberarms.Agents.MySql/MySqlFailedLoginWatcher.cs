using System;
using System.Text.RegularExpressions;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace Cyberarms.Agents.MySql;

//  [PluginAttribute("Intrusion Detection Base Windows Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public partial class MySqlFailedLoginWatcher : AgentPlugin, IExtendedInformation
{


    private EventLogQuery query;
    private EventLogWatcher watcher;

    internal const string EVENT_LOG_QUERY_MYSQL_SERVER_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""100"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=100) and
                        TimeCreated[timediff(@SystemTime) &lt;= 864000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public MySqlFailedLoginWatcher()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_MYSQL_SERVER_LOGIN_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(Watcher_EventRecordWritten);
        watcher.Enabled = true;
    }

    /// <summary>
    /// Resume from Pause
    /// </summary>
    protected override void OnContinueAgent() => watcher.Enabled = true;

    /// <summary>
    /// Pause the agent
    /// </summary>
    protected override void OnPauseAgent() => watcher.Enabled = false;

    /// <summary>
    /// Stop the agent
    /// </summary>
    protected override void OnStopAgent()
    {
        watcher.Enabled = false;
        watcher = null;
        query = null;
    }

    private void Watcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            // (new System.Collections.Generic.Mscorlib_CollectionDebugView<System.Diagnostics.Eventing.Reader.EventProperty>(e.EventRecord.Properties)).Items[0]
            foreach (EventProperty prop in e.EventRecord.Properties)
            {
                if (MyRegex().IsMatch(prop.Value.ToString()))
                {
                    Match ipAddress = MyRegex1().Match(prop.Value.ToString());
                    NotificationEventArgs args = new()
                    {
                        CreateDate = e.EventRecord.TimeCreated.Value,
                        EventId = e.EventRecord.Id,
                        IpAddress = ipAddress.Value
                    };
                    if (System.Net.IPAddress.TryParse(ipAddress.Value, out System.Net.IPAddress probe))
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
            EventLog.WriteEntry("Cyberarms.Agents.MySqlServer.MySqlFailedLoginWatcher", ex.Message);
        }
    }

    public string DisplayName
    {
        get => "MySql Server Security Agent";
        set
        {

        }
    }

    public Image Icon
    {
        get => Resource.agent15px_sql_dark;
        set
        {

        }
    }

    public Image SelectedIcon
    {
        get => Resource.agent15px_sql_white;
        set
        {

        }
    }

    public Image UnselectedIcon
    {
        get => Resource.agent15px_sql_dark;
        set
        {

        }
    }



    public Guid Id => new("{EE4906AD-7242-4940-A3B0-81B4E3F16B71}");

    [GeneratedRegex("^.*?\bAccess denied\b.*(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex1();
}
