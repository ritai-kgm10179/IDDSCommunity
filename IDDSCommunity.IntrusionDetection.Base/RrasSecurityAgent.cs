using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;

namespace IDDSCommunity.IntrusionDetection.Base.Plugins;
/// <summary>
/// 掃描與監控系統事件紀錄中 RRAS 路由與遠端存取攻擊之入侵偵測 Agent。
/// </summary>
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
    /// 初始化 <see cref="RrasSecurityAgent"/> 類別的新執行個體。
    /// </summary>
    public RrasSecurityAgent()
    {
    }
    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
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
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => Api.Localization.Strings.Get("RRAS Security Agent - Routing and Remote Access"); set => throw new NotSupportedException(Api.Localization.Strings.Get("DisplayName cannot be changed!"));
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
    public Guid Id => new("{FDA41145-2E75-400E-882C-E06EC4790EBE}");
    /// <summary>
    /// 取得匹配 IP 位址的規則運算式。
    /// </summary>
    /// <returns>傳回 <see cref="Regex"/> 執行個體。</returns>
    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
