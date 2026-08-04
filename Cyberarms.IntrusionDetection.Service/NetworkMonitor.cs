using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace Cyberarms.IntrusionDetection.Service;

public class NetworkMonitor
{
    ThreadStart? ts;
    Thread? td;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkMonitor"/> class.
    /// </summary>

    private NetworkMonitor()
    {
    }

    private static NetworkMonitor? _instance;
    public static NetworkMonitor Instance
    {
        get
        {
            _instance ??= new NetworkMonitor();
            return _instance;
        }

        set => _instance = new NetworkMonitor();
    }


    /// <summary>
    /// Starts network sniffer.
    /// </summary>

    protected void StartNetworkSniffer()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts) { IsBackground = true };
        td.Start();
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
                    Sniffer s = new();
                    foreach (TcpSnifferPort port in TcpSnifferPorts)
                    {
                        if (port.IPAddress == null || port.IPAddress != null && port.IPAddress.Equals(ip))
                        {
                            if (port.HandlesReceived)
                            {
                                s.IpPacketReceived += port.Received;
                            }
                            if (port.HandlesSent)
                            {
                                s.IpPacketSent += port.Sent;
                            }
                        }
                    }
                    ParameterizedThreadStart pts = new(s.WatchAddress);
                    pts.Invoke(ip);
                }
            }
        }
    }

    /// <summary>
    /// Handles the ip packet received event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void IpPacketReceived(object sender, EventArgs e) => throw new NotImplementedException();

    /// <summary>
    /// Adds sniffer address port.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="handlesReceived">The handles received value.</param>
    /// <param name="handlesSent">The handles sent value.</param>
    /// <param name="received">The received value.</param>
    /// <param name="sent">The sent value.</param>

    public void AddSnifferAddressPort(IPAddress address, int port, bool handlesReceived, bool handlesSent, EventHandler received, EventHandler sent) => TcpSnifferPorts.Add(new TcpSnifferPort(address, port, handlesReceived, handlesSent, received, sent));

    private List<TcpSnifferPort>? _tcpSnifferPorts;
    private List<TcpSnifferPort> TcpSnifferPorts
    {
        get
        {
            _tcpSnifferPorts ??= [];
            return _tcpSnifferPorts;
        }

        set => _tcpSnifferPorts = value;
    }

    private class TcpSnifferPort(IPAddress ipaddress, int port, bool handlesReceived, bool handlesSent, EventHandler received, EventHandler sent)
    {
        public IPAddress IPAddress = ipaddress;
        public int Port = port;
        public bool HandlesReceived = handlesReceived;
        public bool HandlesSent = handlesSent;
        public EventHandler Received = received;
        public EventHandler Sent = sent;
    }

}
