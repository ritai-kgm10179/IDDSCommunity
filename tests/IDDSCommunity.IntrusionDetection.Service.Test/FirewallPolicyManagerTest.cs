using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class FirewallPolicyManagerTest
{
    [TestMethod]
    public void RemoteAddressMergeRemainsSingleRuleAndRejectsDuplicates()
    {
        string addresses = "*";
        for (int index = 0; index < 10000; index++)
            addresses = FirewallPolicyManager.MergeRemoteAddresses(addresses, $"10.{index / 65536}.{index / 256 % 256}.{index % 256}");
        string unchanged = FirewallPolicyManager.MergeRemoteAddresses(addresses, "10.0.0.1");
        Assert.AreEqual(addresses, unchanged);
        Assert.AreEqual(10000, addresses.Split(',').Length);
    }
    /// <summary>
    /// Verifies that firewall lookup matches complete IP entries rather than substrings.
    /// </summary>
    [TestMethod]
    public void ContainsAddress_RequiresExactIpEntry()
    {
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("192.0.2.1,198.51.100.2/32", "198.51.100.2"));
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("11.2.3.40", "1.2.3.4"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("*", "1.2.3.4"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("198.51.100.0/24", "198.51.100.42"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("198.51.100.42/255.255.255.255", "198.51.100.42"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("198.51.100.0/255.255.255.0", "198.51.100.42"));
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("198.51.100.0/255.0.255.0", "198.51.100.42"));
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("198.51.100.0/24", "198.51.101.42"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("2001:db8::/32", "2001:db8::42"));
    }

    /// <summary>
    /// 驗證 Windows 防火牆回傳的子網路遮罩格式可正規化供狀態協調使用。
    /// </summary>
    [TestMethod]
    public void NormalizeRemoteAddressEntry_ConvertsSubnetMasksAndHostMasks()
    {
        Assert.AreEqual("198.51.100.42", FirewallPolicyManager.NormalizeRemoteAddressEntry("198.51.100.42/255.255.255.255"));
        Assert.AreEqual("198.51.100.0/24", FirewallPolicyManager.NormalizeRemoteAddressEntry("198.51.100.0/255.255.255.0"));
        Assert.AreEqual("2001:db8::/32", FirewallPolicyManager.NormalizeRemoteAddressEntry("2001:db8::/32"));
        Assert.IsNull(FirewallPolicyManager.NormalizeRemoteAddressEntry("LocalSubnet"));
    }
    /// <summary>
    /// 驗證當同一個 C 段子網超過門檻時，自動聚合為 CIDR 條目。
    /// </summary>
    [TestMethod]
    public void AggregateIpAddresses_AggregatesCSubnetWhenThresholdReached()
    {
        System.Collections.Generic.List<string> ips = ["192.168.1.1", "192.168.1.2", "192.168.1.3", "192.168.1.4", "192.168.1.5", "10.0.0.1"];
        System.Collections.Generic.List<string> aggregated = FirewallPolicyManager.AggregateIpAddresses(ips, subnetThreshold: 5);
        Assert.IsTrue(aggregated.Contains("192.168.1.0/24"));
        Assert.IsTrue(aggregated.Contains("10.0.0.1"));
        Assert.IsFalse(aggregated.Contains("192.168.1.1"));
    }
    /// <summary>
    /// 驗證當 C 段子網中含有 Safe Networks 白名單 IP 時，取消 CIDR 聚合以避免誤殺。
    /// </summary>
    [TestMethod]
    public void AggregateIpAddresses_SkipsAggregationWhenWhitelistCollides()
    {
        System.Collections.Generic.List<string> ips = ["192.168.1.1", "192.168.1.2", "192.168.1.3", "192.168.1.4", "192.168.1.5"];
        System.Collections.Generic.List<string> safeNetworks = ["192.168.1.254"];
        System.Collections.Generic.List<string> aggregated = FirewallPolicyManager.AggregateIpAddresses(ips, safeNetworks: safeNetworks, subnetThreshold: 5);
        Assert.IsFalse(aggregated.Contains("192.168.1.0/24"));
        Assert.IsTrue(aggregated.Contains("192.168.1.1"));
        Assert.IsTrue(aggregated.Contains("192.168.1.5"));
    }

    /// <summary>
    /// 驗證傳入放行規則規格模型之屬性驗證與相等性比較。
    /// </summary>
    [TestMethod]
    public void FirewallInboundRuleDefinition_PropertiesAndEquality()
    {
        var rule1 = new FirewallInboundRuleDefinition("SelfServicePortal", "Portal Rule", 8444, "TCP");
        var rule2 = new FirewallInboundRuleDefinition("selfserviceportal", "Different Display Name", 8444, "tcp");
        var rule3 = new FirewallInboundRuleDefinition("ManagementApi", "API Rule", 8443, "TCP");

        Assert.AreEqual("SelfServicePortal", rule1.FeatureKey);
        Assert.AreEqual(8444, rule1.Port);
        Assert.AreEqual("TCP", rule1.Protocol);
        Assert.AreEqual(rule1, rule2);
        Assert.AreNotEqual(rule1, rule3);
        Assert.AreEqual(rule1.GetHashCode(), rule2.GetHashCode());

        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => new FirewallInboundRuleDefinition("Test", "Test", 0));
        Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(() => new FirewallInboundRuleDefinition("Test", "Test", 65536));
    }

    /// <summary>
    /// 驗證傳入放行規則命名之標準化格式。
    /// </summary>
    [TestMethod]
    public void GetInboundAllowRuleName_GeneratesStandardizedName()
    {
        string name = FirewallPolicyManager.GetInboundAllowRuleName("SelfServicePortal", "tcp", 8444);
        Assert.AreEqual("IDDSCommunity_Allow_SelfServicePortal_TCP_8444", name);

        string hubName = FirewallPolicyManager.GetInboundAllowRuleName("ThreatHub", "TCP", 8443);
        Assert.AreEqual("IDDSCommunity_Allow_ThreatHub_TCP_8443", hubName);
    }
}
