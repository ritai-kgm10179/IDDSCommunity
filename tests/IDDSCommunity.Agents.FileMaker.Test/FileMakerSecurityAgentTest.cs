using IDDSCommunity.Agents.FileMaker;
using IDDSCommunity.IntrusionDetection.Api.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.FileMaker.Test;

[TestClass]
public sealed class FileMakerSecurityAgentTest
{
    [TestMethod]
    public void EventLogQueryTargetsLoginDeniedEventId()
    {
        StringAssert.Contains(FileMakerSecurityAgent.EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED, "EventID=661");
        StringAssert.Contains(FileMakerSecurityAgent.EVENT_LOG_QUERY_FILEMAKER_LOGIN_DENIED, "Path=\"Application\"");
    }

    [TestMethod]
    public void AgentExposesStableIdentityAndDisplayName()
    {
        FileMakerSecurityAgent agent = new();

        Assert.AreEqual(Strings.Get("FileMaker Security Agent"), agent.DisplayName);
        Assert.AreNotEqual(System.Guid.Empty, agent.Id);
    }
}
