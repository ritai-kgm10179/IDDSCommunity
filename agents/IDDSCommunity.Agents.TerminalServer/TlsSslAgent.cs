using System;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.Agents.TerminalServer;

/// <summary>
/// 監看 Windows 安全性事件記錄，偵測遠端桌面（RDP）TLS/SSL 連線之驗證失敗事件（事件 4625、登入類型 10）。
/// </summary>
public class TlsSslAgent : AgentPlugin, IExtendedInformation
{
    private System.Diagnostics.Eventing.Reader.EventLogWatcher? _securityWatcher;

    /// <summary>
    /// 初始化 <see cref="TlsSslAgent"/> 類別的新執行個體。
    /// </summary>
    public TlsSslAgent()
    {
        TslSslConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }
    /// <summary>
    /// 處理啟動 Agent 的通知。
    /// </summary>
    protected override void OnStartAgent()
    {
        StartEventLogWatcher();
        base.OnStartAgent();
    }

    /// <summary>
    /// 啟動 Windows 事件日誌中針對 RDP 認證失敗的即時監聽器。
    /// </summary>
    private void StartEventLogWatcher()
    {
        try
        {
            // Event 4625 的 LogonType 10 才代表遠端桌面互動式登入失敗。
            string securityQueryText = @"<QueryList>
                <Query Id=""0"" Path=""Security"">
                  <Select Path=""Security"">
                    *[System[(EventID=4625)]] and *[EventData[Data[@Name='LogonType']='10']]
                  </Select>
                </Query>
              </QueryList>";
            var securityQuery = new System.Diagnostics.Eventing.Reader.EventLogQuery("Security", System.Diagnostics.Eventing.Reader.PathType.LogName, securityQueryText);
            _securityWatcher = new System.Diagnostics.Eventing.Reader.EventLogWatcher(securityQuery);
            _securityWatcher.EventRecordWritten += OnRdpSecurityEventWritten;
            _securityWatcher.Enabled = true;
        }
        catch (Exception ex)
        {
            try { System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.TlsSslAgent", "Failed to start Security EventLog watcher for RDP: " + ex.Message, System.Diagnostics.EventLogEntryType.Warning); }
            catch (Exception logException) { System.Diagnostics.Trace.TraceError("Unable to write TLS/SSL security event log entry: {0}", logException.Message); }
        }

    }

    private static readonly System.Diagnostics.Eventing.Reader.EventLogPropertySelector RdpPropertySelector = new(
    [
        @"Event/EventData/Data[@Name=""LogonType""]",
        @"Event/EventData/Data[@Name=""Status""]",
        @"Event/EventData/Data[@Name=""SubStatus""]",
        @"Event/EventData/Data[@Name=""IpAddress""]"
    ]);

    private void OnRdpSecurityEventWritten(object? sender, System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs e)
    {
        try
        {
            using System.Diagnostics.Eventing.Reader.EventRecord? rawRecord = e.EventRecord;
            if (rawRecord is not System.Diagnostics.Eventing.Reader.EventLogRecord record) return;
            var values = record.GetPropertyValues(RdpPropertySelector);
            if (values.Count < 4 || !IsCredentialFailure(values[0]?.ToString(), values[1]?.ToString(), values[2]?.ToString()))
            {
                return;
            }
            string rawIp = values[3]?.ToString()?.Trim('[', ']') ?? string.Empty;
            if (IPAddress.TryParse(rawIp, out IPAddress? address) && !IPAddress.IsLoopback(address)) UnsuccessfulLogin(address.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("無法處理 RDP 登入失敗事件：{0}", ex.Message);
        }
    }

    internal static bool IsCredentialFailure(string? logonType, string? status, string? subStatus)
    {
        if (!string.Equals(logonType, "10", StringComparison.Ordinal)) return false;
        return IsCredentialStatus(status) || IsCredentialStatus(subStatus);
    }

    private static bool IsCredentialStatus(string? value) =>
        string.Equals(value, "0xC000006D", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "0xC0000064", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "0xC000006A", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 處理繼續執行 Agent 的通知。
    /// </summary>
    protected override void OnContinueAgent()
    {
        OnStartAgent();
        base.OnContinueAgent();
    }
    /// <summary>
    /// 處理暫停 Agent 的通知。
    /// </summary>
    protected override void OnPauseAgent()
    {
        OnStopAgent();
        base.OnPauseAgent();
    }
    /// <summary>
    /// 處理停止 Agent 的通知。
    /// </summary>
    protected override void OnStopAgent()
    {
        if (_securityWatcher != null)
        {
            _securityWatcher.Enabled = false;
            _securityWatcher.EventRecordWritten -= OnRdpSecurityEventWritten;
            _securityWatcher.Dispose();
            _securityWatcher = null;
        }
        base.OnStopAgent();
    }

    /// <summary>
    /// 取得 Agent 目前是否正在執行。
    /// </summary>
    public override bool IsRunning => base.IsRunning;
    /// <summary>
    /// 處理登入失敗作業。
    /// </summary>
    /// <param name="ipAddress">IP 位址參數。</param>
    void UnsuccessfulLogin(string ipAddress)
    {
        NotificationEventArgs args = new()
        {
            CreateDate = DateTime.Now,
            EventId = 9112,
            EventMessage = Strings.Get("Remote desktop connection TLS/SSL authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }

    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => "TLS/SSL Security Agent";
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
    public Guid Id => new("{A682433B-852F-4150-ADF4-FB7F75090015}");
}
