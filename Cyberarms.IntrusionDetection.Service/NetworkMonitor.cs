using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace Cyberarms.IntrusionDetection.Service;

public class NetworkMonitor
{
    ThreadStart ts;
    Thread td;

    private NetworkMonitor()
    {
    }

    private static NetworkMonitor _instance;
    public static NetworkMonitor Instance
    {
        get
        {
            _instance ??= new NetworkMonitor();
            return _instance;
        }

        set => _instance = new NetworkMonitor();
    }


    protected void StartNetworkSniffer()
    {
        ts = new ThreadStart(RunWatcher);
        td = new Thread(ts);
        td.Start();
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

    void IpPacketReceived(object sender, EventArgs e) => throw new NotImplementedException();

    public void AddSnifferAddressPort(IPAddress address, int port, bool handlesReceived, bool handlesSent, EventHandler received, EventHandler sent) => TcpSnifferPorts.Add(new TcpSnifferPort(address, port, handlesReceived, handlesSent, received, sent));

    private List<TcpSnifferPort> _tcpSnifferPorts;
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
