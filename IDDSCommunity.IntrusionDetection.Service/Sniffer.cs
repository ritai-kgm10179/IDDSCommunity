using System;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service;

public class Sniffer
{

    private readonly RawSocketReceiver receiver = new();

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
            if (ipAddressToMonitor is not IPAddress address)
                throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("An IP address is required."), nameof(ipAddressToMonitor));
            IPAddress = address;
            receiver.PacketReceived += OnReceive;
            receiver.Start(address);
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

    private void OnReceive(object? sender, RawPacketEventArgs e)
    {
        if (!isPaused)
        {
            try
            {
                byte[] packet = e.Packet;
                IPHeader ipHeader = new(packet, packet.Length);
                if (ipHeader.SourceAddress.Equals(IPAddress)) OnPacketSent(ipHeader);
                if (ipHeader.DestinationAddress.Equals(IPAddress)) OnPacketReceived(ipHeader);
            }
            catch (Exception ex)
            {
                LogTrace(ex);
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

    public void CloseSocket()
    {
        receiver.PacketReceived -= OnReceive;
        receiver.Stop();
    }

    /// <summary>
    /// Executes the log trace operation.
    /// </summary>
    /// <param name="ex">The exception associated with the operation.</param>

    public static void LogTrace(Exception ex)
    {
        System.IO.StreamWriter? sw = null;
        try
        {
            sw = System.IO.File.AppendText(System.IO.Path.GetTempPath() + "\\IDDSCommunity.IntrusionDetection.Sniffer.ErrorLog.txt");
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


