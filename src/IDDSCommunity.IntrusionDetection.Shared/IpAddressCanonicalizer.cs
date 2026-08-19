using System;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供來源 IP 位址的單一正規化規則，避免等價位址以不同文字格式分散統計、允許清單與封鎖狀態。
/// </summary>
public static class IpAddressCanonicalizer
{
    /// <summary>
    /// 嘗試解析並正規化 IP 位址；IPv4-mapped IPv6 會轉換為 IPv4，IPv6 則使用標準壓縮格式且移除介面範圍識別碼。
    /// </summary>
    /// <param name="value">欲解析的 IP 位址文字。</param>
    /// <param name="canonicalAddress">正規化後的 IP 位址文字。</param>
    /// <returns>解析成功時傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryCanonicalize(string? value, out string canonicalAddress)
    {
        canonicalAddress = string.Empty;
        if (!IPAddress.TryParse(value?.Trim(), out IPAddress? address))
            return false;

        canonicalAddress = Canonicalize(address).ToString();
        return true;
    }

    /// <summary>
    /// 正規化指定 IP 位址物件。
    /// </summary>
    /// <param name="address">欲正規化的 IP 位址。</param>
    /// <returns>正規化後的 IP 位址。</returns>
    public static IPAddress Canonicalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.IsIPv4MappedToIPv6)
            return address.MapToIPv4();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && address.ScopeId != 0
            ? new IPAddress(address.GetAddressBytes())
            : address;
    }

    /// <summary>
    /// 解析並正規化 IP 位址，無效格式會擲回例外狀況。
    /// </summary>
    /// <param name="value">欲解析的 IP 位址文字。</param>
    /// <returns>正規化後的 IP 位址文字。</returns>
    /// <exception cref="FormatException"><paramref name="value"/> 不是有效 IP 位址。</exception>
    public static string Canonicalize(string value) => TryCanonicalize(value, out string canonicalAddress)
        ? canonicalAddress
        : throw new FormatException(Localization.Strings.Get("Invalid IP address."));
}
