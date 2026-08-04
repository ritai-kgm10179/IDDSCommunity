using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;

namespace Cyberarms.Agents.FtpServer;

public class FtpAgent : AgentPlugin, IExtendedInformation
{
    public event EventHandler? Trace;
    public bool Tracing { get; set; }
    private ThreadStart? ts;
    private Thread? td;

    private readonly List<Sniffer> sniffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpAgent"/> class.
    /// </summary>

    public FtpAgent()
    {
        FtpConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
    }

    /// <summary>
    /// Processes the start agent notification.
    /// </summary>

    protected override void OnStartAgent()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts) { IsBackground = true };
        td.Start();
        base.OnStartAgent();
    }

    /// <summary>
    /// Executes the run watcher operation.
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

    private void WatchAddress(object? ipAddress)
    {
        if (ipAddress is not IPAddress address) return;
        Sniffer s = new();
        s.IpPacketSent += IpPacketSent;
        if (Configuration.AgentSettings is not FtpConfig settings) return;
        s.TcpPort = settings.FtpPort;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("Cyberarms.Agents.FtpServer", $"Ftp Server Security Agent is listening on port {s.TcpPort}");
        }
        catch { }
        try
        {
            s.WatchAddress(address);
        }
        catch { }
        sniffers.Add(s);
    }

    /// <summary>
    /// Handles the ip packet sent event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IpPacketSent(object? sender, EventArgs e)
    {
        if (sender is not IPHeader ipHeader) return;
        if (ipHeader.ProtocolType == Protocol.Tcp)
        {
            try
            {
                TCPHeader tcp = new(ipHeader.Data, ipHeader.MessageLength);
                if (int.TryParse(tcp.SourcePort, out int sourcePort))
                {
                    if (Configuration.AgentSettings is FtpConfig settings && sourcePort == settings.FtpPort)
                    {
                        if (Tracing)
                        {
                            OnTrace(ipHeader);
                        }
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerFtp ftp = new(tcp.Data, tcp.Data.Length);
                            if (ftp.FtpReplyCode == AppLayerFtp.FTP_REPLY_CODE_LOGIN_DENIED)
                            {
                                UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
                            }
                        }
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

    private void UnsuccessfulLogin(string ipAddress)
    {
        NotificationEventArgs args = new()
        {
            CreateDate = DateTime.Now,
            EventId = 9112,
            EventMessage = "FTP authentication failure",
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }

    public string DisplayName
    {
        get => "FTP Security Agent";
        set { }
    }

    public Image Icon
    {
        get => Resource.agent15px_ftp_dark;
        set { }
    }

    public Image SelectedIcon
    {
        get => Resource.agent15px_ftp_white;
        set { }
    }

    public Image UnselectedIcon
    {
        get => Resource.agent15px_ftp_dark;
        set { }
    }

    public Guid Id => new("{F040A37F-8A53-428E-85A3-EDC858144742}");
}
