using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.Agents.MailServer;

public class Pop3Agent : AgentPlugin
{
    public const int CLEANUP_INTERVAL_MINS = 2;
    public event EventHandler? Trace;
    public bool Tracing { get; set; }
    public System.Timers.Timer cleanupTimer;

    readonly List<Sniffer> sniffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Pop3Agent"/> class.
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
    /// Handles the elapsed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void cleanupTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        //for (int i = CurrentClients.Keys.Max(); i > 0; i--) {
        //    if (CurrentClients.ContainsKey(i) && CurrentClients[i].LastInteraction.AddMinutes(CLEANUP_INTERVAL_MINS) < DateTime.Now) CurrentClients.Remove(i);
        //}
        foreach (int key in CurrentClients.Keys)
        {
            if (CurrentClients.TryGetValue(key, out Pop3Client? client) && client.LastInteraction.AddMinutes(CLEANUP_INTERVAL_MINS) < DateTime.Now)
                _currentClients.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Processes the start agent notification.
    /// </summary>

    protected override void OnStartAgent()
    {
        RunWatcher();
        base.OnStartAgent();
    }

    /// <summary>
    /// Executes the run watcher operation.
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
    /// Executes the watch address operation.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

    void WatchAddress(object? ipAddress)
    {
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not Pop3Config settings) return;
        Sniffer s = new();
        s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = settings.Pop3Port;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("Cyberarms.Agents.MailServer", string.Format("POP3 Server Security Agent is listening on port {0}", s.TcpPort));
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
    /// Executes the test receive operation.
    /// </summary>
    /// <param name="data">The data value.</param>

    public void TestReceive(byte[] data)
    {
        IPHeader hdr = new(data, data.Length);
        s_IpPacketReceived(hdr, EventArgs.Empty);
    }

    /// <summary>
    /// Executes the test send operation.
    /// </summary>
    /// <param name="data">The data value.</param>

    public void TestSend(byte[] data)
    {
        IPHeader hdr = new(data, data.Length);
        s_IpPacketSent(hdr, EventArgs.Empty);
    }

    /// <summary>
    /// Handles the ip packet received event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
                Sniffer.LogTrace(ex);
            }

        }
    }

    /// <summary>
    /// Handles the ip packet sent event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
                Sniffer.LogTrace(ex);
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
    /// Processes the trace notification.
    /// </summary>
    /// <param name="tlsPackage">The tls package value.</param>

    private void OnTrace(IPHeader tlsPackage) => Trace?.Invoke(tlsPackage, EventArgs.Empty);

    /// <summary>
    /// Processes the continue agent notification.
    /// </summary>

    protected override void OnContinueAgent()
    {
        OnStartAgent();
        base.OnContinueAgent();
    }

    /// <summary>
    /// Processes the pause agent notification.
    /// </summary>

    protected override void OnPauseAgent()
    {
        OnStopAgent();
        base.OnPauseAgent();
    }

    /// <summary>
    /// Processes the stop agent notification.
    /// </summary>

    protected override void OnStopAgent()
    {
        foreach (Sniffer s in sniffers)
        {
            s.Abort();
            s.CloseSocket();
        }
        sniffers.Clear();
        base.OnStopAgent();
    }

    public override bool IsRunning => base.IsRunning;

    /// <summary>
    /// Executes the unsuccessful login operation.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

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


    public static Guid Id => new("{1F917251-2661-473A-970B-B2BB62EA6E1A}");

}
