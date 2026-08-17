using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.Agents.MailServer;
/// <summary>
/// 偵測明文 IMAP 驗證失敗嘗試，並於 STARTTLS 成功後停止解析。
/// </summary>
public sealed class ImapAgent : AgentPlugin, IExtendedInformation
{
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public static Guid AgentId => new("{3F8B715C-4A2D-4C98-9C6E-7F89B219E022}");
    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    public Guid Id => AgentId;
    /// <summary>
    /// 取得或設定 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    public string DisplayName { get; set; } = "IDDSCommunity.Agents.MailServer.ImapAgent";
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

    private readonly ConcurrentDictionary<int, ImapSessionInspector> sessions = [];
    private readonly List<PacketSniffer> sniffers = [];
    private readonly System.Timers.Timer cleanupTimer;
    private const int CleanupIntervalMins = 2;
    /// <summary>
    /// 初始化 <see cref="ImapAgent"/> 類別的新執行個體。
    /// </summary>
    public ImapAgent()
    {
        ImapConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
        cleanupTimer = new System.Timers.Timer(5000);
        cleanupTimer.Elapsed += (_, _) => RemoveExpiredSessions(DateTime.UtcNow);
    }

    internal void RemoveExpiredSessions(DateTime utcNow)
    {
        foreach (int key in sessions.Keys)
        {
            if (sessions.TryGetValue(key, out ImapSessionInspector? session) && session.LastInteraction.AddMinutes(CleanupIntervalMins) < utcNow)
                sessions.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 啟動連線階段逾期清理計時器，並開始監聽本機所有 IPv4 位址的 IMAP 流量。
    /// </summary>
    protected override void OnStartAgent()
    {
        cleanupTimer.Start();
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress address in hostEntry.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
                WatchAddress(address);
        }
        base.OnStartAgent();
    }
    /// <summary>
    /// 停止清理計時器與所有封包監聽器。
    /// </summary>
    protected override void OnPauseAgent()
    {
        cleanupTimer.Stop();
        StopWatchers();
        base.OnPauseAgent();
    }
    /// <summary>
    /// 從暫停狀態重新啟動監聽器與清理計時器。
    /// </summary>
    protected override void OnContinueAgent()
    {
        OnStartAgent();
        base.OnContinueAgent();
    }
    /// <summary>
    /// 停止清理計時器與所有封包監聽器，並清除連線階段狀態。
    /// </summary>
    protected override void OnStopAgent()
    {
        cleanupTimer.Stop();
        StopWatchers();
        base.OnStopAgent();
    }

    private void WatchAddress(IPAddress address)
    {
        if (Configuration.AgentSettings is not ImapConfig settings) return;
        PacketSniffer sniffer = new() { TcpPort = settings.ImapPort };
        sniffer.TcpPacketReceived += ClientPacketReceived;
        sniffer.TcpPacketSent += ServerPacketSent;
        try
        {
            sniffer.WatchAddress(address);
            sniffers.Add(sniffer);
        }
        catch (SocketException exception)
        {
            PacketSniffer.LogTrace(exception);
            sniffer.CloseSocket();
        }
        catch (UnauthorizedAccessException exception)
        {
            PacketSniffer.LogTrace(exception);
            sniffer.CloseSocket();
        }
    }

    private void ClientPacketReceived(object? sender, TcpPacketEventArgs e)
    {
        IPHeader packet = e.IpHeader;
        TCPHeader tcp = e.TcpHeader;
        try
        {
            int clientPort = tcp.SourcePortValue;
            if (tcp.MessageLength == 0) return;
            sessions.GetOrAdd(clientPort, static _ => new ImapSessionInspector()).ProcessClientData(tcp.Data);
        }
        catch (Exception exception)
        {
            PacketSniffer.LogTrace(exception);
        }
    }

    private void ServerPacketSent(object? sender, TcpPacketEventArgs e)
    {
        IPHeader packet = e.IpHeader;
        TCPHeader tcp = e.TcpHeader;
        try
        {
            int clientPort = tcp.DestinationPortValue;
            if (tcp.MessageLength == 0) return;
            if (sessions.TryGetValue(clientPort, out ImapSessionInspector? session) && session.ProcessServerData(tcp.Data))
            {
                NotificationEventArgs notification = new()
                {
                    CreateDate = DateTime.Now,
                    EventId = 9114,
                    EventMessage = Strings.Get("IMAP authentication failure"),
                    IpAddress = packet.DestinationAddress.ToString()
                };
                OnAttackDetected(this, notification);
            }
        }
        catch (Exception exception)
        {
            PacketSniffer.LogTrace(exception);
        }
    }

    private void StopWatchers()
    {
        foreach (PacketSniffer sniffer in sniffers)
        {
            sniffer.Abort();
            sniffer.CloseSocket();
        }
        sniffers.Clear();
        sessions.Clear();
    }
}
