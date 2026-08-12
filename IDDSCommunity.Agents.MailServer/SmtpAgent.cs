using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Net.Sockets;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.MailServer;

public class SmtpAgent : AgentPlugin, IExtendedInformation
{
    public static Guid AgentId => new("{EB69BF23-939C-4F89-97D0-50274306D018}");
    public Guid Id => AgentId;
    public string DisplayName { get; set; } = "IDDSCommunity.Agents.MailServer.SmtpAgent";
    public System.Drawing.Image? Icon { get; set; }
    public System.Drawing.Image? SelectedIcon { get; set; }
    public System.Drawing.Image? UnselectedIcon { get; set; }

    public event EventHandler? Trace;
    public bool Tracing { get; set; }

    private readonly List<PacketSniffer> sniffers = [];
    /// <summary>
    /// 初始化 <see cref="SmtpAgent"/> 類別的新執行個體。
    /// </summary>
    public SmtpAgent()
    {
        SmtpConfig settings = new();
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
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not SmtpConfig settings) return;
        PacketSniffer s = new();
        s.IpPacketSent += s_IpPacketSent;
        s.TcpPort = settings.SmtpPort;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.SmtpServer", $"Smtp Server Security Agent is listening on port {s.TcpPort}");
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceWarning("SMTP agent startup log failed: {0}", exception); }
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
    private void s_IpPacketSent(object? sender, EventArgs e)
    {
        if (sender is not IPHeader ipHeader) return;
        if (ipHeader.ProtocolType == Protocol.Tcp)
        {
            try
            {
                TCPHeader tcp = new(ipHeader.Data, ipHeader.MessageLength);
                if (int.TryParse(tcp.SourcePort, out int sourcePort))
                {
                    if (Configuration.AgentSettings is SmtpConfig settings && sourcePort == settings.SmtpPort)
                    {
                        if (Tracing)
                        {
                            OnTrace(ipHeader);
                        }
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerSmtp ftp = new(tcp.Data, tcp.Data.Length);
                            if (ftp.SmtpReplyCode == AppLayerSmtp.SMTP_REPLY_CODE_LOGIN_DENIED)
                            {
                                UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
                            }
                        }
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
    private void UnsuccessfulLogin(string ipAddress)
    {
        NotificationEventArgs args = new()
        {
            CreateDate = DateTime.Now,
            EventId = 9112,
            EventMessage = Strings.Get("SMTP authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }
}
