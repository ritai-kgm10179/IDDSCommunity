using System;
using System.Collections.Generic;
using IDDSCommunity.Agents.Radius;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.Radius.Test;

[TestClass]
public sealed class RadiusSecurityAgentTest
{
    [TestMethod]
    public void ParserRequiresCredentialMismatchReason()
    {
        Dictionary<string, string> fields = new() { ["ReasonCode"] = "16", ["ClientIPAddress"] = "203.0.113.21", ["UserName"] = "vpn-user" };
        Assert.IsNotNull(RadiusSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
        fields["ReasonCode"] = "48";
        Assert.IsNull(RadiusSecurityAgent.TryParseFields(fields, DateTimeOffset.UtcNow));
    }
}
