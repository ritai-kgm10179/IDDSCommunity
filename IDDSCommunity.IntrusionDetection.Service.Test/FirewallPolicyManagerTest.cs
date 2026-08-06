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
}
