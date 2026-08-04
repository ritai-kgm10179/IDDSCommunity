using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Threading;
using System.Drawing;

namespace Cyberarms.Agents.Smtp;

public class SmtpAgent : AgentPlugin, IExtendedInformation
{
    public event EventHandler Trace;
    public bool Tracing { get; set; }
    ThreadStart ts;
    Thread td;

    readonly List<Sniffer> sniffers = [];

    public SmtpAgent()
    {
        Configuration.AgentSettings = new SmtpConfig();
        Configuration.ConfigurationSettingsTypeName =
            Configuration.AgentSettings.GetType().FullName;
    }

    protected override void OnStartAgent()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts);
        td.Start();
        base.OnStartAgent();
    }

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



    void WatchAddress(object ipAddress)
    {
        Sniffer s = new();
        // s.IpPacketReceived += new EventHandler(s_IpPacketReceived);
        s.IpPacketSent += new EventHandler(s_IpPacketSent);
        s.TcpPort = ((SmtpConfig)Configuration.AgentSettings).SmtpPort;
        System.Diagnostics.EventLog.WriteEntry("Cyberarms.Agents.SmtpServer", string.Format("Smtp Server Security Agent is listening on port {0}", s.TcpPort));
        s.WatchAddress((IPAddress)ipAddress);
        sniffers.Add(s);
    }

    void s_IpPacketSent(object sender, EventArgs e)
    {
        var ipHeader = (IPHeader)sender;
        if (ipHeader.ProtocolType == Protocol.Tcp)
        {
            try
            {
                TCPHeader tcp = new(ipHeader.Data, ipHeader.MessageLength);
                if (int.TryParse(tcp.SourcePort, out int sourcePort))
                {
                    if (sourcePort == ((SmtpConfig)Configuration.AgentSettings).SmtpPort)
                    {
                        if (Tracing)
                        {
                            OnTrace((IPHeader)sender);
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

    private void OnTrace(IPHeader tlsPackage) => Trace?.Invoke(tlsPackage, EventArgs.Empty);

    protected override void OnContinueAgent()
    {
        Start();
        base.OnContinueAgent();
    }

    protected override void OnPauseAgent()
    {
        Stop();
        base.OnPauseAgent();
    }

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

    void UnsuccessfulLogin(string ipAddress)
    {
        NotificationEventArgs args = new()
        {
            CreateDate = DateTime.Now,
            EventId = 9112,
            EventMessage = "SMTP authentication failure",
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
