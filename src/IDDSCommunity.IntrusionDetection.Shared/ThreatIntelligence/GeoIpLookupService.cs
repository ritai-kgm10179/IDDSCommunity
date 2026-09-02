using System;
using System.Buffers.Binary;
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

    private sealed class GeoIpEntry
    {
        public IPNetwork? Network { get; }
        public uint StartV4 { get; }
        public uint EndV4 { get; }
        public byte[]? StartV6 { get; }
        public byte[]? EndV6 { get; }
        public string CountryCode { get; }
        public string CountryName { get; }

        public GeoIpEntry(IPNetwork network, string countryCode, string countryName)
        {
            Network = network;
            CountryCode = countryCode;
            CountryName = countryName;
        }

        public GeoIpEntry(uint startV4, uint endV4, string countryCode, string countryName)
        {
            StartV4 = startV4;
            EndV4 = endV4;
            CountryCode = countryCode;
            CountryName = countryName;
        }

        public GeoIpEntry(byte[] startV6, byte[] endV6, string countryCode, string countryName)
        {
            StartV6 = startV6;
            EndV6 = endV6;
            CountryCode = countryCode;
            CountryName = countryName;
        }

        public bool Contains(IPAddress address, uint? addressV4Int, byte[]? addressV6Bytes)
        {
            if (Network.HasValue)
            {
                return Network.Value.Contains(address);
            }

            if (addressV4Int.HasValue && EndV4 > 0)
            {
                return addressV4Int.Value >= StartV4 && addressV4Int.Value <= EndV4;
            }

            if (addressV6Bytes != null && StartV6 != null && EndV6 != null)
            {
                return CompareBytes(addressV6Bytes, StartV6) >= 0 && CompareBytes(addressV6Bytes, EndV6) <= 0;
            }

            return false;
        }

        private static int CompareBytes(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                if (a[i] < b[i]) return -1;
                if (a[i] > b[i]) return 1;
            }
            return 0;
        }
    }

    /// <summary>
    /// 取得目前已載入之 GeoIP 網段記錄數量。
    /// </summary>
    public static int TotalLoadedRecords => ipv4Entries.Count + ipv6Entries.Count;

    /// <summary>
    /// 取得目前已載入之相異國家總數。
    /// </summary>
    public static int TotalLoadedCountries =>
        ipv4Entries.Concat(ipv6Entries)
            .Select(e => e.CountryCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    /// <summary>
    /// 清除所有已載入之 GeoIP 網段記錄。
    /// </summary>
    public static void Clear()
    {
        ipv4Entries = [];
        ipv6Entries = [];
    }

    /// <summary>
    /// 自單一 CSV 格式字串（格式：CIDR,CountryCode,CountryName 或 StartIP,EndIP,CountryCode,CountryName）載入 GeoIP 網段對照表。
    /// </summary>
    /// <param name="csvContent">CSV 文字內容。</param>
    /// <returns>成功載入之記錄總數。</returns>
    public static int LoadFromCsv(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            Clear();
            return 0;
        }

        List<GeoIpEntry> v4 = [];
        List<GeoIpEntry> v6 = [];
        ParseCsvContent(csvContent, v4, v6);

        ipv4Entries = v4;
        ipv6Entries = v6;
        return v4.Count + v6.Count;
    }

    /// <summary>
    /// 分別自 IPv4 與 IPv6 之 CSV 格式字串載入 GeoIP 網段對照表（原子熱替換）。
    /// </summary>
    /// <param name="ipv4CsvContent">IPv4 CSV 文字內容。</param>
    /// <param name="ipv6CsvContent">IPv6 CSV 文字內容。</param>
    /// <returns>成功載入之記錄總數。</returns>
    public static int LoadFromCsv(string? ipv4CsvContent, string? ipv6CsvContent)
    {
        List<GeoIpEntry> v4 = [];
        List<GeoIpEntry> v6 = [];

        if (!string.IsNullOrWhiteSpace(ipv4CsvContent))
        {
            ParseCsvContent(ipv4CsvContent, v4, v6);
        }

        if (!string.IsNullOrWhiteSpace(ipv6CsvContent))
        {
            ParseCsvContent(ipv6CsvContent, v4, v6);
        }

        ipv4Entries = v4;
        ipv6Entries = v6;
        return v4.Count + v6.Count;
    }

    private static void ParseCsvContent(string csvContent, List<GeoIpEntry> v4List, List<GeoIpEntry> v6List)
    {
        using StringReader reader = new(csvContent);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';') || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            string[] parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;

            // 清理引號
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim('"', '\'');
            }

            // 忽略常見 CSV 標頭行 (如 network,country_iso_code 或 start_ip,end_ip)
            if (string.Equals(parts[0], "network", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parts[0], "start_ip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parts[0], "cidr", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 格式 1: CIDR (例如 1.1.1.0/24, AU, Australia)
            if (IPNetwork.TryParse(parts[0], out IPNetwork network))
            {
                string code = parts[1].ToUpperInvariant();
                string name = parts.Length >= 3 ? parts[2] : code;
                var entry = new GeoIpEntry(network, code, name);

                if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                    v4List.Add(entry);
                else
                    v6List.Add(entry);
                continue;
            }

            // 格式 2: IP 範圍 (例如 1.0.0.0, 1.0.0.255, AU, Australia)
            if (parts.Length >= 3 &&
                IPAddress.TryParse(parts[0], out IPAddress? startIp) &&
                IPAddress.TryParse(parts[1], out IPAddress? endIp))
            {
                string code = parts[2].ToUpperInvariant();
                string name = parts.Length >= 4 ? parts[3] : code;

                if (startIp.AddressFamily == AddressFamily.InterNetwork && endIp.AddressFamily == AddressFamily.InterNetwork)
                {
                    uint startInt = ToUInt32(startIp);
                    uint endInt = ToUInt32(endIp);
                    if (startInt <= endInt)
                    {
                        v4List.Add(new GeoIpEntry(startInt, endInt, code, name));
                    }
                }
                else if (startIp.AddressFamily == AddressFamily.InterNetworkV6 && endIp.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    byte[] startBytes = startIp.GetAddressBytes();
                    byte[] endBytes = endIp.GetAddressBytes();
                    v6List.Add(new GeoIpEntry(startBytes, endBytes, code, name));
                }
            }
        }
    }

    private static uint ToUInt32(IPAddress ip)
    {
        Span<byte> bytes = stackalloc byte[4];
        ip.TryWriteBytes(bytes, out _);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
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

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            uint addressInt = ToUInt32(address);
            var list = ipv4Entries;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry.Contains(address, addressInt, null))
                {
                    countryCode = entry.CountryCode;
                    countryName = entry.CountryName;
                    return true;
                }
            }
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] addressBytes = address.GetAddressBytes();
            var list = ipv6Entries;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry.Contains(address, null, addressBytes))
                {
                    countryCode = entry.CountryCode;
                    countryName = entry.CountryName;
                    return true;
                }
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
