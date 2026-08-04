using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Cyberarms.Agents.MailServer;

public class SmtpAgent : AgentPlugin
{
    public event EventHandler? Trace;
    public bool Tracing { get; set; }
    private ThreadStart? ts;
    private Thread? td;

    private readonly List<Sniffer> sniffers = [];

    public SmtpAgent()
    {
        Configuration.AgentSettings = new SmtpConfig();
        Configuration.ConfigurationSettingsTypeName = Configuration.AgentSettings.GetType().FullName ?? string.Empty;
    }

    protected override void OnStartAgent()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts) { IsBackground = true };
        td.Start();
        base.OnStartAgent();
    }

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

    private void WatchAddress(object? ipAddress)
    {
        if (ipAddress is not IPAddress address) return;
        Sniffer s = new();
        s.IpPacketSent += s_IpPacketSent;
        s.TcpPort = ((SmtpConfig)Configuration.AgentSettings).SmtpPort;
        try
        {
            System.Diagnostics.EventLog.WriteEntry("Cyberarms.Agents.SmtpServer", $"Smtp Server Security Agent is listening on port {s.TcpPort}");
        }
        catch { }
        try
        {
            s.WatchAddress(address);
        }
        catch { }
        sniffers.Add(s);
    }

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
                    if (sourcePort == ((SmtpConfig)Configuration.AgentSettings).SmtpPort)
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

    private void UnsuccessfulLogin(string ipAddress)
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

    public Guid Id => new("{EB69BF23-939C-4F89-97D0-50274306D018}");
}
