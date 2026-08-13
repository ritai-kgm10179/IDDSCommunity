using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;
/// <summary>
/// 掃描與監控系統事件紀錄中 Windows 登入失敗攻擊之基礎入侵偵測 Agent。
/// </summary>
[Plugin("Intrusion Detection Base Windows Security Agent", "This agent scans and monitors the system eventlog for possible attacks.")]
public class WindowsSecurityBase : AgentPlugin, IExtendedInformation
{
    private EventLogQuery? query;
    private EventLogWatcher? watcher;

    internal const string EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED = @"<QueryList>
                  <Query Id=""0"" Path=""Security"">
                    <Select Path=""Security"">
                        *[System[(EventID=4625) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]] and
                        *[EventData[Data[@Name='Status']='0xC000006D' or
                        Data[@Name='SubStatus']='0xC0000064' or
                        Data[@Name='SubStatus']='0xC000006A']]
                    </Select>
                  </Query>
                </QueryList>";
    /// <summary>
    /// 初始化 <see cref="WindowsSecurityBase"/> 類別的新執行個體。
    /// </summary>
    public WindowsSecurityBase()
    {
    }
    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
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
    /// 從暫停狀態復原 Agent 服務。
    /// </summary>
    protected override void OnContinueAgent() => watcher!.Enabled = true;
    /// <summary>
    /// 暫停 Agent 服務。
    /// </summary>
    protected override void OnPauseAgent() => watcher!.Enabled = false;
    /// <summary>
    /// 停止 Agent 服務並釋放事件紀錄監聽器。
    /// </summary>
    protected override void OnStopAgent()
    {
        watcher?.Dispose();
        watcher = null;
        query = null;
    }
    /// <summary>
    /// 處理事件紀錄寫入事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件紀錄寫入參數。</param>
    private void watcher_EventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        try
        {
            string[] xPathProperties = [@"Event/EventData/Data[@Name=""IpAddress""]"];
            EventLogPropertySelector props = new(xPathProperties);
            if (e.EventRecord is not EventLogRecord record)
                return;
            string ipAddress = record.GetPropertyValues(props)[0]?.ToString()?.Trim('[', ']') ?? string.Empty;
            if (!System.Net.IPAddress.TryParse(ipAddress, out System.Net.IPAddress? address) || System.Net.IPAddress.IsLoopback(address)) return;
            NotificationEventArgs args = new()
            {
                CreateDate = record.TimeCreated ?? DateTime.Now,
                EventId = record.Id,
                IpAddress = address.ToString()
            };
            OnAttackDetected(this, args);
        }
        catch (Exception ex)
        {
            EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Base.Plugins.WindowsSecurityBase", ex.Message);
        }
    }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => Api.Localization.Strings.Get("Windows Base Security Agent"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
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
    public Guid Id => new("{CC03AE88-51B4-426C-BA68-50875D70409F}");
}
