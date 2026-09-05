using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

internal sealed class IpPrefixIndex
{
    private readonly Dictionary<int, HashSet<UInt128>> v4 = [];
    private readonly Dictionary<int, HashSet<UInt128>> v6 = [];
    internal int Count { get; }
    internal IpPrefixIndex(IEnumerable<IPNetwork> networks)
    {
        foreach (IPNetwork network in networks)
        {
            var map = network.BaseAddress.AddressFamily == AddressFamily.InterNetwork ? v4 : v6;
            if (!map.TryGetValue(network.PrefixLength, out var set)) map[network.PrefixLength] = set = [];
            if (set.Add(Number(network.BaseAddress))) Count++;
        }
    }
    internal bool Contains(IPAddress address)
    {
        bool ipv4 = address.AddressFamily == AddressFamily.InterNetwork;
        UInt128 value = Number(address);
        int bits = ipv4 ? 32 : 128;
        foreach (var entry in ipv4 ? v4 : v6)
        {
            UInt128 masked = entry.Key == 0 ? 0 : (value >> (bits - entry.Key)) << (bits - entry.Key);
            if (entry.Value.Contains(masked)) return true;
        }
        return false;
    }
    internal static UInt128 Number(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[16];
        address.TryWriteBytes(bytes, out int length);
        return length == 4 ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt128BigEndian(bytes);
    }
}