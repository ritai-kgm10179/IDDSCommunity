using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class CidrMatcherTest
{
    [TestMethod]
    public void TryMatchCidr_MatchesIpWithinRange()
    {
        Assert.IsTrue(CidrMatcher.TryMatchCidr("192.168.1.0/24", "192.168.1.50"));
        Assert.IsTrue(CidrMatcher.TryMatchCidr("192.168.1.0/24", "192.168.1.254"));
        Assert.IsFalse(CidrMatcher.TryMatchCidr("192.168.1.0/24", "192.168.2.1"));
        Assert.IsTrue(CidrMatcher.TryMatchCidr("10.0.0.0/8", "10.255.4.1"));
        Assert.IsFalse(CidrMatcher.TryMatchCidr("10.0.0.0/8", "11.0.0.1"));
    }

    [TestMethod]
    public void TryMatchCidr_HandlesInvalidInputGracefully()
    {
        Assert.IsFalse(CidrMatcher.TryMatchCidr("invalid", "192.168.1.1"));
        Assert.IsFalse(CidrMatcher.TryMatchCidr("192.168.1.0/33", "192.168.1.1"));
        Assert.IsFalse(CidrMatcher.TryMatchCidr("192.168.1.0/24", "notanip"));
    }
}
