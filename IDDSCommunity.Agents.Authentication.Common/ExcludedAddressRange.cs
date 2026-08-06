using System;
using System.Net;
using System.Net.Sockets;

namespace IDDSCommunity.Agents.Authentication.Common;

internal readonly struct ExcludedAddressRange
{
    private readonly byte[] networkBytes;
    private readonly AddressFamily addressFamily;
    private readonly int prefixLength;

    private ExcludedAddressRange(IPAddress network, int prefixLength)
    {
        networkBytes = network.GetAddressBytes();
        addressFamily = network.AddressFamily;
        this.prefixLength = prefixLength;
    }

    internal static bool TryParse(string value, out ExcludedAddressRange range)
    {
        range = default;
        string[] parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || !IPAddress.TryParse(parts[0], out IPAddress? address)) return false;
        int maximumPrefix = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        int prefix = maximumPrefix;
        if (parts.Length == 2 && (!int.TryParse(parts[1], out prefix) || prefix < 0 || prefix > maximumPrefix)) return false;
        if (address.IsIPv4MappedToIPv6 && prefix >= 96)
        {
            address = address.MapToIPv4();
            prefix -= 96;
        }
        range = new ExcludedAddressRange(address, prefix);
        return true;
    }

    internal bool Contains(IPAddress address)
    {
        address = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        if (address.AddressFamily != addressFamily) return false;
        byte[] candidate = address.GetAddressBytes();
        int completeBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        if (!candidate.AsSpan(0, completeBytes).SequenceEqual(networkBytes.AsSpan(0, completeBytes))) return false;
        if (remainingBits == 0) return true;
        int mask = 0xFF << (8 - remainingBits);
        return (candidate[completeBytes] & mask) == (networkBytes[completeBytes] & mask);
    }
}
