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

        // Alibaba Cloud ECS IMDS 100.100.100.200
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocal("http://100.100.100.200/latest/meta-data/"));
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

    [TestMethod]
    public void IsBlockedImdsOrLinkLocalAddress_DetectsAndBlocksCorrectly()
    {
        // Null
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(null));

        // Normal IPs
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("1.1.1.1")));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("8.8.8.8")));
        Assert.IsFalse(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("2606:4700:4700::1111")));

        // IPv4 IMDS & Link-Local
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("169.254.169.254")));
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("169.254.1.50")));

        // Alibaba Cloud ECS IMDS 100.100.100.200
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("100.100.100.200")));

        // IPv4-mapped IPv6 IMDS
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("::ffff:169.254.169.254")));
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("::ffff:100.100.100.200")));

        // IPv6 Link-Local
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("fe80::1")));

        // AWS IPv6 IMDS
        Assert.IsTrue(NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(System.Net.IPAddress.Parse("fd00:ec2::254")));
    }

    [TestMethod]
    public async System.Threading.Tasks.Task HttpClientHelper_BlocksImdsConnectionAtSocketLayer()
    {
        using var client = HttpClientHelper.CreatePooledClient(timeout: System.TimeSpan.FromSeconds(5));

        var ex = await Assert.ThrowsExactlyAsync<System.Net.Http.HttpRequestException>(async () =>
        {
            await client.GetAsync("http://169.254.169.254/latest/meta-data/");
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<System.InvalidOperationException>(ex.InnerException);
        StringAssert.Contains(ex.InnerException.Message, "169.254.169.254");
    }

    [TestMethod]
    public async System.Threading.Tasks.Task HttpClientHelper_BlocksCustomFilterAtSocketLayer()
    {
        using var client = HttpClientHelper.CreatePooledClient(
            timeout: System.TimeSpan.FromSeconds(5),
            customBlockFilter: ip => System.Net.IPAddress.IsLoopback(ip));

        var ex = await Assert.ThrowsExactlyAsync<System.Net.Http.HttpRequestException>(async () =>
        {
            await client.GetAsync("http://127.0.0.1:65530/test");
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<System.InvalidOperationException>(ex.InnerException);
        StringAssert.Contains(ex.InnerException.Message, "127.0.0.1");
    }
}