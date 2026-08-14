using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.Agents.WebSecurity;

//  [PluginAttribute("Intrusion Detection Base Windows Security Agent", "此 Agent 掃描並監控系統事件紀錄以偵測可能的攻擊。")]

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
    /// 初始化 Agent。
    /// </summary>
    public WebSecurityAgent()
    {

    }

    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
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
    /// 從暫停狀態復原 Agent 服務。
    /// </summary>
    protected override void OnContinueAgent() => SetWatcherEnabled(true);
    /// <summary>
    /// 暫停 Agent 服務。
    /// </summary>
    protected override void OnPauseAgent() => SetWatcherEnabled(false);
    /// <summary>
    /// 停止 Agent 服務。
    /// </summary>
    protected override void OnStopAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = false;
            watcher.EventRecordWritten -= Watcher_EventRecordWritten;
            watcher.Dispose();
        }
        watcher = null;
        query = null;
    }
    /// <summary>
    /// 設定監聽器啟用狀態。
    /// </summary>
    /// <param name="enabled">是否啟用的數值。</param>
    private void SetWatcherEnabled(bool enabled)
    {
        if (watcher is not null)
        {
            watcher.Enabled = enabled;
        }
    }
    /// <summary>
    /// 處理事件紀錄寫入事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void Watcher_EventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            using EventRecord? record = e.EventRecord;
            if (record is null)
            {
                return;
            }

            foreach (EventProperty prop in record.Properties)
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
                        CreateDate = record.TimeCreated ?? DateTime.Now,
                        EventId = record.Id,
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
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => "Web Security Agent";
        set
        {

        }
    }
    /// <summary>
    /// 取得或設定 Agent 的預設圖示。
    /// </summary>
    public Image? Icon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? SelectedIcon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的主題圖示。
    /// </summary>
    public Image? UnselectedIcon { get; set; }


    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public Guid Id => new("{63F5567C-7A75-4870-A842-E981855DA3E9}");


}
