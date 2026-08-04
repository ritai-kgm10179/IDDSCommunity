using System;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Cyberarms.IntrusionDetection.Base.Plugins;

[Plugin("Intrusion Detection RRAS Security Agent", "This agent scans and monitors the system eventlog for possible RRAS attacks.")]

public partial class RrasSecurityAgent : AgentPlugin, IExtendedInformation
{

    private EventLogQuery query;
    private EventLogWatcher watcher;

    internal const string EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""20271"" Path=""System"">
                    <Select Path=""System"">
                        *[System[(EventID=20271) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public RrasSecurityAgent()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("System", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED));
        watcher = new EventLogWatcher(query);
        watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
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

    private void watcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            foreach (EventProperty prop in e.EventRecord.Properties)
            {
                if (MyRegex().IsMatch(prop.Value.ToString()))
                {
                    Match ipAddress = MyRegex().Match(prop.Value.ToString());
                    NotificationEventArgs args = new()
                    {
                        CreateDate = e.EventRecord.TimeCreated.Value,
                        EventId = e.EventRecord.Id,
                        IpAddress = ipAddress.Value
                    };
                    System.Net.IPAddress.TryParse(args.IpAddress, out System.Net.IPAddress ip);
                    if (ip != null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        OnAttackDetected(this, args);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("Cyberarms.IntrusionDetection.Base.Plugins.RrasSecurityAgent", ex.Message);
        }
    }


    public string DisplayName
    {
        get => "RRAS Security Agent - Routing and Remote Access"; set => throw new NotSupportedException("DisplayName cannot be changed!");
    }

    private Image _icon = Resources.agent15px_rras_dark;
    public Image Icon
    {
        get => _icon; set => _icon = value;
    }

    private Image _selectedIcon = Resources.agent15px_rras_white;
    public Image SelectedIcon
    {
        get => _selectedIcon; set => _selectedIcon = value;
    }

    private Image _unselectedIcon = Resources.agent15px_rras_dark;
    public Image UnselectedIcon
    {
        get => _unselectedIcon; set => _unselectedIcon = value;
    }


    public Guid Id => new("{FDA41145-2E75-400E-882C-E06EC4790EBE}");

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
