using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.IntrusionDetection.Service.Test;

[TestClass]
public sealed class FirewallPolicyManagerTest
{
    /// <summary>
    /// Verifies that firewall lookup matches complete IP entries rather than substrings.
    /// </summary>
    [TestMethod]
    public void ContainsAddress_RequiresExactIpEntry()
    {
        Assert.IsTrue(FirewallPolicyManager.ContainsAddress("192.0.2.1,198.51.100.2/32", "198.51.100.2"));
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("11.2.3.40", "1.2.3.4"));
        Assert.IsFalse(FirewallPolicyManager.ContainsAddress("*", "1.2.3.4"));
    }
}
