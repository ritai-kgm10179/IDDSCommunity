using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class WebhookConnectionPolicyTest
{
    private sealed class ResponseStream(string response) : Stream
    {
        private readonly MemoryStream reader = new(Encoding.ASCII.GetBytes(response));
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => reader.Length;
        public override long Position { get => reader.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => reader.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task Redirect_IsReturnedWithoutConnectingToTarget()
    {
        int connections = 0;
        using HttpClient client = WebhookConnectionPolicy.CreateClientForTests(
            "http://hooks.example.com/start", null, TimeSpan.FromSeconds(2),
            (_, _, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }),
            (_, _, _) =>
            {
                Interlocked.Increment(ref connections);
                return Task.FromResult<Stream>(new ResponseStream("HTTP/1.1 302 Found\r\nLocation: http://10.0.0.1/secret\r\nContent-Length: 0\r\n\r\n"));
            });
        using HttpResponseMessage reply = await client.GetAsync("http://hooks.example.com/start");
        Assert.AreEqual(HttpStatusCode.Found, reply.StatusCode);
        Assert.AreEqual(1, connections);
    }
    [TestMethod]
    public void PrivateDestination_RequiresExactHttpsOriginAndAddress()
    {
        Uri destination = new("https://hooks.example.com:9443/path");
        IPAddress privateIp = IPAddress.Parse("192.168.10.5");
        Assert.IsFalse(WebhookConnectionPolicy.IsAllowed(destination, privateIp, null));
        Assert.IsFalse(WebhookConnectionPolicy.IsAllowed(destination, privateIp, "https://hooks.example.com:9444|192.168.10.0/24"));
        Assert.IsTrue(WebhookConnectionPolicy.IsAllowed(destination, privateIp, "https://hooks.example.com:9443|192.168.10.0/24"));
        Assert.IsFalse(WebhookConnectionPolicy.IsAllowed(destination, IPAddress.Parse("169.254.169.254"), "https://hooks.example.com:9443|0.0.0.0/0"));
        Assert.IsFalse(WebhookConnectionPolicy.IsAllowed(destination, IPAddress.Parse("fe80::1"), "https://hooks.example.com:9443|::/0"));
    }

    [TestMethod]
    public async Task MixedDnsAnswer_IsBlockedBeforeSocketCreation()
    {
        int connections = 0;
        using HttpClient client = WebhookConnectionPolicy.CreateClientForTests(
            "http://hooks.example.com/test", null, TimeSpan.FromSeconds(2),
            (_, _, _) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("10.0.0.8") }),
            (_, _, _) => { Interlocked.Increment(ref connections); return Task.FromResult<Stream>(new MemoryStream()); });
        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () => await client.GetAsync("http://hooks.example.com/test"));
        Assert.AreEqual(0, connections);
    }

    [TestMethod]
    public async Task AuthorizedPrivateAddress_ReachesConnector_ImdsNeverDoes()
    {
        static async Task<int> ConnectionsFor(IPAddress answer)
        {
            int count = 0;
            using HttpClient client = WebhookConnectionPolicy.CreateClientForTests(
                "https://hooks.example.com:9443/test", "https://hooks.example.com:9443|0.0.0.0/0",
                TimeSpan.FromSeconds(2), (_, _, _) => Task.FromResult(new[] { answer }),
                (_, _, _) => { Interlocked.Increment(ref count); return Task.FromResult<Stream>(new MemoryStream()); });
            try { await client.GetAsync("https://hooks.example.com:9443/test"); }
            catch (HttpRequestException) { }
            return count;
        }
        Assert.AreEqual(1, await ConnectionsFor(IPAddress.Parse("192.168.10.5")));
        Assert.AreEqual(0, await ConnectionsFor(IPAddress.Parse("169.254.169.254")));
    }
}
