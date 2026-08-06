using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;

namespace IDDSCommunity.Agents.FileMaker;

[Plugin("Intrusion Detection Base Windows Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]

public partial class FileMakerSecurityAgent : AgentPlugin, IExtendedInformation
{



    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""661"" Path=""Application"">
                    <Select Path=""Application"">
                        *[System[(EventID=661) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public FileMakerSecurityAgent()
    {

    }


    /// <summary>
    /// Agent Startup, initialization of our EventLog watcher
    /// </summary>
    protected override void OnStartAgent()
    {
        query = new EventLogQuery("Application", PathType.LogName,
            string.Format(EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED));
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
                    IPAddress.TryParse(args.IpAddress, out IPAddress? ip);
                    if (ip != null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        OnAttackDetected(this, args);
                    }
                }
                //if (prop.Value.ToString().Contains("CLIENT:")) {
                //    string client = prop.Value.ToString();
                //    int start = client.IndexOf("CLIENT:") + 7;
                //    string ipAddress = client.Substring(start, client.LastIndexOf(']') - start).Trim();
                //    NotificationEventArgs args = new NotificationEventArgs();
                //    args.CreateDate = e.EventRecord.TimeCreated.Value;
                //    args.EventId = e.EventRecord.Id;
                //    args.IpAddress = ipAddress;
                //    OnAttackDetected(this, args);
                //}
            }

        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent", ex.Message);
        }
    }


    public string DisplayName
    {
        get => IDDSCommunity.IntrusionDetection.Api.Localization.Strings.Get("FileMaker Security Agent"); set => throw new NotSupportedException(IDDSCommunity.IntrusionDetection.Api.Localization.Strings.Get("DisplayName cannot be changed!"));
    }

    public Image Icon { get; set; } = null!;
    public Image SelectedIcon { get; set; } = null!;
    public Image UnselectedIcon { get; set; } = null!;

    public Guid Id => new("{F0F28CC4-8103-4781-927E-CFD4C5991092}");

    /// <summary>
    /// Executes the my regex operation.
    /// </summary>
    /// <returns>The my regex result.</returns>

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
