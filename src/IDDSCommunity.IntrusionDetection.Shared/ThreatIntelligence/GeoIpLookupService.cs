using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供 IP 位址之國家地理位置查詢 (GeoIP Lookup) 與基於 ISO 3166-1 國家代碼之區域封鎖 (Geo-fencing) 引擎。
/// </summary>
public static class GeoIpLookupService
{
    private static volatile List<GeoIpEntry> ipv4Entries = [];
    private static volatile List<GeoIpEntry> ipv6Entries = [];

    private sealed record GeoIpEntry(IPNetwork Network, string CountryCode, string CountryName);

    /// <summary>
    /// 取得目前已載入之 GeoIP 網段記錄數量。
    /// </summary>
    public static int TotalLoadedRecords => ipv4Entries.Count + ipv6Entries.Count;

    /// <summary>
    /// 清除所有已載入之 GeoIP 網段記錄。
    /// </summary>
    public static void Clear()
    {
        ipv4Entries = [];
        ipv6Entries = [];
    }

    /// <summary>
    /// 自 CSV 格式字串（格式：CIDR,CountryCode,CountryName）載入 GeoIP 網段對照表。
    /// </summary>
    /// <param name="csvContent">CSV 文字內容。</param>
    /// <returns>成功載入之記錄總數。</returns>
    public static int LoadFromCsv(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent)) return 0;

        List<GeoIpEntry> v4 = [];
        List<GeoIpEntry> v6 = [];

        using StringReader reader = new(csvContent);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            string[] parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;

            if (IPNetwork.TryParse(parts[0], out IPNetwork network))
            {
                string code = parts[1].ToUpperInvariant();
                string name = parts.Length >= 3 ? parts[2] : code;
                var entry = new GeoIpEntry(network, code, name);

                if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                    v4.Add(entry);
                else
                    v6.Add(entry);
            }
        }

        ipv4Entries = v4;
        ipv6Entries = v6;
        return v4.Count + v6.Count;
    }

    /// <summary>
    /// 查詢指定 IP 位址之國家代碼與名稱。
    /// </summary>
    /// <param name="address">要查詢之 IP 位址。</param>
    /// <param name="countryCode">查詢成功時輸出的 2 位字母 ISO 國家代碼；未找到時為 "ZZ"。</param>
    /// <param name="countryName">查詢成功時輸出的國家名稱；未找到時為 "Unknown"。</param>
    /// <returns>若成功找到所屬國家則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryLookup(IPAddress? address, out string countryCode, out string countryName)
    {
        if (address is null)
        {
            countryCode = "ZZ";
            countryName = "Unknown";
            return false;
        }

        address = IpAddressCanonicalizer.Canonicalize(address);

        var list = address.AddressFamily == AddressFamily.InterNetwork ? ipv4Entries : ipv6Entries;
        foreach (var entry in list)
        {
            if (entry.Network.Contains(address))
            {
                countryCode = entry.CountryCode;
                countryName = entry.CountryName;
                return true;
            }
        }

        countryCode = "ZZ";
        countryName = "Unknown";
        return false;
    }

    /// <summary>
    /// 判斷指定 IP 位址是否屬於指定之國家封鎖清單。
    /// </summary>
    /// <param name="address">要檢查的 IP 位址。</param>
    /// <param name="blockedCountries">封鎖的 ISO 國家代碼集合（例如 ["CN", "RU", "KP"]）。</param>
    /// <returns>若 IP 屬於被封鎖之國家則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsCountryBlocked(IPAddress? address, IEnumerable<string>? blockedCountries)
    {
        if (address is null || blockedCountries is null)
            return false;

        var set = new HashSet<string>(blockedCountries.Select(c => c.Trim().ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
        if (set.Count == 0)
            return false;

        if (TryLookup(address, out string countryCode, out _))
        {
            return set.Contains(countryCode);
        }

        return false;
    }
}
