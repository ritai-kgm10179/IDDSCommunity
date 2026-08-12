using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Net;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 在同一個處理程序內共用每個 IPv4 位址的原始封包擷取器。
/// </summary>
internal static class PacketCaptureHub
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<IPAddress, CaptureEntry> Captures = [];

    internal static IDisposable Subscribe(IPAddress address, int? tcpPort, Action<IPHeader, TCPHeader> packetSent, Action<IPHeader, TCPHeader> packetReceived, Action<RawSocketErrorEventArgs> captureFailed)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(packetSent);
        ArgumentNullException.ThrowIfNull(packetReceived);
        ArgumentNullException.ThrowIfNull(captureFailed);

        lock (SyncRoot)
        {
            if (!Captures.TryGetValue(address, out CaptureEntry? entry))
            {
                entry = new CaptureEntry(address);
                Captures.Add(address, entry);
                try
                {
                    entry.Start();
                }
                catch
                {
                    Captures.Remove(address);
                    entry.Dispose();
                    throw;
                }
            }

            PacketSubscription subscription = new(entry, tcpPort, packetSent, packetReceived, captureFailed);
            entry.Add(subscription);
            return subscription;
        }
    }

    private static void Unsubscribe(CaptureEntry entry, PacketSubscription subscription)
    {
        lock (SyncRoot)
        {
            entry.Remove(subscription);
            if (entry.SubscriptionCount != 0)
                return;
            Captures.Remove(entry.Address);
            entry.Dispose();
        }
    }

    private sealed class CaptureEntry : IDisposable
    {
        private readonly RawSocketReceiver receiver = new();
        private readonly List<PacketSubscription> subscriptions = [];
        private PacketSubscription[] subscriptionSnapshot = [];

        internal CaptureEntry(IPAddress address) => Address = address;

        internal IPAddress Address { get; }

        internal int SubscriptionCount => subscriptions.Count;

        internal void Start()
        {
            receiver.PacketReceived += OnPacketReceived;
            receiver.CaptureFailed += OnCaptureFailed;
            receiver.Start(Address);
        }

        internal void Add(PacketSubscription subscription)
        {
            subscriptions.Add(subscription);
            Volatile.Write(ref subscriptionSnapshot, [.. subscriptions]);
        }

        internal void Remove(PacketSubscription subscription)
        {
            subscriptions.Remove(subscription);
            Volatile.Write(ref subscriptionSnapshot, [.. subscriptions]);
        }

        public void Dispose()
        {
            receiver.PacketReceived -= OnPacketReceived;
            receiver.CaptureFailed -= OnCaptureFailed;
            receiver.Dispose();
        }

        private void OnPacketReceived(object? sender, RawPacketEventArgs eventArgs)
        {
            if (eventArgs.Packet.Length >= 10 && eventArgs.Packet[9] != (byte)Protocol.Tcp)
                return;
            if (!TryReadRoute(eventArgs.Packet, out PacketRoute route))
            {
                IDDSCommunityMetrics.RecordMalformed();
                return;
            }
            PacketSubscription[] snapshot = Volatile.Read(ref subscriptionSnapshot);
            bool matched = false;
            foreach (PacketSubscription subscription in snapshot)
                matched |= subscription.Matches(route.SourcePort, route.DestinationPort);
            if (!matched)
                return;

            if (!IPHeader.TryParse(eventArgs.Packet, eventArgs.Packet.Length, out IPHeader? parsedIpHeader) || parsedIpHeader is not IPHeader ipHeader ||
                ipHeader.ProtocolType != Protocol.Tcp || !TCPHeader.TryParse(ipHeader.Payload, out TCPHeader? parsedTcpHeader) || parsedTcpHeader is not TCPHeader tcpHeader)
            {
                IDDSCommunityMetrics.RecordMalformed();
                return;
            }

            foreach (PacketSubscription subscription in snapshot)
                subscription.Dispatch(ipHeader, tcpHeader, tcpHeader.SourcePortValue, tcpHeader.DestinationPortValue, Address);
        }

        private void OnCaptureFailed(object? sender, RawSocketErrorEventArgs eventArgs)
        {
            PacketSubscription[] snapshot = Volatile.Read(ref subscriptionSnapshot);
            foreach (PacketSubscription subscription in snapshot)
                subscription.NotifyFailure(eventArgs);
        }

        private static bool TryReadRoute(byte[] packet, out PacketRoute route)
        {
            route = default;
            if (packet.Length < 40 || packet[0] >> 4 != 4 || packet[9] != (byte)Protocol.Tcp)
                return false;
            int ipHeaderLength = (packet[0] & 0x0F) * 4;
            ushort totalLength = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2));
            if (ipHeaderLength < 20 || totalLength < ipHeaderLength + 20 || totalLength > packet.Length)
                return false;
            ReadOnlySpan<byte> tcp = packet.AsSpan(ipHeaderLength);
            int tcpHeaderLength = (tcp[12] >> 4) * 4;
            if (tcpHeaderLength < 20 || ipHeaderLength + tcpHeaderLength > totalLength)
                return false;
            route = new PacketRoute(BinaryPrimitives.ReadUInt16BigEndian(tcp), BinaryPrimitives.ReadUInt16BigEndian(tcp[2..]));
            return true;
        }
    }

    private sealed class PacketSubscription : IDisposable
    {
        private readonly CaptureEntry owner;
        private readonly int? tcpPort;
        private readonly Action<IPHeader, TCPHeader> packetSent;
        private readonly Action<IPHeader, TCPHeader> packetReceived;
        private readonly Action<RawSocketErrorEventArgs> captureFailed;
        private bool disposed;

        internal PacketSubscription(CaptureEntry owner, int? tcpPort, Action<IPHeader, TCPHeader> packetSent, Action<IPHeader, TCPHeader> packetReceived, Action<RawSocketErrorEventArgs> captureFailed)
        {
            this.owner = owner;
            this.tcpPort = tcpPort;
            this.packetSent = packetSent;
            this.packetReceived = packetReceived;
            this.captureFailed = captureFailed;
        }

        internal void Dispatch(IPHeader ipHeader, TCPHeader tcpHeader, ushort sourcePort, ushort destinationPort, IPAddress localAddress)
        {
            if (disposed || !Matches(sourcePort, destinationPort))
                return;
            if (ipHeader.SourceAddress.Equals(localAddress))
                packetSent(ipHeader, tcpHeader);
            if (ipHeader.DestinationAddress.Equals(localAddress))
                packetReceived(ipHeader, tcpHeader);
        }

        internal bool Matches(ushort sourcePort, ushort destinationPort) => tcpPort is not int port || sourcePort == port || destinationPort == port;

        internal void NotifyFailure(RawSocketErrorEventArgs eventArgs)
        {
            if (!disposed)
                captureFailed(eventArgs);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Unsubscribe(owner, this);
        }
    }

    private readonly record struct PacketRoute(ushort SourcePort, ushort DestinationPort);
}
