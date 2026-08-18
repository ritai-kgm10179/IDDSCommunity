namespace IDDSCommunity.IntrusionDetection.Shared.Network;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

/// <summary>
/// 提供符合 RFC 7239 與標準 X-Forwarded-For 規格之受信任反向代理（Trusted Proxy）鏈結解析器，
/// 實作由右至左（Right-to-Left）逐層剝除受信任代理之安全演算法。
/// </summary>
public static partial class TrustedProxyParser
{
    private const int DefaultMaxHops = 10;

    /// <summary>
    /// 依據直接連線端點（Direct Peer）與 HTTP 標頭（Forwarded / X-Forwarded-For），
    /// 配合受信任代理 CIDR 清單，自右向左安全解析出真實客戶端來源 IP 位址。
    /// </summary>
    /// <param name="directPeer">直接建立連線之來源端點 IP 位址。</param>
    /// <param name="forwardedHeader">RFC 7239 Forwarded 標頭字串（若存在）。</param>
    /// <param name="xForwardedForHeader">標準 X-Forwarded-For 標頭字串（若存在）。</param>
    /// <param name="trustedProxyCidrs">受信任反向代理之 IP 或 CIDR 清單。</param>
    /// <param name="maxHops">允許解析之最大躍點數（預設為 10）。</param>
    /// <returns>經安全驗證後之客戶端來源 IP 位址；若直接端點不受信任或標頭無效則傳回直接端點。</returns>
    public static IPAddress ResolveClientIp(
        IPAddress directPeer,
        string? forwardedHeader,
        string? xForwardedForHeader,
        IEnumerable<string>? trustedProxyCidrs,
        int maxHops = DefaultMaxHops)
    {
        ArgumentNullException.ThrowIfNull(directPeer);

        // 若無設定任何受信任代理，或直接連線之端點不在受信任清單中，嚴禁採用任何轉發標頭
        if (trustedProxyCidrs is null || !IsTrustedProxy(directPeer, trustedProxyCidrs))
        {
            return directPeer;
        }

        List<IPAddress> hopList = [];

        // 優先解析 RFC 7239 Forwarded 標頭；若無或無有效 IP 則解析 X-Forwarded-For
        if (!string.IsNullOrWhiteSpace(forwardedHeader))
        {
            hopList = ParseForwardedHeader(forwardedHeader, maxHops);
        }

        if (hopList.Count == 0 && !string.IsNullOrWhiteSpace(xForwardedForHeader))
        {
            hopList = ParseXForwardedForHeader(xForwardedForHeader, maxHops);
        }

        if (hopList.Count == 0)
        {
            return directPeer;
        }

        // 將直接端點加入躍點鏈最右側
        hopList.Add(directPeer);

        // 由右至左檢查躍點：若當前躍點為受信任代理，則繼續往左尋找；一旦遇到非受信任 IP 即判定為真實客戶端來源
        for (int i = hopList.Count - 1; i >= 0; i--)
        {
            IPAddress currentHop = hopList[i];
            if (!IsTrustedProxy(currentHop, trustedProxyCidrs))
            {
                return currentHop;
            }
        }

        // 若鏈結上所有躍點均落在受信任範圍內，傳回最左側端點
        return hopList[0];
    }

    /// <summary>
    /// 檢查指定 IP 位址是否符合受信任代理 CIDR 清單中之任一規則。
    /// </summary>
    /// <param name="address">待檢查之 IP 位址。</param>
    /// <param name="trustedProxyCidrs">受信任 CIDR 或單一 IP 清單。</param>
    /// <returns>若符合受信任規則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsTrustedProxy(IPAddress address, IEnumerable<string> trustedProxyCidrs)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (trustedProxyCidrs is null) return false;

        foreach (string entry in trustedProxyCidrs)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;
            string trimmed = entry.Trim();

            // 單一 IP 比對
            if (IPAddress.TryParse(trimmed, out IPAddress? singleIp))
            {
                if (singleIp.Equals(address)) return true;
                continue;
            }

            // CIDR 區段比對 (e.g. 10.0.0.0/8 or 2001:db8::/32)
            string[] parts = trimmed.Split('/');
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? networkIp) && int.TryParse(parts[1], out int prefixLength))
            {
                if (networkIp.AddressFamily == address.AddressFamily && IsIpInCidr(address, networkIp, prefixLength))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 解析 RFC 7239 Forwarded 標頭（例如: for=192.0.2.60;proto=http;by=203.0.113.43, for="[2001:db8:cafe::17]:4711"）。
    /// </summary>
    /// <param name="headerValue">Forwarded 標頭值。</param>
    /// <param name="maxHops">最大躍點數。</param>
    /// <returns>解析出的 IP 躍點清單（由左至右順序）。</returns>
    public static List<IPAddress> ParseForwardedHeader(string headerValue, int maxHops = DefaultMaxHops)
    {
        List<IPAddress> hops = [];
        if (string.IsNullOrWhiteSpace(headerValue)) return hops;

        // 依逗號分隔多個代理轉發元素
        string[] elements = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int hopCount = 0;

        foreach (string elem in elements)
        {
            if (hopCount >= maxHops) break;

            // 依分號拆分各參數 (for, proto, by, host)
            string[] directives = elem.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string directive in directives)
            {
                int eqIdx = directive.IndexOf('=');
                if (eqIdx <= 0) continue;

                string key = directive[..eqIdx].Trim();
                string val = directive[(eqIdx + 1)..].Trim().Trim('"');

                if (string.Equals(key, "for", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryParseCleanIp(val, out IPAddress? parsed) && parsed is not null)
                    {
                        hops.Add(parsed);
                        hopCount++;
                    }
                    break;
                }
            }
        }

        return hops;
    }

    /// <summary>
    /// 解析標準 X-Forwarded-For 標頭（例如: 198.51.100.1, 203.0.113.195, 10.0.0.1）。
    /// </summary>
    /// <param name="headerValue">X-Forwarded-For 標頭值。</param>
    /// <param name="maxHops">最大躍點數。</param>
    /// <returns>解析出的 IP 躍點清單（由左至右順序）。</returns>
    public static List<IPAddress> ParseXForwardedForHeader(string headerValue, int maxHops = DefaultMaxHops)
    {
        List<IPAddress> hops = [];
        if (string.IsNullOrWhiteSpace(headerValue)) return hops;

        string[] elements = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int hopCount = 0;

        foreach (string elem in elements)
        {
            if (hopCount >= maxHops) break;

            if (TryParseCleanIp(elem, out IPAddress? parsed) && parsed is not null)
            {
                hops.Add(parsed);
                hopCount++;
            }
        }

        return hops;
    }

    /// <summary>
    /// 清理並解析可能附帶連接埠或中括號的 IPv4 / IPv6 字串。
    /// </summary>
    /// <param name="raw">原始 IP 或主機連接埠字串。</param>
    /// <param name="address">解析成功之 IPAddress 物件。</param>
    /// <returns>若解析成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryParseCleanIp(string? raw, out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string trimmed = raw.Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed) || 
            trimmed.StartsWith('_') || 
            string.Equals(trimmed, "unknown", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "-", StringComparison.Ordinal))
        {
            return false;
        }

        // 移除方括號與可能包含的埠號 (e.g. "[2001:db8::1]:8080" -> "2001:db8::1")
        if (trimmed.StartsWith('['))
        {
            int closeIdx = trimmed.IndexOf(']');
            if (closeIdx > 1)
            {
                string inside = trimmed.Substring(1, closeIdx - 1);
                return IPAddress.TryParse(inside, out address);
            }
        }

        // IPv4 附帶埠號 (e.g. "192.0.2.1:5000")
        int colonCount = 0;
        foreach (char c in trimmed) if (c == ':') colonCount++;

        if (colonCount == 1)
        {
            int colonIdx = trimmed.IndexOf(':');
            string ipPart = trimmed[..colonIdx];
            if (IPAddress.TryParse(ipPart, out address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return true;
            }
        }

        return IPAddress.TryParse(trimmed, out address);
    }

    private static bool IsIpInCidr(IPAddress address, IPAddress network, int prefixLength)
    {
        byte[] addressBytes = address.GetAddressBytes();
        byte[] networkBytes = network.GetAddressBytes();

        if (addressBytes.Length != networkBytes.Length) return false;
        int bitsToCheck = prefixLength;
        int byteIndex = 0;

        while (bitsToCheck >= 8)
        {
            if (addressBytes[byteIndex] != networkBytes[byteIndex]) return false;
            bitsToCheck -= 8;
            byteIndex++;
        }

        if (bitsToCheck > 0)
        {
            int mask = (0xFF << (8 - bitsToCheck)) & 0xFF;
            if ((addressBytes[byteIndex] & mask) != (networkBytes[byteIndex] & mask)) return false;
        }

        return true;
    }
}
