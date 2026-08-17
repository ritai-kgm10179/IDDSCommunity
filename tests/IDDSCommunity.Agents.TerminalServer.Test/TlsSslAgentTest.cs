using IDDSCommunity.Agents.TerminalServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.TerminalServer.Test;

[TestClass]
public sealed class TlsSslAgentTest
{
    [TestMethod]
    public void IsCredentialFailure_RequiresNetworkLogonType10()
    {
        Assert.IsTrue(TlsSslAgent.IsCredentialFailure("10", "0xC000006D", "0x0"));
        Assert.IsFalse(TlsSslAgent.IsCredentialFailure("3", "0xC000006D", "0x0"));
    }

    [TestMethod]
    public void IsCredentialFailure_AcceptsKnownCredentialStatusOrSubStatus()
    {
        Assert.IsTrue(TlsSslAgent.IsCredentialFailure("10", "0x0", "0xC0000064"));
        Assert.IsTrue(TlsSslAgent.IsCredentialFailure("10", "0x0", "0xC000006A"));
        Assert.IsFalse(TlsSslAgent.IsCredentialFailure("10", "0x0", "0x0"));
    }
}

[TestClass]
public sealed class AppLayerTlsSslTest
{
    [TestMethod]
    public void ParsesHandshakeRecordHeader()
    {
        byte[] buffer = [0x16, 0x03, 0x01, 0x05, 0x00];
        AppLayerTlsSsl record = new(buffer, buffer.Length);

        Assert.AreEqual(AppLayerTlsSsl.CONTENT_TYPE_HANDSHAKE, record.TlsHeader.ContentType);
        Assert.AreEqual((byte)0x03, record.TlsHeader.MajorVersion);
        Assert.AreEqual((byte)0x01, record.TlsHeader.MinorVersion);
        Assert.AreEqual((ushort)5, record.TlsHeader.Length);
    }
}

[TestClass]
public sealed class TslSslConfigTest
{
    [TestMethod]
    public void RdpPort_DefaultsTo3389WhenUnset()
    {
        TslSslConfig config = new();
        Assert.AreEqual(3389, config.RdpPort);
    }

    [TestMethod]
    public void RdpPort_ZeroFallsBackToDefault()
    {
        TslSslConfig config = new() { RdpPort = 33890 };
        config.RdpPort = 0;
        Assert.AreEqual(3389, config.RdpPort);
    }
}
