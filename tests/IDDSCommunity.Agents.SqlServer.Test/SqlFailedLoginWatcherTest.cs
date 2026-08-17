using IDDSCommunity.Agents.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.SqlServer.Test;

[TestClass]
public sealed class SqlFailedLoginWatcherTest
{
    [TestMethod]
    public void EventLogQueryTargetsLoginFailedEventId()
    {
        StringAssert.Contains(SqlFailedLoginWatcher.EVENT_LOG_QUERY_SQL_SERVER_LOGIN_DENIED, "EventID=18456");
        StringAssert.Contains(SqlFailedLoginWatcher.EVENT_LOG_QUERY_SQL_SERVER_LOGIN_DENIED, "Path=\"Application\"");
    }

    [TestMethod]
    public void AgentExposesStableIdentityAndDisplayName()
    {
        SqlFailedLoginWatcher agent = new();

        Assert.AreEqual("SQL Server Security Agent", agent.DisplayName);
        Assert.AreNotEqual(System.Guid.Empty, agent.Id);
    }
}
