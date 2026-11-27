using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供 IPv4 與 IPv6 CIDR 前綴網路之高效能記憶體索引結構。
/// </summary>
public sealed class IpPrefixIndex
{
    private readonly Dictionary<int, HashSet<UInt128>> v4 = [];
    private readonly Dictionary<int, HashSet<UInt128>> v6 = [];

    /// <summary>
    /// 取得已索引之唯一 CIDR 前綴網路總數。
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// 初始化 <see cref="IpPrefixIndex"/> 類別的新執行個體。
    /// </summary>
    /// <param name="networks">欲加入索引之 CIDR 網路集合。</param>
    public IpPrefixIndex(IEnumerable<IPNetwork> networks)
    {
        foreach (IPNetwork network in networks)
        {
            Dictionary<int, HashSet<UInt128>> map = network.BaseAddress.AddressFamily == AddressFamily.InterNetwork ? v4 : v6;
            if (!map.TryGetValue(network.PrefixLength, out HashSet<UInt128>? set)) map[network.PrefixLength] = set = [];
            if (set.Add(Number(network.BaseAddress))) Count++;
        }
    }

    /// <summary>
    /// 判斷指定的 IP 位址是否落於任何已索引的前綴網路內。
    /// </summary>
    /// <param name="address">欲檢查的 IP 位址。</param>
    /// <returns>若符合任一前綴網路則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool Contains(IPAddress address)
    {
        bool ipv4 = address.AddressFamily == AddressFamily.InterNetwork;
        UInt128 value = Number(address);
        int bits = ipv4 ? 32 : 128;
        foreach (KeyValuePair<int, HashSet<UInt128>> entry in ipv4 ? v4 : v6)
        {
            UInt128 masked = entry.Key == 0 ? 0 : (value >> (bits - entry.Key)) << (bits - entry.Key);
            if (entry.Value.Contains(masked)) return true;
        }
        return false;
    }

    /// <summary>
    /// 將 IPv4 或 IPv6 位址轉換為無配置之 128 位元整數表示法。
    /// </summary>
    /// <param name="address">欲轉換的 IP 位址。</param>
    /// <returns>傳回高位元組對齊之無符號 128 位元整數。</returns>
    public static UInt128 Number(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[16];
        address.TryWriteBytes(bytes, out int length);
        return length == 4 ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt128BigEndian(bytes);
    }
}