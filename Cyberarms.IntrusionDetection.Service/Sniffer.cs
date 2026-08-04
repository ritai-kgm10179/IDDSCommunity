using System;
using System.Net;
using System.Net.Sockets;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

public class Sniffer
{

    private Socket? ipSocket;
    private byte[] byteData = [];

    public event EventHandler? IpPacketReceived;
    public event EventHandler? IpPacketSent;
    public event EventHandler? TcpPacketReceived;
    public event EventHandler? TcpPacketSent;


    private bool isPaused = false;

    /// <summary>
    /// Executes the pause operation.
    /// </summary>

    public void Pause() => isPaused = true;

    /// <summary>
    /// Executes the continue operation.
    /// </summary>

    public void Continue() => isPaused = false;


    public IPAddress IPAddress { get; set; } = IPAddress.Loopback;

    /// <summary>
    /// Executes the watch address operation.
    /// </summary>
    /// <param name="ipAddressToMonitor">The ip address to monitor value.</param>

    public void WatchAddress(object? ipAddressToMonitor)
    {
        try
        {
            byteData = new byte[128];
            if (ipAddressToMonitor is not IPAddress address)
                throw new ArgumentException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("An IP address is required."), nameof(ipAddressToMonitor));
            IPAddress = address;
            ipSocket = new Socket(IPAddress.AddressFamily,
                SocketType.Raw, ProtocolType.IP)
            {
                ExclusiveAddressUse = false
            };
            ipSocket.Bind(new IPEndPoint(IPAddress, 0));
            ipSocket.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.HeaderIncluded,
                true);
            byte[] byTrue = [3, 0, 0, 0];
            byte[] byOut = [3, 0, 0, 0];  // capture outgoing packets
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

    /// <summary>
    /// Processes the receive notification.
    /// </summary>
    /// <param name="ar">The ar value.</param>

    private void OnReceive(IAsyncResult ar)
    {
        if (!isPaused)
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
                IPHeader ipHeader = new(packet, length);
                if (ipHeader.SourceAddress.Equals(IPAddress)) OnPacketSent(ipHeader);
                if (ipHeader.DestinationAddress.Equals(IPAddress)) OnPacketReceived(ipHeader);
            }
            catch (Exception ex)
            {
                LogTrace(ex);
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

    private void OnPacketSent(IPHeader ipHeader)
    {
        IpPacketSent?.Invoke(ipHeader, EventArgs.Empty);
        TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);
        TcpPacketSent?.Invoke(tcpHeader, EventArgs.Empty);
    }

    /// <summary>
    /// Processes the packet received notification.
    /// </summary>
    /// <param name="ipHeader">The ip header value.</param>

    private void OnPacketReceived(IPHeader ipHeader)
    {
        IpPacketReceived?.Invoke(ipHeader, EventArgs.Empty);
        TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);
        TcpPacketReceived?.Invoke(tcpHeader, EventArgs.Empty);
    }


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
            sw = System.IO.File.AppendText(System.IO.Path.GetTempPath() + "\\Cyberarms.IntrusionDetection.Sniffer.ErrorLog.txt");
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


