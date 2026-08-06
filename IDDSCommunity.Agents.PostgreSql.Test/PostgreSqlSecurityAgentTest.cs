using IDDSCommunity.Agents.Authentication.Common;
using IDDSCommunity.Agents.PostgreSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.PostgreSql.Test;

[TestClass]
public sealed class PostgreSqlSecurityAgentTest
{
    [TestMethod]
    public void ParserRequiresFailureAndSourceAddress()
    {
        AuthenticationFailureEvent? failure = PostgreSqlSecurityAgent.TryParseLine("2026-08-05 host=192.0.2.20 FATAL: password authentication failed for user \"postgres\"");
        Assert.IsNotNull(failure);
        Assert.AreEqual("postgres", failure.AccountName);
        Assert.IsNull(PostgreSqlSecurityAgent.TryParseLine("FATAL: password authentication failed for user \"postgres\""));

        AuthenticationFailureEvent? json = PostgreSqlSecurityAgent.TryParseLine("{\"timestamp\":\"2026-08-05T03:04:05Z\",\"user\":\"postgres\",\"remote_host\":\"192.0.2.21\",\"message\":\"password authentication failed for user postgres\"}");
        Assert.IsNotNull(json);
        Assert.AreEqual("192.0.2.21", json.SourceAddress.ToString());
    }
}
