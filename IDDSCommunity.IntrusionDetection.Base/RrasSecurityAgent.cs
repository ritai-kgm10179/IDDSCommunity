using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;

[Plugin("Intrusion Detection RRAS Security Agent", "This agent scans and monitors the system eventlog for possible RRAS attacks.")]

public partial class RrasSecurityAgent : AgentPlugin, IExtendedInformation
{

    private EventLogQuery? query;
    private EventLogWatcher? watcher;

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
            if (e.EventRecord is not EventRecord record)
                return;
            foreach (EventProperty prop in record.Properties)
            {
                string propertyValue = prop.Value?.ToString() ?? string.Empty;
                if (MyRegex().IsMatch(propertyValue))
                {
                    Match ipAddress = MyRegex().Match(propertyValue);
                    NotificationEventArgs args = new()
                    {
                        CreateDate = record.TimeCreated ?? DateTime.Now,
                        EventId = record.Id,
                        IpAddress = ipAddress.Value
                    };
                    System.Net.IPAddress.TryParse(args.IpAddress, out System.Net.IPAddress? ip);
                    if (ip != null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        OnAttackDetected(this, args);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Base.Plugins.RrasSecurityAgent", ex.Message);
        }
    }


    public string DisplayName
    {
        get => Api.Localization.Strings.Get("RRAS Security Agent - Routing and Remote Access"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
    }

    public Image? Icon { get; set; }
    public Image? SelectedIcon { get; set; }
    public Image? UnselectedIcon { get; set; }


    public Guid Id => new("{FDA41145-2E75-400E-882C-E06EC4790EBE}");

    /// <summary>
    /// Executes the my regex operation.
    /// </summary>
    /// <returns>The my regex result.</returns>

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
