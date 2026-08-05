using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cyberarms.IntrusionDetection.Api.Plugin;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.Agents.MailServer;

/// <summary>
/// Detects failed cleartext IMAP authentication attempts and stops parsing after STARTTLS succeeds.
/// </summary>
public sealed class ImapAgent : AgentPlugin
{
    private readonly ConcurrentDictionary<int, ImapSessionInspector> sessions = [];
    private readonly List<Sniffer> sniffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ImapAgent"/> class.
    /// </summary>
    public ImapAgent()
    {
        ImapConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }

    /// <inheritdoc />
    protected override void OnStartAgent()
    {
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress address in hostEntry.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
                WatchAddress(address);
        }
        base.OnStartAgent();
    }

    /// <inheritdoc />
    protected override void OnPauseAgent()
    {
        StopWatchers();
        base.OnPauseAgent();
    }

    /// <inheritdoc />
    protected override void OnContinueAgent()
    {
        OnStartAgent();
        base.OnContinueAgent();
    }

    /// <inheritdoc />
    protected override void OnStopAgent()
    {
        StopWatchers();
        base.OnStopAgent();
    }

    private void WatchAddress(IPAddress address)
    {
        if (Configuration.AgentSettings is not ImapConfig settings) return;
        Sniffer sniffer = new() { TcpPort = settings.ImapPort };
        sniffer.IpPacketReceived += ClientPacketReceived;
        sniffer.IpPacketSent += ServerPacketSent;
        try
        {
            sniffer.WatchAddress(address);
            sniffers.Add(sniffer);
        }
        catch (SocketException exception)
        {
            Sniffer.LogTrace(exception);
            sniffer.CloseSocket();
        }
        catch (UnauthorizedAccessException exception)
        {
            Sniffer.LogTrace(exception);
            sniffer.CloseSocket();
        }
    }

    private void ClientPacketReceived(object? sender, EventArgs e)
    {
        if (sender is not IPHeader packet || packet.ProtocolType != Protocol.Tcp) return;
        try
        {
            TCPHeader tcp = new(packet.Data, packet.MessageLength);
            if (!int.TryParse(tcp.SourcePort, out int clientPort) || tcp.Data.Length == 0) return;
            sessions.GetOrAdd(clientPort, static _ => new ImapSessionInspector()).ProcessClientData(tcp.Data);
        }
        catch (Exception exception)
        {
            Sniffer.LogTrace(exception);
        }
    }

    private void ServerPacketSent(object? sender, EventArgs e)
    {
        if (sender is not IPHeader packet || packet.ProtocolType != Protocol.Tcp) return;
        try
        {
            TCPHeader tcp = new(packet.Data, packet.MessageLength);
            if (!int.TryParse(tcp.DestinationPort, out int clientPort) || tcp.Data.Length == 0) return;
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
            Sniffer.LogTrace(exception);
        }
    }

    private void StopWatchers()
    {
        foreach (Sniffer sniffer in sniffers)
        {
            sniffer.Abort();
            sniffer.CloseSocket();
        }
        sniffers.Clear();
        sessions.Clear();
    }
}
