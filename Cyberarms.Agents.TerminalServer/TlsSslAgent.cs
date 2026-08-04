using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Threading;
using System.Drawing;

namespace Cyberarms.Agents.TerminalServer;

public class TlsSslAgent : AgentPlugin, IExtendedInformation
{
    public event EventHandler? Trace;
    public bool Tracing { get; set; }
    private ThreadStart? ts;
    private Thread? td;

    readonly List<Sniffer> sniffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TlsSslAgent"/> class.
    /// </summary>

    public TlsSslAgent()
    {
        TslSslConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }

    /// <summary>
    /// Processes the start agent notification.
    /// </summary>

    protected override void OnStartAgent()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts);
        td.Start();
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
                    ParameterizedThreadStart pts = new(WatchAddress);
                    pts.Invoke(ip);
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
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not TslSslConfig settings) return;
        Sniffer s = new();
        // s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = settings.RdpPort;
        System.Diagnostics.EventLog.WriteEntry("Cyberarms.Agents.TlsSslAgent", string.Format("Remote Desktop Security Agent is listening on port {0}", s.TcpPort));
        s.WatchAddress(address);
        sniffers.Add(s);
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
                Sniffer.LogTrace(ex);
            }

        }
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
        Start();
        base.OnContinueAgent();
    }

    /// <summary>
    /// Processes the pause agent notification.
    /// </summary>

    protected override void OnPauseAgent()
    {
        Stop();
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
            EventMessage = "Remote desktop connection TLS/SSL authentication failure",
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }


    public string DisplayName
    {
        get => "TLS/SSL Security Agent";
        set
        {

        }
    }

    public Image Icon
    {
        get => Resource.agent15px_rdp_dark;
        set
        {

        }
    }

    public Image SelectedIcon
    {
        get => Resource.agent15px_rdp_white;
        set
        {

        }
    }

    public Image UnselectedIcon
    {
        get => Resource.agent15px_rdp_dark;
        set
        {

        }
    }


    public Guid Id => new("{A682433B-852F-4150-ADF4-FB7F75090015}");
}
