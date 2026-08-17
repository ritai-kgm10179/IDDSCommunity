using System.Text;
using IDDSCommunity.Agents.FtpServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.FtpServer.Test;

[TestClass]
public sealed class AppLayerFtpTest
{
    [TestMethod]
    public void IsAuthenticationFailure_LoginIncorrect_ReturnsTrue()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("530 Login incorrect.\r\n");
        AppLayerFtp reply = new(buffer, buffer.Length);

        Assert.IsTrue(reply.IsAuthenticationFailure);
        Assert.AreEqual("530", reply.FtpReplyCode);
    }

    [TestMethod]
    public void IsAuthenticationFailure_SuccessfulLogin_ReturnsFalse()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("230 User logged in, proceed.\r\n");
        AppLayerFtp reply = new(buffer, buffer.Length);

        Assert.IsFalse(reply.IsAuthenticationFailure);
    }

    [TestMethod]
    public void IsAuthenticationFailure_530WithUnrelatedReason_ReturnsFalse()
    {
        byte[] buffer = Encoding.ASCII.GetBytes("530 Not connected.\r\n");
        AppLayerFtp reply = new(buffer, buffer.Length);

        Assert.IsFalse(reply.IsAuthenticationFailure);
    }
}

[TestClass]
public sealed class FtpConfigTest
{
    [TestMethod]
    public void FtpPort_DefaultsTo21WhenUnset()
    {
        FtpConfig config = new();
        Assert.AreEqual(21, config.FtpPort);
    }

    [TestMethod]
    public void FtpPort_ZeroFallsBackToDefault()
    {
        FtpConfig config = new() { FtpPort = 2121 };
        config.FtpPort = 0;
        Assert.AreEqual(21, config.FtpPort);
    }
}
