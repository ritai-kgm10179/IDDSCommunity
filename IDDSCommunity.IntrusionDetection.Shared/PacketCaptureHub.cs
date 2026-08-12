using System;
using System.Collections.Generic;
using System.Net;

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

        internal CaptureEntry(IPAddress address) => Address = address;

        internal IPAddress Address { get; }

        internal int SubscriptionCount => subscriptions.Count;

        internal void Start()
        {
            receiver.PacketReceived += OnPacketReceived;
            receiver.CaptureFailed += OnCaptureFailed;
            receiver.Start(Address);
        }

        internal void Add(PacketSubscription subscription) => subscriptions.Add(subscription);

        internal void Remove(PacketSubscription subscription) => subscriptions.Remove(subscription);

        public void Dispose()
        {
            receiver.PacketReceived -= OnPacketReceived;
            receiver.CaptureFailed -= OnCaptureFailed;
            receiver.Dispose();
        }

        private void OnPacketReceived(object? sender, RawPacketEventArgs eventArgs)
        {
            try
            {
                IPHeader ipHeader = new(eventArgs.Packet, eventArgs.Packet.Length);
                if (ipHeader.ProtocolType != Protocol.Tcp)
                    return;
                TCPHeader tcpHeader = new(ipHeader.Data, ipHeader.MessageLength);
                if (!int.TryParse(tcpHeader.SourcePort, out int sourcePort) || !int.TryParse(tcpHeader.DestinationPort, out int destinationPort))
                    return;

                PacketSubscription[] snapshot;
                lock (SyncRoot)
                    snapshot = [.. subscriptions];
                foreach (PacketSubscription subscription in snapshot)
                    subscription.Dispatch(ipHeader, tcpHeader, sourcePort, destinationPort, Address);
            }
            catch (Exception exception)
            {
                OnCaptureFailed(this, new RawSocketErrorEventArgs(exception));
            }
        }

        private void OnCaptureFailed(object? sender, RawSocketErrorEventArgs eventArgs)
        {
            PacketSubscription[] snapshot;
            lock (SyncRoot)
                snapshot = [.. subscriptions];
            foreach (PacketSubscription subscription in snapshot)
                subscription.NotifyFailure(eventArgs);
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

        internal void Dispatch(IPHeader ipHeader, TCPHeader tcpHeader, int sourcePort, int destinationPort, IPAddress localAddress)
        {
            if (disposed || (tcpPort is int port && sourcePort != port && destinationPort != port))
                return;
            if (ipHeader.SourceAddress.Equals(localAddress))
                packetSent(ipHeader, tcpHeader);
            if (ipHeader.DestinationAddress.Equals(localAddress))
                packetReceived(ipHeader, tcpHeader);
        }

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
}
