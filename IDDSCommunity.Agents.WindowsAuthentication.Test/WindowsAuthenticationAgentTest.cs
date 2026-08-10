using IDDSCommunity.Agents.TerminalServer;
using IDDSCommunity.IntrusionDetection.Base.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.WindowsAuthentication.Test;

[TestClass]
public sealed class WindowsAuthenticationAgentTest
{
    [TestMethod]
    [DataRow("10", "0xC000006D", "0xC0000064")]
    [DataRow("10", "0xc000006d", "0xc000006a")]
    public void RdpCredentialFailure_IsCounted(string logonType, string status, string subStatus)
    {
        Assert.IsTrue(TlsSslAgent.IsCredentialFailure(logonType, status, subStatus));
    }

    [TestMethod]
    [DataRow("10", "0x0", "0x0")]
    [DataRow("7", "0xC000006D", "0xC000006A")]
    [DataRow("3", "0xC000006D", "0xC000006A")]
    public void SuccessfulOrNonRdpLogon_IsNotCounted(string logonType, string status, string subStatus)
    {
        Assert.IsFalse(TlsSslAgent.IsCredentialFailure(logonType, status, subStatus));
    }

    [TestMethod]
    public void WindowsLogonQuery_CountsOnlyCredentialFailures()
    {
        StringAssert.Contains(WindowsSecurityBase.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "EventID=4625");
        StringAssert.Contains(WindowsSecurityBase.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "0xC000006D");
        StringAssert.Contains(WindowsSecurityBase.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "0xC000006A");
    }

    [TestMethod]
    public void NtLmQuery_ExcludesSuccessfulCredentialValidation()
    {
        StringAssert.Contains(AdCredentialValidationSecurityAgent.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "EventID=4776");
        Assert.IsFalse(AdCredentialValidationSecurityAgent.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED.Contains("='0x0'", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void KerberosQuery_CountsOnlyBadPasswordPreAuthentication()
    {
        StringAssert.Contains(KerberosSecurityAgent.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "EventID=4771");
        StringAssert.Contains(KerberosSecurityAgent.EVENT_LOG_QUERY_WINDOWS_LOGIN_DENIED, "='0x18'");
    }
}
