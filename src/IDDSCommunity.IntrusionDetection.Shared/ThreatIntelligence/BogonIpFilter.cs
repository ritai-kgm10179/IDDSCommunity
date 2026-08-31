using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供符合 RFC 8704、RFC 1918、RFC 6598、RFC 5737、RFC 3849、IANA 與 Team Cymru Fullbogons 標準之雙層（靜態硬編碼 + 動態前綴快取）Bogon、私有、多播、迴路與未分配 IP 區段過濾器。
/// </summary>
public static class BogonIpFilter
{
    private static volatile IPNetwork[] dynamicBogons = [];

    /// <summary>
    /// 取得目前已載入之動態 Bogon 網段前綴數量。
    /// </summary>
    public static int DynamicBogonCount => dynamicBogons.Length;

    /// <summary>
    /// 更新動態 Bogon 網段前綴快取（原子替換）。
    /// </summary>
    /// <param name="networks">新載入之 IPNetwork 清單。</param>
    public static void UpdateDynamicBogons(IEnumerable<IPNetwork> networks)
    {
        if (networks is null)
        {
            dynamicBogons = [];
            return;
        }

        dynamicBogons = networks.ToArray();
    }

    /// <summary>
    /// 清除所有動態載入之 Bogon 網段前綴。
    /// </summary>
    public static void ClearDynamicBogons() => dynamicBogons = [];

    /// <summary>
    /// 解析 Team Cymru Fullbogons 或標準 CIDR 格式之 Bogon 文字清單。
    /// </summary>
    /// <param name="content">Bogon 文字內容。</param>
    /// <returns>解析出的 <see cref="IPNetwork"/> 清單。</returns>
    public static List<IPNetwork> ParseBogonList(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        List<IPNetwork> results = [];
        using StringReader reader = new(content);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';') || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            int commentIndex = line.IndexOfAny([';', '#', ' ']);
            string candidate = commentIndex > 0 ? line[..commentIndex].Trim() : line;

            if (IPNetwork.TryParse(candidate, out IPNetwork network))
            {
                results.Add(network);
            }
            else if (IPAddress.TryParse(candidate, out IPAddress? singleIp))
            {
                int prefixLength = singleIp.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
                results.Add(new IPNetwork(singleIp, prefixLength));
            }
        }

        return results;
    }

    /// <summary>
    /// 判斷指定 IP 位址是否屬於 Bogon、私有網段、迴路、廣播、多播或特殊保留位址。
    /// </summary>
    /// <param name="address">要評估之 IP 位址。</param>
    /// <returns>若為 Bogon 或保留位址則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsBogonOrReserved(IPAddress? address)
    {
        if (address is null) return true;

        address = IpAddressCanonicalizer.Canonicalize(address);

        // 1. 第一級：靜態硬編碼極速位元檢查 (O(1))
        bool isStaticBogon = address.AddressFamily == AddressFamily.InterNetwork
            ? IsStaticBogonIPv4(address)
            : IsStaticBogonIPv6(address);

        if (isStaticBogon) return true;

        // 2. 第二級：動態 Fullbogons 前綴比對 (隨 IANA/Team Cymru 更新)
        IPNetwork[] dynamicSnapshot = dynamicBogons;
        for (int i = 0; i < dynamicSnapshot.Length; i++)
        {
            if (dynamicSnapshot[i].Contains(address))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判斷指定字串格式之 IP 位址是否屬於 Bogon、私有網段或保留位址。
    /// </summary>
    /// <param name="ipString">IP 字串。</param>
    /// <returns>若為 Bogon、保留位址或無效 IP 則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsBogonOrReserved(string? ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString)) return true;
        if (!IPAddress.TryParse(ipString.Trim(), out IPAddress? address)) return true;
        return IsBogonOrReserved(address);
    }

    private static bool IsStaticBogonIPv4(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        byte b0 = bytes[0];
        byte b1 = bytes[1];

        // 0.0.0.0/8 - Current network (RFC 791 / RFC 1122)
        if (b0 == 0) return true;

        // 10.0.0.0/8 - Private-Use (RFC 1918)
        if (b0 == 10) return true;

        // 100.64.0.0/10 - Shared Address Space / CGNAT (RFC 6598: 100.64.0.0 ~ 100.127.255.255)
        if (b0 == 100 && (b1 >= 64 && b1 <= 127)) return true;

        // 127.0.0.0/8 - Loopback (RFC 1122)
        if (b0 == 127) return true;

        // 169.254.0.0/16 - Link Local (RFC 3927)
        if (b0 == 169 && b1 == 254) return true;

        // 172.16.0.0/12 - Private-Use (RFC 1918: 172.16.0.0 ~ 172.31.255.255)
        if (b0 == 172 && (b1 >= 16 && b1 <= 31)) return true;

        // 192.0.0.0/24 - IETF Protocol Assignments (RFC 6890)
        if (b0 == 192 && b1 == 0 && bytes[2] == 0) return true;

        // 192.0.2.0/24 - Documentation TEST-NET-1 (RFC 5737)
        if (b0 == 192 && b1 == 0 && bytes[2] == 2) return true;

        // 192.168.0.0/16 - Private-Use (RFC 1918)
        if (b0 == 192 && b1 == 168) return true;

        // 198.18.0.0/15 - Benchmarking (RFC 2544: 198.18.0.0 ~ 198.19.255.255)
        if (b0 == 198 && (b1 == 18 || b1 == 19)) return true;

        // 198.51.100.0/24 - Documentation TEST-NET-2 (RFC 5737)
        if (b0 == 198 && b1 == 51 && bytes[2] == 100) return true;

        // 203.0.113.0/24 - Documentation TEST-NET-3 (RFC 5737)
        if (b0 == 203 && b1 == 0 && bytes[2] == 113) return true;

        // 224.0.0.0/4 - Multicast (RFC 5771: 224.0.0.0 ~ 239.255.255.255)
        if (b0 >= 224 && b0 <= 239) return true;

        // 240.0.0.0/4 - Reserved for Future Use (RFC 1112: 240.0.0.0 ~ 255.255.255.254)
        // 255.255.255.255/32 - Limited Broadcast (RFC 919)
        if (b0 >= 240) return true;

        return false;
    }

    private static bool IsStaticBogonIPv6(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal)
            return true;

        byte[] bytes = address.GetAddressBytes();

        // ::/128 - Unspecified
        bool isAllZero = true;
        for (int i = 0; i < 16; i++)
        {
            if (bytes[i] != 0) { isAllZero = false; break; }
        }
        if (isAllZero) return true;

        // ::ffff:0:0/96 - IPv4 mapped
        if (address.IsIPv4MappedToIPv6)
            return IsStaticBogonIPv4(address.MapToIPv4());

        // fc00::/7 - Unique Local Addresses (ULA: fc00:: ~ fdff::)
        if ((bytes[0] & 0xFE) == 0xFC) return true;

        // 2001:db8::/32 - Documentation (RFC 3849)
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8) return true;

        // 2001:2::/48 - Benchmarking (RFC 5180)
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x02) return true;

        // 100::/64 - Discard-Only Address Block (RFC 6666)
        if (bytes[0] == 0x01 && bytes[1] == 0x00) return true;

        return false;
    }
}
