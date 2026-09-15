using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class FirewallPolicyManagerTest
{
    [TestMethod]
    public void BuildBlockReconciliationPlan_OneHundredThousandUnchanged_HasNoOperations()
    {
        string[] desired = System.Linq.Enumerable.Range(0, 100_000)
            .Select(index => $"10.{index / 65536}.{index / 256 % 256}.{index % 256}")
            .ToArray();
        FirewallBlockReconciliationPlan initial = FirewallPolicyManager.BuildBlockReconciliationPlan(
            desired, [], FirewallBlockMode.Inbound);
        FirewallManagedRuleSnapshot[] snapshots = initial.RulesToCreate
            .Select(rule => new FirewallManagedRuleSnapshot(
                rule.Name, rule.Direction, rule.Action, rule.Protocol, rule.Enabled, rule.RemoteAddresses))
            .ToArray();

        FirewallBlockReconciliationPlan unchanged = FirewallPolicyManager.BuildBlockReconciliationPlan(
            desired, snapshots, FirewallBlockMode.Inbound);

        Assert.IsEmpty(unchanged.RulesToCreate);
        Assert.IsEmpty(unchanged.RulesToPatch);
        Assert.IsEmpty(unchanged.RulesToDelete);
    }

    [TestMethod]
    public void BuildBlockReconciliationPlan_SingleAddressDelta_ChangesOneStableShard()
    {
        string[] desired = System.Linq.Enumerable.Range(0, 100_000)
            .Select(index => $"10.{index / 65536}.{index / 256 % 256}.{index % 256}")
            .ToArray();
        FirewallBlockReconciliationPlan initial = FirewallPolicyManager.BuildBlockReconciliationPlan(
            desired, [], FirewallBlockMode.Inbound);
        FirewallManagedRuleSnapshot[] snapshots = initial.RulesToCreate
            .Select(rule => new FirewallManagedRuleSnapshot(
                rule.Name, rule.Direction, rule.Action, rule.Protocol, rule.Enabled, rule.RemoteAddresses))
            .ToArray();

        FirewallBlockReconciliationPlan changed = FirewallPolicyManager.BuildBlockReconciliationPlan(
            [.. desired, "203.0.113.250"], snapshots, FirewallBlockMode.Inbound);

        Assert.AreEqual(1, changed.RulesToCreate.Count + changed.RulesToPatch.Count);
        Assert.IsEmpty(changed.RulesToDelete);
    }

    [TestMethod]
    public void BuildBlockReconciliationPlan_OneMillionRemove_PreservesFollowingShardMembership()
    {
        string[] desired = System.Linq.Enumerable.Range(0, 1_000_000)
            .Select(index => $"10.{index / 65536}.{index / 256 % 256}.{index % 256}")
            .ToArray();
        FirewallBlockReconciliationPlan initial = FirewallPolicyManager.BuildBlockReconciliationPlan(
            desired, [], FirewallBlockMode.Inbound);
        FirewallManagedRuleSnapshot[] snapshots = initial.RulesToCreate
            .Select(rule => new FirewallManagedRuleSnapshot(
                rule.Name, rule.Direction, rule.Action, rule.Protocol, rule.Enabled, rule.RemoteAddresses))
            .ToArray();
        System.Collections.Generic.Dictionary<string, string> assignments = initial.Assignments
            .ToDictionary(item => item.Address, item => item.ShardId, System.StringComparer.OrdinalIgnoreCase);

        FirewallBlockReconciliationPlan changed = FirewallPolicyManager.BuildBlockReconciliationPlan(
            desired.Skip(1).ToArray(), snapshots, FirewallBlockMode.Inbound, assignments);

        Assert.IsEmpty(changed.RulesToCreate);
        Assert.HasCount(1, changed.RulesToPatch);
        Assert.IsEmpty(changed.RulesToDelete);
    }

    [TestMethod]
    public void BuildBlockReconciliationPlan_ExplicitCidr_RemainsExactPrefix()
    {
        FirewallBlockReconciliationPlan plan = FirewallPolicyManager.BuildBlockReconciliationPlan(
            ["198.51.100.0/24", "2001:db8::/32"], [], FirewallBlockMode.Inbound);

        string[] entries = plan.RulesToCreate
            .SelectMany(rule => rule.RemoteAddresses.Split(','))
            .ToArray();
        CollectionAssert.Contains(entries, "198.51.100.0/24");
        CollectionAssert.Contains(entries, "2001:db8::/32");
        Assert.HasCount(2, entries);
    }

    [TestMethod]
    public void StableShardParserAndComparer_UseNumericSequence()
    {
        const string baseName = "IDDSCommunity_BlockAttacker_AllPorts";
        string shard = FirewallPolicyManager.GetStableShardName(baseName, 7, 10);

        Assert.IsTrue(FirewallPolicyManager.TryParseStableShardName(baseName, shard, out int bucket, out int sequence));
        Assert.AreEqual(7, bucket);
        Assert.AreEqual(10, sequence);
        Assert.IsLessThan(0, FirewallPolicyManager.CompareShardNames(baseName + "_2", baseName + "_10"));
    }

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
    /// 驗證少量主機位址不會被擴大成整個子網路。
    /// </summary>
    [TestMethod]
    public void AggregateIpAddresses_DoesNotExpandHostsIntoSubnet()
    {
        System.Collections.Generic.List<string> ips = ["192.168.1.1", "192.168.1.2", "192.168.1.3", "192.168.1.4", "192.168.1.5", "10.0.0.1"];
        System.Collections.Generic.List<string> aggregated = FirewallPolicyManager.AggregateIpAddresses(ips, subnetThreshold: 5);
        Assert.IsFalse(aggregated.Contains("192.168.1.0/24"));
        Assert.IsTrue(aggregated.Contains("10.0.0.1"));
        Assert.IsTrue(aggregated.Contains("192.168.1.1"));
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
    /// 驗證安全網路 CIDR 與封鎖 CIDR 重疊時，不建立可能封鎖白名單的規則。
    /// </summary>
    [TestMethod]
    public void AggregateIpAddresses_RejectsAnySafeNetworkOverlap()
    {
        System.Collections.Generic.List<string> aggregated = FirewallPolicyManager.AggregateIpAddresses(
            ["198.51.100.0/24", "2001:db8::/32", "203.0.113.9"],
            ["198.51.100.128/25", "2001:db8:1::/48"]);

        Assert.HasCount(18, aggregated);
        Assert.IsTrue(aggregated.Contains("198.51.100.0/25"));
        Assert.IsTrue(aggregated.Contains("203.0.113.9"));
        Assert.IsFalse(System.Linq.Enumerable.Any(aggregated, entry => FirewallPolicyManager.ContainsAddress(entry, "198.51.100.200")));
        Assert.IsFalse(System.Linq.Enumerable.Any(aggregated, entry => FirewallPolicyManager.ContainsAddress(entry, "2001:db8:1::1")));
        Assert.IsTrue(System.Linq.Enumerable.Any(aggregated, entry => FirewallPolicyManager.ContainsAddress(entry, "2001:db8:2::1")));
    }

    /// <summary>
    /// 驗證從 CIDR 移除單一主機時會保留完整差集，不會刪除整個網段。
    /// </summary>
    [TestMethod]
    public void ExceptAddress_RemovesSingleHostFromCidrWithoutWideningOrDroppingRemainder()
    {
        System.Collections.Generic.IReadOnlyList<string> remaining = FirewallPolicyManager.ExceptAddress("192.0.2.0/24", "192.0.2.42");

        Assert.HasCount(8, remaining);
        Assert.IsFalse(System.Linq.Enumerable.Any(remaining, entry => FirewallPolicyManager.ContainsAddress(entry, "192.0.2.42")));
        Assert.IsTrue(System.Linq.Enumerable.Any(remaining, entry => FirewallPolicyManager.ContainsAddress(entry, "192.0.2.41")));
        Assert.IsTrue(System.Linq.Enumerable.Any(remaining, entry => FirewallPolicyManager.ContainsAddress(entry, "192.0.2.43")));
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

    /// <summary>
    /// 驗證 ParsedRuleAddresses 能正確且高效解析單一 IP、CIDR 網段、子網路遮罩與萬用字元，並完成批次比對。
    /// </summary>
    [TestMethod]
    public void ParsedRuleAddresses_BatchMatching_AccurateAndFast()
    {
        var parser = new FirewallPolicyManager.ParsedRuleAddresses();
        parser.AddRange("192.0.2.1, 198.51.100.0/24, 203.0.113.10/255.255.255.255, 2001:db8::/32");

        // 單一精確 IP
        Assert.IsTrue(parser.Contains(System.Net.IPAddress.Parse("192.0.2.1")));
        Assert.IsFalse(parser.Contains(System.Net.IPAddress.Parse("192.0.2.2")));

        // CIDR 前綴網段
        Assert.IsTrue(parser.Contains(System.Net.IPAddress.Parse("198.51.100.42")));
        Assert.IsFalse(parser.Contains(System.Net.IPAddress.Parse("198.51.101.42")));

        // 點分十進位主機遮罩
        Assert.IsTrue(parser.Contains(System.Net.IPAddress.Parse("203.0.113.10")));
        Assert.IsFalse(parser.Contains(System.Net.IPAddress.Parse("203.0.113.11")));

        // IPv6 網段
        Assert.IsTrue(parser.Contains(System.Net.IPAddress.Parse("2001:db8::1234")));
        Assert.IsFalse(parser.Contains(System.Net.IPAddress.Parse("2001:db9::1")));

        // 萬用字元
        var wildcardParser = new FirewallPolicyManager.ParsedRuleAddresses();
        wildcardParser.AddRange("*");
        Assert.IsTrue(wildcardParser.Contains(System.Net.IPAddress.Parse("1.2.3.4")));
        Assert.IsTrue(wildcardParser.Contains(System.Net.IPAddress.Parse("2001:db8::1")));
    }
}

