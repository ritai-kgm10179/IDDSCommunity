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
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("198.51.100.0/24", "198.51.101.42"));
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("2001:db8::/32", "2001:db8::42"));
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
}
