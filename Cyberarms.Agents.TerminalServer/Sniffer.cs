using System;
using System.Net;
using System.Net.Sockets;


namespace Cyberarms.Agents.TerminalServer;

public class Sniffer
{
    private Socket? ipSocket;
    private byte[] byteData = [];

    public event EventHandler? IpPacketReceived;
    public event EventHandler? IpPacketSent;

    private bool aborted = false;

    public void Abort() => aborted = true;

    public void Continue() => aborted = false;

    public int? TcpPort { get; set; }

    public IPAddress IPAddress { get; set; } = IPAddress.Loopback;

    public void WatchAddress(object ipAddressToMonitor)
    {
        try
        {
            byteData = new byte[128];
            IPAddress = (IPAddress)ipAddressToMonitor;
            ipSocket = new Socket(IPAddress.AddressFamily,
                SocketType.Raw, ProtocolType.IP)
            {
                ExclusiveAddressUse = false
            };
            ipSocket.Bind(new IPEndPoint(IPAddress, TcpPort ?? 3389));
            ipSocket.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.HeaderIncluded,
                true);
            byte[] byTrue = [3, 0, 0, 0];
            byte[] byOut = [1, 0, 0, 0];  // capture outgoing packets
            ipSocket.IOControl(IOControlCode.ReceiveAll,
                byTrue, byOut);

            ipSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(OnReceive), null);
        }
        catch (Exception ex)
        {
            LogTrace(ex);
        }

    }

    private void OnReceive(IAsyncResult ar)
    {
        if (!aborted)
        {
            try
            {
                if (ipSocket is null)
                {
                    return;
                }

                int length = ipSocket.EndReceive(ar);
                byte[] packet = new byte[length];
                Array.Copy(byteData, 0, packet, 0, length);
                //ParseData(byteData, nReceived);
                IPHeader ipHeader = new(packet, length);
                if (ipHeader.SourceAddress.Equals(IPAddress)) OnPacketSent(ipHeader);
                // if (ipHeader.DestinationAddress.Equals(IPAddress)) OnPacketReceived(ipHeader);
                // OnPacketReceived(new NetworkPacket(byteData,length));


            }
            catch (Exception)
            {
                // Sniffer.LogTrace(ex);
            }
            finally
            {
                byteData = new byte[128];          // set to 16276 bytes
                // continue receiving
                ipSocket?.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), null);
            }
        }
    }

    private void OnPacketSent(IPHeader ipHeader) => IpPacketSent?.Invoke(ipHeader, EventArgs.Empty);

    private void OnPacketReceived(IPHeader ipHeader) => IpPacketReceived?.Invoke(ipHeader, EventArgs.Empty);


    public void CloseSocket() => ipSocket?.Close();

    public static void LogTrace(Exception ex)
    {
        System.IO.StreamWriter? sw = null;
        try
        {
            sw = System.IO.File.AppendText(System.IO.Path.GetTempPath() + "\\Cyberarms.Agents.TerminalServer.ErrorLog.txt");
            sw.WriteLine(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            sw.Flush();
        }
        catch { }
        finally
        {
            sw?.Close();
        }
    }

}


