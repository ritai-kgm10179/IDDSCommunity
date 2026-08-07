using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.TerminalServer;

public class TlsSslAgent : AgentPlugin, IExtendedInformation
{
    public event EventHandler? Trace;
    public bool Tracing { get; set; }

    readonly List<PacketSniffer> sniffers = [];
    private System.Diagnostics.Eventing.Reader.EventLogWatcher? _securityWatcher;
    private System.Diagnostics.Eventing.Reader.EventLogWatcher? _terminalServicesWatcher;

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
        RunWatcher();
        StartEventLogWatchers();
        base.OnStartAgent();
    }

    /// <summary>
    /// 啟動 Windows 事件日誌中針對 RDP (LogonType 10 / 7) 的即時監聽器。
    /// </summary>
    private void StartEventLogWatchers()
    {
        try
        {
            // 1. 監聽 Security 日誌中的 Event 4625 且 LogonType = 10 (RemoteInteractive) 或 7 (Unlock)
            string securityQueryText = @"<QueryList>
                <Query Id=""0"" Path=""Security"">
                  <Select Path=""Security"">
                    *[System[(EventID=4625)]] and (*[EventData[Data[@Name='LogonType']='10']] or *[EventData[Data[@Name='LogonType']='7']])
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
            System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.TlsSslAgent", "Failed to start Security EventLog watcher for RDP: " + ex.Message, System.Diagnostics.EventLogEntryType.Warning);
        }

        try
        {
            // 2. 監聽 TerminalServices-RemoteConnectionManager Operational 日誌中的 1149 事件
            string tsQueryText = @"<QueryList>
                <Query Id=""0"" Path=""Microsoft-Windows-TerminalServices-RemoteConnectionManager/Operational"">
                  <Select Path=""Microsoft-Windows-TerminalServices-RemoteConnectionManager/Operational"">
                    *[System[(EventID=1149)]]
                  </Select>
                </Query>
              </QueryList>";
            var tsQuery = new System.Diagnostics.Eventing.Reader.EventLogQuery("Microsoft-Windows-TerminalServices-RemoteConnectionManager/Operational", System.Diagnostics.Eventing.Reader.PathType.LogName, tsQueryText);
            _terminalServicesWatcher = new System.Diagnostics.Eventing.Reader.EventLogWatcher(tsQuery);
            _terminalServicesWatcher.EventRecordWritten += OnTerminalServicesEventWritten;
            _terminalServicesWatcher.Enabled = true;
        }
        catch
        {
            // Operational 日誌在部分系統未開啟分析時屬正常備援
        }
    }

    private void OnRdpSecurityEventWritten(object? sender, System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs e)
    {
        try
        {
            if (e.EventRecord is not System.Diagnostics.Eventing.Reader.EventLogRecord record) return;
            string[] xPathProperties = [@"Event/EventData/Data[@Name=""IpAddress""]"];
            var props = new System.Diagnostics.Eventing.Reader.EventLogPropertySelector(xPathProperties);
            var values = record.GetPropertyValues(props);
            string rawIp = values != null && values.Count > 0 ? values[0]?.ToString() ?? string.Empty : string.Empty;
            rawIp = rawIp.Trim('[', ']');
            if (IPAddress.TryParse(rawIp, out IPAddress? address) && !IPAddress.IsLoopback(address))
            {
                UnsuccessfulLogin(address.ToString());
            }
        }
        catch (Exception ex)
        {
            PacketSniffer.LogTrace(ex);
        }
    }

    private void OnTerminalServicesEventWritten(object? sender, System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs e)
    {
        try
        {
            if (e.EventRecord is not System.Diagnostics.Eventing.Reader.EventLogRecord record) return;
            string[] xPathProperties = [@"Event/UserData/EventXML/Param3"];
            var props = new System.Diagnostics.Eventing.Reader.EventLogPropertySelector(xPathProperties);
            var values = record.GetPropertyValues(props);
            string rawIp = values != null && values.Count > 0 ? values[0]?.ToString() ?? string.Empty : string.Empty;
            rawIp = rawIp.Trim('[', ']');
            if (IPAddress.TryParse(rawIp, out IPAddress? address) && !IPAddress.IsLoopback(address))
            {
                UnsuccessfulLogin(address.ToString());
            }
        }
        catch (Exception ex)
        {
            PacketSniffer.LogTrace(ex);
        }
    }
    /// <summary>
    /// 執行監聽器啟動作業。
    /// </summary>
    void RunWatcher()
    {
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        if (hostEntry.AddressList.Length > 0)
        {
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    WatchAddress(ip);
                }
            }
        }
    }


    /// <summary>
    /// 執行監聽指定位址作業。
    /// </summary>
    /// <param name="ipAddress">IP 位址參數。</param>
    void WatchAddress(object? ipAddress)
    {
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not TslSslConfig settings) return;
        PacketSniffer s = new();
        // s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = settings.RdpPort;
        System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.TlsSslAgent", string.Format("Remote Desktop Security Agent is listening on port {0}", s.TcpPort));
        s.WatchAddress(address);
        sniffers.Add(s);
    }
    /// <summary>
    /// 處理 IP 封包傳送事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void s_IpPacketSent(object? sender, EventArgs e)
    {
        if (sender is not IPHeader ipHeader) return;
        if (ipHeader.ProtocolType == Protocol.Tcp)
        {
            try
            {
                TCPHeader tcp = new(ipHeader.Data, ipHeader.MessageLength);
                if (int.TryParse(tcp.SourcePort, out int sourcePort))
                {
                    if (Configuration.AgentSettings is TslSslConfig settings && sourcePort == settings.RdpPort)
                    {
                        if (Tracing)
                        {
                            OnTrace(ipHeader);
                        }
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerTlsSsl tls = new(tcp.Data, tcp.Data.Length);
                            if (tls.TlsHeader.MinorVersion >= 1 && tls.TlsHeader.MinorVersion < 10 && tls.TlsHeader.MajorVersion >= 1 && tls.TlsHeader.MajorVersion < 10)
                            {       // check if packet is tls/ssl
                                if (tls.TlsHeader.ContentType == AppLayerTlsSsl.CONTENT_TYPE_ENCRYPTED_ALERT)
                                {
                                    UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
                                }
                            }
                        }

                        // Console.WriteLine("Flags: {0}\tAck: {1}\tSeq:{2}", tcp.Flags, tcp.AcknowledgementNumber, tcp.SequenceNumber);
                        // Console.WriteLine("Source: {0}:{1}\tDestination: {2}:{3}", ipHeader.SourceAddress, tcp.SourcePort, ipHeader.DestinationAddress, tcp.DestinationPort);
                    }
                }
            }
            catch (Exception ex)
            {
                PacketSniffer.LogTrace(ex);
            }

        }
    }
    /// <summary>
    /// 處理追蹤通知。
    /// </summary>
    /// <param name="tlsPackage">TLS 封包資料。</param>
    private void OnTrace(IPHeader tlsPackage) => Trace?.Invoke(tlsPackage, EventArgs.Empty);
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
            _securityWatcher.Dispose();
            _securityWatcher = null;
        }
        if (_terminalServicesWatcher != null)
        {
            _terminalServicesWatcher.Enabled = false;
            _terminalServicesWatcher.Dispose();
            _terminalServicesWatcher = null;
        }
        foreach (PacketSniffer s in sniffers)
        {
            s.Abort();
            s.CloseSocket();
        }
        sniffers.Clear();
        base.OnStopAgent();
    }

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
