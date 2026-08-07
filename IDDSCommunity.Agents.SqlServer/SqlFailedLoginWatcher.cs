using System;
using System.Text.RegularExpressions;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.Agents.SqlServer;

//  [PluginAttribute("Intrusion Detection Base Windows Security Agent", "此 Agent 掃描並監控系統事件紀錄以偵測可能的攻擊。")]
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
    /// 初始化 Agent。
    /// </summary>
    public SqlFailedLoginWatcher()
    {

    }

    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
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
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => "SQL Server Security Agent";
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
    public Guid Id => new("{0F470A49-594D-4895-ADE1-46B48B9B8A58}");
    /// <summary>
    /// 取得匹配規則運算式。
    /// </summary>
    /// <returns>傳回規則運算式執行個體。</returns>

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
