using System;
using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.OpenSsh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.OpenSsh.Test;

[TestClass]
public sealed class OpenSshSecurityAgentTest
{
    [TestMethod]
    public void ParserSupportsIpv4AndInvalidUser()
    {
        AuthenticationFailureEvent? failure = OpenSshSecurityAgent.TryParseMessage("Failed password for invalid user admin from 203.0.113.8 port 50000 ssh2", DateTimeOffset.UtcNow);
        Assert.IsNotNull(failure);
        Assert.AreEqual("203.0.113.8", failure.SourceAddress.ToString());
        Assert.AreEqual("admin", failure.AccountName);
    }

    [TestMethod]
    public void ConfigurationRequiresAtLeastOneLogSource()
    {
        OpenSshConfiguration configuration = new() { ReadEventLog = false, LogFilePath = string.Empty };

        Assert.ThrowsExactly<InvalidOperationException>(configuration.Validate);
    }
}
