using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;

[Plugin("Active Directory Credential validation Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public class AdCredentialValidationSecurityAgent : AgentPlugin, IExtendedInformation
{


    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""0"" Path=""Security"">
                    <Select Path=""Security"">
                        *[System[(EventID=4776) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]
                    </Select>
                  </Query>
                </QueryList>";

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public AdCredentialValidationSecurityAgent()
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
            string[] xPathProperties = [@"Event/EventData/Data[@Name=""Workstation""]"];
            EventLogPropertySelector props = new(xPathProperties);
            if (e.EventRecord is not EventLogRecord record)
                return;
            string hostName = record.GetPropertyValues(props)[0]?.ToString() ?? string.Empty;
            string[] ipAddresses = ResolveIp(hostName);
            foreach (string ipAddress in ipAddresses)
            {
                NotificationEventArgs args = new()
                {
                    CreateDate = record.TimeCreated ?? DateTime.Now,
                    EventId = record.Id,
                    IpAddress = ipAddress
                };
                OnAttackDetected(this, args);
            }
        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase.AdCredentialValidation", ex.Message);
        }
    }

    /// <summary>
    /// Resolves ip.
    /// </summary>
    /// <param name="hostname">The hostname value.</param>
    /// <returns>The resolve ip result.</returns>

    private static string[] ResolveIp(string hostname)
    {
        List<string> result = [];
        IPAddress[] addr = Dns.GetHostAddresses(hostname);
        foreach (IPAddress ip in addr)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                result.Add(ip.ToString());
            }
        }
        return result.ToArray();
    }

    public string DisplayName
    {
        get => Api.Localization.Strings.Get("AD Credential Validation Security Agent"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
    }

    public Image? Icon { get; set; }
    public Image? SelectedIcon { get; set; }
    public Image? UnselectedIcon { get; set; }


    public Guid Id => new("{D67852B4-DBEF-4831-877C-E37DAB764952}");

}
