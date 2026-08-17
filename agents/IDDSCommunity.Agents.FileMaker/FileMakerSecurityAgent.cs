using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;

namespace IDDSCommunity.Agents.FileMaker;

/// <summary>
/// 監看 Windows 應用程式事件記錄中的 FileMaker Server 驗證失敗事件（事件 661）。
/// </summary>
[Plugin("Intrusion Detection Base Windows Security Agent", "此 Agent 掃描並監控系統事件紀錄以偵測可能的攻擊。")]
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
    /// 初始化 Agent。
    /// </summary>
    public FileMakerSecurityAgent()
    {

    }

    /// <summary>
    /// 啟動 Agent 服務並初始化事件紀錄監聽器。
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
    /// 從暫停狀態復原 Agent 服務。
    /// </summary>
    protected override void OnContinueAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = true;
        }
    }
    /// <summary>
    /// 暫停 Agent 服務。
    /// </summary>
    protected override void OnPauseAgent()
    {
        if (watcher is not null)
        {
            watcher.Enabled = false;
        }
    }
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
                string? propertyValue = prop.Value?.ToString();
                if (propertyValue is not null)
                {
                    try
                    {
                        Match ipMatch = MyRegex().Match(propertyValue);
                        if (ipMatch.Success)
                        {
                            NotificationEventArgs args = new()
                            {
                                CreateDate = record.TimeCreated ?? DateTime.Now,
                                EventId = record.Id,
                                IpAddress = ipMatch.Value
                            };
                            if (IPAddress.TryParse(args.IpAddress, out IPAddress? ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                OnAttackDetected(this, args);
                            }
                        }
                    }
                    catch (RegexMatchTimeoutException) { }
                }
            }
        }
        catch (Exception ex)
        {
            try { EventLog.WriteEntry("IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent", ex.Message); }
            catch (Exception logException) { System.Diagnostics.Trace.TraceError("Unable to write FileMaker security event log entry: {0}", logException.Message); }
        }
    }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => IDDSCommunity.IntrusionDetection.Api.Localization.Strings.Get("FileMaker Security Agent"); set => throw new NotSupportedException(IDDSCommunity.IntrusionDetection.Api.Localization.Strings.Get("DisplayName cannot be changed!"));
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
    public Guid Id => new("{F0F28CC4-8103-4781-927E-CFD4C5991092}");
    /// <summary>
    /// 取得匹配規則運算式。
    /// </summary>
    /// <returns>傳回規則運算式執行個體。</returns>
    [GeneratedRegex(@"(?:[0-9]{1,3}\.){3}[0-9]{1,3}", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex MyRegex();
}
