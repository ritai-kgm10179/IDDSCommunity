using System;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared;


namespace IDDSCommunity.Agents.TerminalServer;

public class Sniffer
{
    private readonly RawSocketReceiver receiver = new();

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
        try
        {
            IPAddress = (IPAddress)ipAddressToMonitor;
            receiver.PacketReceived += OnReceive;
            receiver.Start(IPAddress);
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
        if (!aborted)
        {
            try
            {
                byte[] packet = e.Packet;
                IPHeader ipHeader = new(packet, packet.Length);
                if (ipHeader.SourceAddress.Equals(IPAddress)) OnPacketSent(ipHeader);
                // if (ipHeader.DestinationAddress.Equals(IPAddress)) OnPacketReceived(ipHeader);
                // OnPacketReceived(new NetworkPacket(byteData,length));


            }
            catch (Exception)
            {
                // Sniffer.LogTrace(ex);
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
            sw = System.IO.File.AppendText(System.IO.Path.GetTempPath() + "\\IDDSCommunity.Agents.TerminalServer.ErrorLog.txt");
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


