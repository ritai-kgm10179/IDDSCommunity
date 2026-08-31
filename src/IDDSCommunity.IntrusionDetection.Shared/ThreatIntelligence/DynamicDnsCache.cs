using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供安全網路動態 DNS（DDNS FQDN）解析結果之全域快取與比對服務。
/// </summary>
public static class DynamicDnsCache
{
    private static readonly ConcurrentDictionary<string, HashSet<IPAddress>> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 更新指定 FQDN 主機名稱所解析出之 IP 位址集合。
    /// </summary>
    /// <param name="fqdn">動態主機名稱（例如 office.ddns.net）。</param>
    /// <param name="addresses">解析出之 IP 位址清單。</param>
    public static void Update(string fqdn, IEnumerable<IPAddress> addresses)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fqdn);
        ArgumentNullException.ThrowIfNull(addresses);

        HashSet<IPAddress> set = [];
        foreach (IPAddress ip in addresses)
        {
            set.Add(IpAddressCanonicalizer.Canonicalize(ip));
        }

        Cache[fqdn.Trim()] = set;
    }

    /// <summary>
    /// 取得指定 FQDN 主機名稱目前快取之 IP 位址集合。
    /// </summary>
    /// <param name="fqdn">動態主機名稱。</param>
    /// <param name="addresses">傳回之 IP 位址集合。</param>
    /// <returns>若快取存在則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryGetResolvedIps(string fqdn, out HashSet<IPAddress> addresses)
    {
        if (string.IsNullOrWhiteSpace(fqdn))
        {
            addresses = [];
            return false;
        }

        return Cache.TryGetValue(fqdn.Trim(), out addresses!);
    }

    /// <summary>
    /// 檢查指定的候選 IP 位址是否符合指定 FQDN 所解析出的任意 IP 位址。
    /// </summary>
    /// <param name="candidateAddress">待檢查之標準化 IP 位址。</param>
    /// <param name="fqdn">動態主機名稱。</param>
    /// <returns>若符合則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsIpInDdns(IPAddress candidateAddress, string fqdn)
    {
        ArgumentNullException.ThrowIfNull(candidateAddress);
        if (string.IsNullOrWhiteSpace(fqdn)) return false;

        if (Cache.TryGetValue(fqdn.Trim(), out HashSet<IPAddress>? set) && set != null)
        {
            IPAddress normalized = IpAddressCanonicalizer.Canonicalize(candidateAddress);
            return set.Contains(normalized);
        }

        return false;
    }

    /// <summary>
    /// 清除所有動態 DNS 解析快取。
    /// </summary>
    public static void Clear()
    {
        Cache.Clear();
    }
}
