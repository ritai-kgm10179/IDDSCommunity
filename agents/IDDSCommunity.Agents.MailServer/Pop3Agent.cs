using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Collections.Concurrent;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.MailServer;

/// <summary>
/// 監聽明文 POP3 流量，偵測伺服器回覆 <c>-ERR</c> 於 <c>PASS</c> 命令之後所代表的驗證失敗。
/// </summary>
public class Pop3Agent : AgentPlugin, IExtendedInformation
{
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public static Guid AgentId => new("{1F917251-2661-473A-970B-B2BB62EA6E1A}");
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public Guid Id => AgentId;
    /// <summary>
    /// 取得或設定 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName { get; set; } = "IDDSCommunity.Agents.MailServer.Pop3Agent";
    /// <summary>
    /// 取得或設定 Agent 的預設圖示。
    /// </summary>
    public System.Drawing.Image? Icon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的主題圖示。
    /// </summary>
    public System.Drawing.Image? SelectedIcon { get; set; }
    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的主題圖示。
    /// </summary>
    public System.Drawing.Image? UnselectedIcon { get; set; }

    /// <summary>
    /// 閒置用戶端連線狀態於清理前的保留分鐘數。
    /// </summary>
    public const int CLEANUP_INTERVAL_MINS = 2;
    /// <summary>
    /// 當偵測到 TLS 封包且已啟用追蹤時引發。
    /// </summary>
    public event EventHandler? Trace;
    /// <summary>
    /// 取得或設定是否啟用 TLS 封包追蹤通知。
    /// </summary>
    public bool Tracing { get; set; }
    /// <summary>
    /// 用於定期清除逾期用戶端連線狀態的計時器。
    /// </summary>
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
        cleanupTimer.Start();
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
        s.TcpPacketReceived += TcpPacketReceived;
        s.TcpPacketSent += TcpPacketSent;
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
        TcpPacketReceived(this, new TcpPacketEventArgs(hdr, new TCPHeader(hdr.Data, hdr.MessageLength)));
    }
    /// <summary>
    /// 執行傳送測試作業。
    /// </summary>
    /// <param name="data">資料參數。</param>
    public void TestSend(byte[] data)
    {
        IPHeader hdr = new(data, data.Length);
        TcpPacketSent(this, new TcpPacketEventArgs(hdr, new TCPHeader(hdr.Data, hdr.MessageLength)));
    }
    /// <summary>
    /// 處理 IP 封包接收事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void TcpPacketReceived(object? sender, TcpPacketEventArgs e)
    {
        IPHeader ipHeader = e.IpHeader;
        TCPHeader tcp = e.TcpHeader;
        try
        {
            int sourcePort = tcp.SourcePortValue;
            int destinationPort = tcp.DestinationPortValue;
            if (Configuration.AgentSettings is Pop3Config settings && destinationPort == settings.Pop3Port)
            {
                if (tcp.MessageLength > 0)
                {
                    AppLayerPop3 pop3 = new(tcp.Data, tcp.Data.Length);
                    Pop3Client client = _currentClients.GetOrAdd(sourcePort, _ => new Pop3Client());
                    client.LastInteraction = DateTime.Now;
                    switch (pop3.Pop3Code.ToUpper())
                    {
                        case AppLayerPop3.POP3_INTERACTION_CODE_LIST: client.LastMessage = Pop3Message.LIST; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_DELE: client.LastMessage = Pop3Message.DELE; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_NOOP: client.LastMessage = Pop3Message.NOOP; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_PASS: client.LastMessage = Pop3Message.PASS; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_QUIT: _currentClients.TryRemove(sourcePort, out _); break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_RETR: client.LastMessage = Pop3Message.RETR; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_RSET: client.LastMessage = Pop3Message.RSET; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_STAT: client.LastMessage = Pop3Message.STAT; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_TOP: client.LastMessage = Pop3Message.TOP; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_UIDL: client.LastMessage = Pop3Message.UIDL; break;
                        case AppLayerPop3.POP3_INTERACTION_CODE_USER: client.LastMessage = Pop3Message.USER; break;
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
    /// 處理 IP 封包傳送事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void TcpPacketSent(object? sender, TcpPacketEventArgs e)
    {
        IPHeader ipHeader = e.IpHeader;
        TCPHeader tcp = e.TcpHeader;
        try
        {
            int sourcePort = tcp.SourcePortValue;
            int destinationPort = tcp.DestinationPortValue;
            if (Configuration.AgentSettings is Pop3Config settings && sourcePort == settings.Pop3Port && tcp.MessageLength > 0)
            {
                AppLayerPop3 pop3 = new(tcp.Data, tcp.Data.Length);
                if (pop3.Pop3Code.Equals(AppLayerPop3.POP3_REPLY_CODE_ERROR, StringComparison.OrdinalIgnoreCase) &&
                    CurrentClients.TryGetValue(destinationPort, out Pop3Client? value) && value.LastMessage == Pop3Message.PASS)
                {
                    if (Tracing)
                        OnTrace(ipHeader);
                    UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            PacketSniffer.LogTrace(ex);
        }
    }


    private ConcurrentDictionary<int, Pop3Client> _currentClients = [];
    /// <summary>
    /// 取得或設定目前追蹤中的用戶端連線狀態，以來源連接埠為索引鍵。
    /// </summary>
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
            EventMessage = Strings.Get("POP3 authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }
}
