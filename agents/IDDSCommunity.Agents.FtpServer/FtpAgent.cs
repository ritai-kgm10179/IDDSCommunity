using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.FtpServer;

/// <summary>
/// 監聽明文 FTP 流量，偵測伺服器回覆碼 530 所代表的登入驗證失敗。
/// </summary>
public class FtpAgent : AgentPlugin, IExtendedInformation
{
    /// <summary>
    /// 當偵測到 TLS 封包且已啟用追蹤時引發。
    /// </summary>
    public event EventHandler? Trace;
    /// <summary>
    /// 取得或設定是否啟用 TLS 封包追蹤通知。
    /// </summary>
    public bool Tracing { get; set; }
    private readonly List<PacketSniffer> sniffers = [];
    /// <summary>
    /// 初始化 <see cref="FtpAgent"/> 類別的新執行個體。
    /// </summary>
    public FtpAgent()
    {
        FtpConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }
    /// <summary>
    /// 處理啟動 Agent 的通知。
    /// </summary>
    protected override void OnStartAgent()
    {
        RunWatcher();
        base.OnStartAgent();
    }
    /// <summary>
    /// 執行監聽器啟動作業。
    /// </summary>
    private void RunWatcher()
    {
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        if (hostEntry.AddressList.Length > 0)
        {
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
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
    private void WatchAddress(object? ipAddress)
    {
        if (ipAddress is not IPAddress address) return;
        PacketSniffer s = new();
        s.TcpPacketSent += TcpPacketSent;
        if (Configuration.AgentSettings is not FtpConfig settings) return;
        s.TcpPort = settings.FtpPort;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.FtpServer", $"Ftp Server Security Agent is listening on port {s.TcpPort}");
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceWarning("FTP agent startup log failed: {0}", exception); }
        try
        {
            s.WatchAddress(address);
        }
        catch (Exception exception) { PacketSniffer.LogTrace(exception); }
        sniffers.Add(s);
    }
    /// <summary>
    /// 處理 IP 封包傳送事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void TcpPacketSent(object? sender, TcpPacketEventArgs e)
    {
        IPHeader ipHeader = e.IpHeader;
        TCPHeader tcp = e.TcpHeader;
        try
        {
            if (Configuration.AgentSettings is FtpConfig settings && tcp.SourcePortValue == settings.FtpPort)
            {
                if (Tracing)
                {
                    OnTrace(ipHeader);
                }
                if (tcp.MessageLength > 0)
                {
                    AppLayerFtp ftp = new(tcp.Data, tcp.MessageLength);
                    if (ftp.IsAuthenticationFailure)
                    {
                        UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PacketSniffer.LogTrace(ex);
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
        foreach (PacketSniffer s in sniffers)
        {
            s.Abort();
            s.CloseSocket();
        }
        sniffers.Clear();
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
    private void UnsuccessfulLogin(string ipAddress)
    {
        NotificationEventArgs args = new()
        {
            CreateDate = DateTime.Now,
            EventId = 9112,
            EventMessage = Strings.Get("FTP authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }
    /// <summary>
    /// 取得 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName
    {
        get => "FTP Security Agent";
        set { }
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
    public Guid Id => new("{F040A37F-8A53-428E-85A3-EDC858144742}");
}
