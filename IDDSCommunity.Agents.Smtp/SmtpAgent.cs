using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.Agents.Smtp;

public class SmtpAgent : AgentPlugin, IExtendedInformation
{
    public event EventHandler? Trace;
    public bool Tracing { get; set; }

    readonly List<Sniffer> sniffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpAgent"/> class.
    /// </summary>

    public SmtpAgent()
    {
        SmtpConfig settings = new();
        Configuration.AgentSettings = settings;
        Configuration.ConfigurationSettingsTypeName = settings.GetType().FullName ?? string.Empty;
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
        if (ipAddress is not IPAddress address || Configuration.AgentSettings is not SmtpConfig settings) return;
        Sniffer s = new();
        // s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = settings.SmtpPort;
        System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.Agents.SmtpServer", string.Format("Smtp Server Security Agent is listening on port {0}", s.TcpPort));
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
                    if (Configuration.AgentSettings is SmtpConfig settings && sourcePort == settings.SmtpPort)
                    {
                        if (Tracing)
                        {
                            OnTrace(ipHeader);
                        }
                        if (tcp.Data.Length > 0)
                        {
                            AppLayerSmtp ftp = new(tcp.Data, tcp.Data.Length);
                            if (ftp.SmtpReplyCode == AppLayerSmtp.SMTP_REPLY_CODE_NEED_TO_AUTHENTICATE || ftp.SmtpReplyCode == AppLayerSmtp.SMTP_REPLY_CODE_LOGIN_DENIED)
                            {
                                UnsuccessfulLogin(ipHeader.DestinationAddress.ToString());
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
            EventMessage = Strings.Get("SMTP authentication failure"),
            IpAddress = ipAddress
        };
        OnAttackDetected(this, args);
    }


    public string DisplayName
    {
        get => "SMTP Security Agent";
        set
        {

        }
    }

    public Image Icon
    {
        get => Smtp.Resource.agent15px_mail_dark;
        set
        {

        }
    }

    public Image SelectedIcon
    {
        get => Smtp.Resource.agent15px_mail_white;
        set
        {

        }
    }

    public Image UnselectedIcon
    {
        get => Smtp.Resource.agent15px_mail_dark;
        set
        {

        }
    }



    public Guid Id => new("{EB69BF23-939C-4F89-97D0-50274306D018}");

}
