using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IDDSCommunity.Agents.TechnitiumDns;
using IDDSCommunity.Agents.Authentication.Common;

namespace IDDSCommunity.Agents.TechnitiumDns.Test;

[TestClass]
public class TechnitiumDnsLogParserTest
{
    [TestMethod]
    public void TryParseMessage_RefusedQueryLine_ExtractsIp()
    {
        string line = "2026-08-13 16:30:00 [QueryLog] Client 192.168.1.50:53123 - Refused query for example.com";
        AuthenticationFailureEvent? failure = TechnitiumDnsLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNotNull(failure);
        Assert.AreEqual("192.168.1.50", failure.SourceAddress.ToString());
    }

    [TestMethod]
    public void TryParseMessage_QpmLimitExceeded_ExtractsIp()
    {
        string line = "2026-08-13 16:30:01 [QpmLimit] Client 10.0.0.5 exceeded QPM limit";
        AuthenticationFailureEvent? failure = TechnitiumDnsLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNotNull(failure);
        Assert.AreEqual("10.0.0.5", failure.SourceAddress.ToString());
    }

    [TestMethod]
    public void TryParseMessage_NormalResponse_ReturnsNull()
    {
        string line = "2026-08-13 16:30:02 [QueryLog] Client 192.168.1.50:53123 - NOERROR query for google.com";
        AuthenticationFailureEvent? failure = TechnitiumDnsLogParser.TryParseMessage(line, DateTimeOffset.UtcNow);

        Assert.IsNull(failure);
    }
}
