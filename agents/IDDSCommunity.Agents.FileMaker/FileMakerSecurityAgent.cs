using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;

namespace IDDSCommunity.Agents.FileMaker;

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

    [GeneratedRegex("(?:[0-9]{1,3}.){3}[0-9]{1,3}")]
    private static partial Regex MyRegex();
}
