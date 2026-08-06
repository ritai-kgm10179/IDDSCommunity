using System;
using System.Diagnostics;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供協定 Agent 使用的單一共用原始封包擷取轉接器。
/// </summary>
public sealed class PacketSniffer : IDisposable
{
    private readonly RawSocketReceiver receiver = new();
    private bool paused;

    public event EventHandler? IpPacketReceived;
    public event EventHandler? IpPacketSent;
    public event EventHandler<RawSocketErrorEventArgs>? CaptureFailed;

    public int? TcpPort { get; set; }
    public IPAddress IPAddress { get; private set; } = IPAddress.Loopback;

    public void WatchAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        IPAddress = address;
        receiver.PacketReceived += OnReceive;
        receiver.CaptureFailed += OnCaptureFailed;
        receiver.Start(address);
    }

    public void Abort() => paused = true;
    public void Continue() => paused = false;
    public void CloseSocket() => Dispose();

    public void Dispose()
    {
        receiver.PacketReceived -= OnReceive;
        receiver.CaptureFailed -= OnCaptureFailed;
        receiver.Dispose();
    }

    private void OnReceive(object? sender, RawPacketEventArgs eventArgs)
    {
        if (paused) return;
        try
        {
            IPHeader header = new(eventArgs.Packet, eventArgs.Packet.Length);
            if (header.ProtocolType != Protocol.Tcp) return;
            TCPHeader tcp = new(header.Data, header.MessageLength);
            if (TcpPort is int port &&
                (!int.TryParse(tcp.SourcePort, out int sourcePort) || !int.TryParse(tcp.DestinationPort, out int destinationPort) ||
                 (sourcePort != port && destinationPort != port))) return;

            if (header.SourceAddress.Equals(IPAddress)) IpPacketSent?.Invoke(header, EventArgs.Empty);
            if (header.DestinationAddress.Equals(IPAddress)) IpPacketReceived?.Invoke(header, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            OnCaptureFailed(this, new RawSocketErrorEventArgs(exception));
        }
    }

    private void OnCaptureFailed(object? sender, RawSocketErrorEventArgs eventArgs)
    {
        Trace.TraceError("Packet capture failed on {0}: {1}", IPAddress, eventArgs.Exception);
        CaptureFailed?.Invoke(this, eventArgs);
    }

    public static void LogTrace(Exception exception) => Trace.TraceError("Packet processing failed: {0}", exception);
}
