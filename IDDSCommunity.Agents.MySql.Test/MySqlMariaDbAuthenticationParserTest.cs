using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.MySql.Test;

[TestClass]
public sealed class MySqlMariaDbAuthenticationParserTest
{
    [TestMethod]
    public void ParsesMySqlIpv4AccessDeniedMessage()
    {
        bool parsed = MySqlMariaDbAuthenticationParser.TryParse(
            "MySQL",
            ["[MY-010926] Access denied for user 'root'@'203.0.113.24' (using password: YES)"],
            out IPAddress? address);

        Assert.IsTrue(parsed);
        Assert.AreEqual("203.0.113.24", address!.ToString());
    }

    [TestMethod]
    public void ParsesMariaDbIpv6AccessDeniedMessage()
    {
        bool parsed = MySqlMariaDbAuthenticationParser.TryParse(
            "MariaDB",
            ["Access denied for user 'admin'@'[2001:db8::42]' (using password: YES)"],
            out IPAddress? address);

        Assert.IsTrue(parsed);
        Assert.AreEqual("2001:db8::42", address!.ToString());
    }

    [TestMethod]
    public void RejectsUnknownProviderAndNonIpHost()
    {
        Assert.IsFalse(MySqlMariaDbAuthenticationParser.TryParse(
            "UnrelatedService",
            ["Access denied for user 'root'@'203.0.113.24' (using password: YES)"],
            out _));
        Assert.IsFalse(MySqlMariaDbAuthenticationParser.TryParse(
            "MariaDB",
            ["Access denied for user 'root'@'workstation.example' (using password: YES)"],
            out _));
    }

    [TestMethod]
    public void EventQueryIncludesStandardProviders()
    {
        StringAssert.Contains(MySqlFailedLoginWatcher.EventLogQuery, "@Name='MySQL'");
        StringAssert.Contains(MySqlFailedLoginWatcher.EventLogQuery, "@Name='MariaDB'");
    }
}
