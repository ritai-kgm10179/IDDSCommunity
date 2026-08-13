using System;
using System.Collections.Generic;
using IDDSCommunity.Agents.WindowsNetworkLogon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.WindowsNetworkLogon.Test;

[TestClass]
public sealed class WindowsNetworkLogonSecurityAgentTest
{
    [TestMethod]
    public void ParserRejectsNonCredentialFailures()
    {
        Dictionary<string, string> fields = new() { ["LogonType"] = "3", ["Status"] = "0xC000006D", ["SubStatus"] = "0xC000006A", ["IpAddress"] = "198.51.100.10", ["TargetUserName"] = "service" };
        Assert.IsNotNull(WindowsNetworkLogonSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
        fields["SubStatus"] = "0xC000015B";
        fields["Status"] = "0xC000015B";
        Assert.IsNull(WindowsNetworkLogonSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
    }
}
