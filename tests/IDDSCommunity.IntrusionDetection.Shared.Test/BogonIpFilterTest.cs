using System.Collections.Generic;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class BogonIpFilterTest
{
    [TestCleanup]
    public void Cleanup()
    {
        BogonIpFilter.ClearDynamicBogons();
    }

    [TestMethod]
    public void BogonIpFilter_IdentifiesPrivateAndReservedIPv4()
    {
        // RFC 1918 Private
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("10.0.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("10.255.255.254"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("172.16.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("172.31.255.254"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("192.168.1.1"));

        // Loopback
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("127.0.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("127.255.255.255"));

        // Link Local (APIPA)
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("169.254.1.1"));

        // CGNAT (RFC 6598)
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("100.64.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("100.127.255.254"));

        // Multicast & Broadcast
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("224.0.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("239.255.255.250"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("240.0.0.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("255.255.255.255"));

        // Documentation / TEST-NET
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("192.0.2.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("198.51.100.1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("203.0.113.1"));

        // Valid Public IPv4
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("8.8.8.8"));
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("1.1.1.1"));
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("140.112.1.1"));
    }

    [TestMethod]
    public void BogonIpFilter_IdentifiesPrivateAndReservedIPv6()
    {
        // Loopback & Unspecified
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("::1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("::"));

        // Link Local & ULA
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("fe80::1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("fc00::1"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("fd12:3456:789a::1"));

        // Documentation
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("2001:db8::1"));

        // Valid Public IPv6 (Global Unicast)
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("2606:4700:4700::1111"));
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("2001:4860:4860::8888"));
    }

    [TestMethod]
    public void BogonIpFilter_DynamicBogons_ParsesAndMatchesDynamicCidr()
    {
        string mockCymruBogonList = @"
# Team Cymru Fullbogons IPv4
# Updated 2026-08-31
198.19.0.0/16
140.112.200.0/24 # dynamically unallocated test subnet
";

        List<IPNetwork> parsed = BogonIpFilter.ParseBogonList(mockCymruBogonList);
        Assert.AreEqual(2, parsed.Count);

        BogonIpFilter.UpdateDynamicBogons(parsed);
        Assert.AreEqual(2, BogonIpFilter.DynamicBogonCount);

        // 測試動態網段命中
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("140.112.200.50"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("140.112.200.254"));

        // 測試未在動態網段中的其他 IP
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("140.112.1.1"));

        // 清除動態網段後應還原
        BogonIpFilter.ClearDynamicBogons();
        Assert.AreEqual(0, BogonIpFilter.DynamicBogonCount);
        Assert.IsFalse(BogonIpFilter.IsBogonOrReserved("140.112.200.50"));
    }

    [TestMethod]
    public void BogonIpFilter_RejectsNullOrInvalidStrings()
    {
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved(string.Empty));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("   "));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved("not-an-ip"));
        Assert.IsTrue(BogonIpFilter.IsBogonOrReserved((IPAddress?)null));
    }
}
