using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDDSCommunity.Agents.FileZilla;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.FileZilla.Test;

[TestClass]
public class FileZillaLogParserTest
{
    [TestMethod]
    public void TryParseMessage_ValidAuthenticationFailedLine_ExtractsIp()
    {
        string line = "2026-08-13 16:30:00 [Session 123] 192.168.1.50 - User 'testuser' authentication failed";
        AuthenticationFailureEvent? failure = FileZillaLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNotNull(failure);
        Assert.AreEqual("192.168.1.50", failure.SourceAddress.ToString());
    }

    [TestMethod]
    public void TryParseMessage_Valid530Line_ExtractsIp()
    {
        string line = "2026-08-13 16:30:01 10.0.0.5 - 530 Password incorrect";
        AuthenticationFailureEvent? failure = FileZillaLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNotNull(failure);
        Assert.AreEqual("10.0.0.5", failure.SourceAddress.ToString());
    }

    [TestMethod]
    public void TryParseMessage_SuccessfulLogin_ReturnsNull()
    {
        string line = "2026-08-13 16:30:02 [Session 125] 192.168.1.50 - 230 User logged in successfully";
        AuthenticationFailureEvent? failure = FileZillaLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNull(failure);
    }
}
