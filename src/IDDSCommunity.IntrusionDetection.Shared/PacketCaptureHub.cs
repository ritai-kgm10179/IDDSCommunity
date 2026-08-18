using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 在同一個處理程序內共用每個 IPv4 位址的原始封包擷取器。
/// </summary>
internal static class PacketCaptureHub
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<IPAddress, CaptureEntry> Captures = [];

    internal static string BuildPcapFilter(IPAddress address, IEnumerable<int?> configuredPorts)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(configuredPorts);
        int[] ports = configuredPorts.OfType<int>().Distinct().Order().ToArray();
        string portFilter = ports.Length == 0 ? "tcp" : $"tcp and ({string.Join(" or ", ports.Select(port => string.Create(CultureInfo.InvariantCulture, $"port {port}")))})";
        return string.Create(CultureInfo.InvariantCulture, $"ip and host {address} and {portFilter}");
    }

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
                PacketSubscription firstSubscription = new(entry, tcpPort, packetSent, packetReceived, captureFailed);
                entry.Add(firstSubscription);
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
                return firstSubscription;
            }

            PacketSubscription subscription = new(entry, tcpPort, packetSent, packetReceived, captureFailed);
            entry.Add(subscription);
            entry.RestartForFilterChange();
            return subscription;
        }
    }

    private static void Unsubscribe(CaptureEntry entry, PacketSubscription subscription)
    {
        lock (SyncRoot)
        {
            entry.Remove(subscription);
            if (entry.SubscriptionCount != 0)
            {
                try
                {
                    entry.RestartForFilterChange();
                }
                catch (Exception exception)
                {
                    Trace.TraceWarning("Packet capture filter refresh failed on {0}: {1}", entry.Address, exception.Message);
                }
                return;
            }
            Captures.Remove(entry.Address);
            entry.Dispose();
        }
    }

    private sealed class CaptureEntry : IDisposable
    {
        private IPacketCaptureReceiver? receiver;
        private readonly List<PacketSubscription> subscriptions = [];
        private PacketSubscription[] subscriptionSnapshot = [];

        internal CaptureEntry(IPAddress address)
        {
            Address = address;
        }

        internal IPAddress Address { get; }

        internal int SubscriptionCount => subscriptions.Count;

        internal void Start()
        {
            receiver = CreatePreferredReceiver();
            AttachReceiver(receiver);
            try
            {
                receiver.Start(Address);
            }
            catch (Exception exception) when (receiver is SharpPcapPacketReceiver)
            {
                DetachAndDisposeReceiver(receiver);
                Trace.TraceWarning("Pcap capture is unavailable on {0}; using Raw Socket fallback: {1}", Address, exception.Message);
                receiver = new RawSocketReceiver(packetFilter: ShouldCapture);
                AttachReceiver(receiver);
                receiver.Start(Address);
            }
        }

        internal void RestartForFilterChange()
        {
            StopReceiver();
            Start();
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

        /// <summary>
        /// 釋放封包擷取中心及其內部接收器所佔用之資源。
        /// </summary>
        public void Dispose()
        {
            StopReceiver();
        }

        private IPacketCaptureReceiver CreatePreferredReceiver()
        {
            if (SharpPcapPacketReceiver.TryCreate(Address, PacketCaptureHub.BuildPcapFilter(Address, subscriptionSnapshot.Select(subscription => subscription.TcpPort)), out SharpPcapPacketReceiver? sharpPcapReceiver))
                return sharpPcapReceiver!;
            return new RawSocketReceiver(packetFilter: ShouldCapture);
        }

        private void StopReceiver()
        {
            if (receiver is null) return;
            DetachAndDisposeReceiver(receiver);
            receiver = null;
        }

        private void AttachReceiver(IPacketCaptureReceiver captureReceiver)
        {
            captureReceiver.PacketReceived += OnPacketReceived;
            captureReceiver.CaptureFailed += OnCaptureFailed;
        }

        private void DetachAndDisposeReceiver(IPacketCaptureReceiver captureReceiver)
        {
            captureReceiver.PacketReceived -= OnPacketReceived;
            captureReceiver.CaptureFailed -= OnCaptureFailed;
            captureReceiver.Dispose();
        }

        private void OnPacketReceived(object? sender, RawPacketEventArgs eventArgs)
        {
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

        private bool ShouldCapture(ReadOnlySpan<byte> packet)
        {
            if (!TryReadRoute(packet, out PacketRoute route))
                return false;
            PacketSubscription[] snapshot = Volatile.Read(ref subscriptionSnapshot);
            foreach (PacketSubscription subscription in snapshot)
                if (subscription.Matches(route.SourcePort, route.DestinationPort)) return true;
            return false;
        }

        private void OnCaptureFailed(object? sender, RawSocketErrorEventArgs eventArgs)
        {
            if (sender is SharpPcapPacketReceiver)
            {
                // 改由執行緒集區派發，避免在觸發此事件的 SharpPcap 擷取執行緒本身同步呼叫
                // StopReceiver()/Dispose()，該執行緒可能仍身處其自身的擷取回呼堆疊中，
                // 直接於原執行緒關閉裝置有自我等待（self-join）而卡住復原流程的風險。
                ThreadPool.QueueUserWorkItem(_ => HandleSharpPcapFailure(eventArgs));
                return;
            }
            PacketSubscription[] snapshot = Volatile.Read(ref subscriptionSnapshot);
            foreach (PacketSubscription subscription in snapshot)
                subscription.NotifyFailure(eventArgs);
        }

        private void HandleSharpPcapFailure(RawSocketErrorEventArgs eventArgs)
        {
            lock (SyncRoot)
            {
                try
                {
                    StopReceiver();
                    receiver = new RawSocketReceiver(packetFilter: ShouldCapture);
                    AttachReceiver(receiver);
                    receiver.Start(Address);
                    Trace.TraceWarning("Pcap capture failed on {0}; Raw Socket fallback is active: {1}", Address, eventArgs.Exception.Message);
                    return;
                }
                catch (Exception fallbackException)
                {
                    eventArgs = new RawSocketErrorEventArgs(new AggregateException(eventArgs.Exception, fallbackException));
                }
            }
            PacketSubscription[] snapshot = Volatile.Read(ref subscriptionSnapshot);
            foreach (PacketSubscription subscription in snapshot)
                subscription.NotifyFailure(eventArgs);
        }

        private static bool TryReadRoute(ReadOnlySpan<byte> packet, out PacketRoute route)
        {
            route = default;
            if (packet.Length < 40 || packet[0] >> 4 != 4 || packet[9] != (byte)Protocol.Tcp)
                return false;
            int ipHeaderLength = (packet[0] & 0x0F) * 4;
            ushort totalLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
            if (ipHeaderLength < 20 || totalLength < ipHeaderLength + 20 || totalLength > packet.Length)
                return false;
            ReadOnlySpan<byte> tcp = packet[ipHeaderLength..];
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
        private volatile bool disposed;

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

        internal int? TcpPort => tcpPort;

        internal void NotifyFailure(RawSocketErrorEventArgs eventArgs)
        {
            if (!disposed)
                captureFailed(eventArgs);
        }

        /// <summary>
        /// 釋放封包擷取中心及其內部接收器所佔用之資源。
        /// </summary>
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
