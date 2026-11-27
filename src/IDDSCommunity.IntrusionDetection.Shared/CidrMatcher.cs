using System;
using System.Net;
using System.Net.Sockets;

namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// 提供標準 IPv4 CIDR 網段解析與匹配比對。
/// </summary>
public static class CidrMatcher
{
    /// <summary>
    /// 嘗試解析 CIDR 字串（如 192.168.1.0/24）並判斷目標 IP 是否落在該網段範圍內。
    /// </summary>
    /// <param name="cidrCandidate">CIDR 字串 candidate。</param>
    /// <param name="targetIp">欲比對的目標 IP 字串。</param>
    /// <returns>若符合 CIDR 範圍則傳回 true，否則傳回 false。</returns>
    public static bool TryMatchCidr(string cidrCandidate, string targetIp)
    {
        if (string.IsNullOrWhiteSpace(cidrCandidate) || string.IsNullOrWhiteSpace(targetIp)) return false;
        ReadOnlySpan<char> span = cidrCandidate.AsSpan();
        int slashIndex = span.IndexOf('/');
        if (slashIndex <= 0 || span[(slashIndex + 1)..].Contains('/')) return false;
        if (!IPAddress.TryParse(span[..slashIndex].Trim(), out IPAddress? networkIp) ||
            !int.TryParse(span[(slashIndex + 1)..].Trim(), out int maskBits)) return false;
        if (networkIp.AddressFamily != AddressFamily.InterNetwork) return false;
        if (!IPAddress.TryParse(targetIp.AsSpan().Trim(), out IPAddress? target)) return false;
        if (target.AddressFamily != AddressFamily.InterNetwork) return false;

        uint networkInt = IpToUint(networkIp);
        uint targetInt = IpToUint(target);
        if (maskBits is < 0 or > 32) return false;
        uint mask = maskBits == 0 ? 0 : 0xFFFFFFFF << (32 - maskBits);

        return (networkInt & mask) == (targetInt & mask);
    }

    private static uint IpToUint(IPAddress ip)
    {
        Span<byte> bytes = stackalloc byte[4];
        ip.TryWriteBytes(bytes, out _);
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }
}
