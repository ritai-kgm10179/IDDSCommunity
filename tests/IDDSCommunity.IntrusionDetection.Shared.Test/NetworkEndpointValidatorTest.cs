using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDDSCommunity.IntrusionDetection.Shared.Network;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class NetworkEndpointValidatorTest
{
    [TestMethod]
    public void IsBlockedImdsOrLinkLocal_DetectsAndBlocksImdsAndLinkLocalUrls()
    {
        // IPv4 IMDS (AWS / Azure / GCP)
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://169.254.169.254/latest/meta-data/"));
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://169.254.169.254/metadata/instance"));
        
        // IPv4 Link-Local general range 169.254.0.0/16
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://169.254.1.1/webhook"));

        // AWS IPv6 IMDS fd00:ec2::254
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://[fd00:ec2::254]/latest/meta-data/"));

        // IPv6 Link-Local fe80::/10
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://[fe80::1ff:fe00:1]/"));

        // AWS instance-data
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://instance-data/latest/meta-data/"));
    }

    [TestMethod]
    public void IsBlockedImdsOrLinkLocal_AllowsLegitimateUrls()
    {
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://hooks.slack.com/services/T00/B00/XXXX"));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://discord.com/api/webhooks/123/xyz"));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://api.telegram.org/bot12345/sendMessage"));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://gateway.corp.local/api/v1/firewall"));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("https://203.0.113.50:8443/webhook"));
    }

    [TestMethod]
    public void IsBlockedImdsOrLinkLocal_HandlesInvalidOrEmptyInputsGracefully()
    {
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal(null));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal(string.Empty));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("   "));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("not a valid url"));
    }
}