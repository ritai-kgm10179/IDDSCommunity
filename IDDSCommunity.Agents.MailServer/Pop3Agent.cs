using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.MailServer;

public class Pop3Agent : AgentPlugin, IExtendedInformation
{
    public static Guid AgentId => new("{1F917251-2661-473A-970B-B2BB62EA6E1A}");
    public Guid Id => AgentId;
    public string DisplayName { get; set; } = "IDDSCommunity.Agents.MailServer.Pop3Agent";
    public System.Drawing.Image? Icon { get; set; }
    public System.Drawing.Image? SelectedIcon { get; set; }
    public System.Drawing.Image? UnselectedIcon { get; set; }

    public const int CLEANUP_INTERVAL_MINS = 2;
    public event EventHandler? Trace;
    public bool Tracing { get; set; }
    public System.Timers.Timer cleanupTimer;

    readonly List<PacketSniffer> sniffers = [];
    /// <summary>
    /// 初始化 <see cref="Pop3Agent"/> 類別的新執行個體。
    /// </summary>
    public Pop3Agent()
    {

        Pop3Config settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
        cleanupTimer = new System.Timers.Timer
        {
            Interval = 5000
        };
        cleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(cleanupTimer_Elapsed);
    }
    /// <summary>
    /// 處理定時器觸發事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void cleanupTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        => RemoveExpiredClients(DateTime.Now);

    internal void RemoveExpiredClients(DateTime now)
    {
        //for (int i = CurrentClients.Keys.Max(); i > 0; i--) {
        //    if (CurrentClients.ContainsKey(i) && CurrentClients[i].LastInteraction.AddMinutes(CLEANUP_INTERVAL_MINS) < DateTime.Now) CurrentClients.Remove(i);
        //}
        foreach (int key in CurrentClients.Keys)
        {
            if (CurrentClients.TryGetValue(key, out Pop3Client? client) && client.LastInteraction.AddMinutes(CLEANUP_INTERVAL_MINS) < now)
                _currentClients.TryRemove(key, out _);
        }
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
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not Pop3Config settings) return;
        PacketSniffer s = new();
        s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = settings.Pop3Port;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.MailServer", string.Format("POP3 Server Security Agent is listening on port {0}", s.TcpPort));
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        try
        {
            s.WatchAddress(address);
        }
        catch (System.Net.Sockets.SocketException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        sniffers.Add(s);
    }
    /// <summary>
    /// 執行接收測試作業。
    /// </summary>
    /// <param name="data">資料參數。</param>
    public void TestReceive(byte[] data)
    {
        IPHeader hdr = new(data, data.Length);
        s_IpPacketReceived(hdr, EventArgs.Empty);
    }
    /// <summary>
    /// 執行傳送測試作業。
    /// </summary>
    /// <param name="data">資料參數。</param>
    public void TestSend(byte[] data)
    {
        IPHeader hdr = new(data, data.Length);
        s_IpPacketSent(hdr, EventArgs.Empty);
    }
    /// <summary>
    /// 處理 IP 封包接收事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void s_IpPacketReceived(object? sender, EventArgs e)
    {
        if (sender is not IPHeader ipHeader) return;
        if (ipHeader.ProtocolType == Protocol.Tcp)
        {
            try
            {
                TCPHeader tcp = new(ipHeader.Data, ipHeader.MessageLength);
                if (int.TryParse(tcp.SourcePort, out int sourcePort) && int.TryParse(tcp.DestinationPort, out int destinationPort))
                {
                    if (Configuration.AgentSettings is Pop3Config settings && destinationPort == settings.Pop3Port)
                    {
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerPop3 pop3 = new(tcp.Data, tcp.Data.Length);
                            Pop3Client client = _currentClients.GetOrAdd(sourcePort, _ => new Pop3Client());
                            client.LastInteraction = DateTime.Now;
                            switch (pop3.Pop3Code.ToUpper())
                            {
                                case AppLayerPop3.POP3_INTERACTION_CODE_LIST:
                                    client.LastMessage = Pop3Message.LIST;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_DELE:
                                    client.LastMessage = Pop3Message.DELE;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_NOOP:
                                    client.LastMessage = Pop3Message.NOOP;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_PASS:
                                    client.LastMessage = Pop3Message.PASS;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_QUIT:
                                    _currentClients.TryRemove(sourcePort, out _);
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_RETR:
                                    client.LastMessage = Pop3Message.RETR;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_RSET:
                                    client.LastMessage = Pop3Message.RSET;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_STAT:
                                    client.LastMessage = Pop3Message.STAT;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_TOP:
                                    client.LastMessage = Pop3Message.TOP;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_UIDL:
                                    client.LastMessage = Pop3Message.UIDL;
                                    break;
                                case AppLayerPop3.POP3_INTERACTION_CODE_USER:
                                    client.LastMessage = Pop3Message.USER;
                                    break;
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
                if (int.TryParse(tcp.SourcePort, out int sourcePort) && int.TryParse(tcp.DestinationPort, out int destinationPort))
                {
                    if (Configuration.AgentSettings is Pop3Config settings && sourcePort == settings.Pop3Port)
                    {
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerPop3 ftp = new(tcp.Data, tcp.Data.Length);
                            if (ftp.Pop3Code.ToUpper().Equals(AppLayerPop3.POP3_REPLY_CODE_ERROR.ToUpper()))
                            {
                                Thread.Sleep(100);
                                if (CurrentClients.TryGetValue(destinationPort, out Pop3Client? value) && value.LastMessage == Pop3Message.PASS)
                                {
                                    if (Tracing)
                                    {
                                        OnTrace(ipHeader);
                                    }
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


    private ConcurrentDictionary<int, Pop3Client> _currentClients = [];
    public IDictionary<int, Pop3Client> CurrentClients
    {
        get
        {
            return _currentClients;
        }

        set => _currentClients = new ConcurrentDictionary<int, Pop3Client>(value);
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
        cleanupTimer.Stop();
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
            EventMessage = Strings.Get("POP3 authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }
}
