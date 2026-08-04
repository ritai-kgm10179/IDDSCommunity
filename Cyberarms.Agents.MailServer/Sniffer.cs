using System;
using System.Net;
using System.Net.Sockets;


namespace Cyberarms.Agents.MailServer;

public class Sniffer
{
    private Socket? ipSocket;
    private byte[] byteData = [];

    public event EventHandler? IpPacketReceived;
    public event EventHandler? IpPacketSent;

    private bool aborted = false;

    /// <summary>
    /// Executes the abort operation.
    /// </summary>

    public void Abort() => aborted = true;

    /// <summary>
    /// Executes the continue operation.
    /// </summary>

    public void Continue() => aborted = false;

    public int? TcpPort { get; set; }

    public IPAddress IPAddress { get; set; } = IPAddress.Loopback;

    /// <summary>
    /// Executes the watch address operation.
    /// </summary>
    /// <param name="ipAddressToMonitor">The ip address to monitor value.</param>

    public void WatchAddress(object ipAddressToMonitor)
    {
        byteData = new byte[128];
        IPAddress = (IPAddress)ipAddressToMonitor;
        ipSocket = new Socket(IPAddress.AddressFamily,
            SocketType.Raw, ProtocolType.IP);
        ipSocket.Bind(new IPEndPoint(IPAddress, TcpPort ?? 21));
        ipSocket.SetSocketOption(SocketOptionLevel.IP,
            SocketOptionName.HeaderIncluded,
            true);
        byte[] byTrue = [1, 0, 0, 0];
        byte[] byOut = [1, 0, 0, 0];  // capture outgoing packets
        ipSocket.IOControl(IOControlCode.ReceiveAll,
            byTrue, byOut);
        ipSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
            new AsyncCallback(OnReceive), null);

    }

    /// <summary>
    /// Processes the receive notification.
    /// </summary>
    /// <param name="ar">The ar value.</param>

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
                //ParseData(byteData, nReceived);
                IPHeader ipHeader = new(byteData, length);
                if (ipHeader.SourceAddress.Equals(IPAddress)) OnPacketSent(ipHeader);
                if (ipHeader.DestinationAddress.Equals(IPAddress)) OnPacketReceived(ipHeader);
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

    /// <summary>
    /// Processes the packet sent notification.
    /// </summary>
    /// <param name="ipHeader">The ip header value.</param>

    private void OnPacketSent(IPHeader ipHeader) => IpPacketSent?.Invoke(ipHeader, EventArgs.Empty);

    /// <summary>
    /// Processes the packet received notification.
    /// </summary>
    /// <param name="ipHeader">The ip header value.</param>

    private void OnPacketReceived(IPHeader ipHeader) => IpPacketReceived?.Invoke(ipHeader, EventArgs.Empty);


    /// <summary>
    /// Closes socket.
    /// </summary>

    public void CloseSocket() => ipSocket?.Close();

    /// <summary>
    /// Executes the log trace operation.
    /// </summary>
    /// <param name="ex">The exception associated with the operation.</param>

    public static void LogTrace(Exception ex)
    {
        System.IO.StreamWriter? sw = null;
        try
        {
            sw = System.IO.File.AppendText(System.IO.Path.GetTempPath() + "\\Cyberarms.Agents.MailServer.ErrorLog.txt");
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


