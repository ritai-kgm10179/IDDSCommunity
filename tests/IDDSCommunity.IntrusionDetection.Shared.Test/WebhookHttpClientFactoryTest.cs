using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class WebhookHttpClientFactoryTest
{
    [TestMethod]
    public void NamedClients_ReuseHandlersButSeparatePublicAndPrivatePools()
    {
        using WebhookHttpClientFactory factory = new(
            (_, _, _) => Task.FromResult(Array.Empty<IPAddress>()),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream()));
        Assert.AreSame(factory.GetHandlerForTests(false), factory.GetHandlerForTests(false));
        Assert.AreSame(factory.GetHandlerForTests(true), factory.GetHandlerForTests(true));
        Assert.AreNotSame(factory.GetHandlerForTests(false), factory.GetHandlerForTests(true));
        Assert.IsFalse(WebhookConnectionPolicy.HasPrivateAllowanceFor(new Uri("https://api.telegram.org/sendMessage"),
            "https://hooks.example.com:9443|192.168.10.0/24"));
        Assert.IsTrue(WebhookConnectionPolicy.HasPrivateAllowanceFor(new Uri("https://hooks.example.com:9443/test"),
            "https://hooks.example.com:9443|192.168.10.0/24"));
    }

    [TestMethod]
    public async Task PrivatePermissionRevocation_DoesNotUsePreviouslyAllowedConnection()
    {
        int connections = 0;
        using WebhookHttpClientFactory factory = new(
            (_, _, _) => Task.FromResult(new[] { IPAddress.Parse("192.168.10.5") }),
            (_, _, _) =>
            {
                Interlocked.Increment(ref connections);
                return Task.FromResult<Stream>(new MemoryStream());
            });
        using (HttpRequestMessage allowed = new(HttpMethod.Get, "https://hooks.example.com:9443/test"))
        {
            await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
                await factory.SendAsync(allowed, allowed.RequestUri!.ToString(),
                    "https://hooks.example.com:9443|192.168.10.0/24", TimeSpan.FromSeconds(2), CancellationToken.None));
        }
        Assert.AreEqual(1, connections);
        using (HttpRequestMessage revoked = new(HttpMethod.Get, "https://hooks.example.com:9443/test"))
        {
            await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
                await factory.SendAsync(revoked, revoked.RequestUri!.ToString(), null,
                    TimeSpan.FromSeconds(2), CancellationToken.None));
        }
        Assert.AreEqual(1, connections);
    }

    [TestMethod]
    public async Task MixedDnsAnswer_IsBlockedByFactoryBeforeConnecting()
    {
        int connections = 0;
        using WebhookHttpClientFactory factory = new(
            (_, _, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("10.0.0.8") }),
            (_, _, _) => { Interlocked.Increment(ref connections); return Task.FromResult<Stream>(new MemoryStream()); });
        using HttpRequestMessage request = new(HttpMethod.Get, "http://hooks.example.com/test");
        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
            await factory.SendAsync(request, request.RequestUri!.ToString(), null, TimeSpan.FromSeconds(2), CancellationToken.None));
        Assert.AreEqual(0, connections);
    }
}
