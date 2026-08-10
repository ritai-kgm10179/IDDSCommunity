using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.MailServer.Test;

[TestClass]
public sealed class MailAgentConfigurationTest
{
    /// <summary>
    /// 驗證 SMTP、IMAP 與 POP3 各自只公開所屬的連接埠設定。
    /// </summary>
    [TestMethod]
    public void MailAgents_ExposeIndependentPortSettings()
    {
        AssertConfiguration(new SmtpAgent(), typeof(SmtpConfig), nameof(SmtpConfig.SmtpPort));
        AssertConfiguration(new ImapAgent(), typeof(ImapConfig), nameof(ImapConfig.ImapPort));
        AssertConfiguration(new Pop3Agent(), typeof(Pop3Config), nameof(Pop3Config.Pop3Port));
    }

    /// <summary>
    /// 驗證三個 Mail Agent 使用互不重複的固定識別碼。
    /// </summary>
    [TestMethod]
    public void MailAgents_UseDistinctStableIdentifiers()
    {
        Guid[] identifiers = [SmtpAgent.AgentId, ImapAgent.AgentId, Pop3Agent.AgentId];

        Assert.HasCount(3, identifiers.Distinct().ToArray());
        Assert.IsFalse(identifiers.Contains(Guid.Empty));
    }

    private static void AssertConfiguration(
        global::IDDSCommunity.IntrusionDetection.Api.Plugin.IAgentPlugin agent,
        Type expectedType,
        string expectedProperty)
    {
        object? settings = agent.Configuration.AgentSettings;
        Assert.IsNotNull(settings);
        Assert.AreEqual(expectedType, settings.GetType());
        string[] properties = settings.GetType().GetProperties().Select(property => property.Name).ToArray();
        CollectionAssert.AreEqual(new[] { expectedProperty }, properties);
    }
}
