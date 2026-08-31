using System;
using System.Collections.Generic;
using IDDSCommunity.Agents.ActiveDirectory;
using IDDSCommunity.Agents.Authentication.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.ActiveDirectory.Test;

/// <summary>
/// 驗證 ActiveDirectorySecurityAgent 針對 Kerberoasting、AS-REP Roasting 與帳號鎖定攻擊之解析。
/// </summary>
[TestClass]
public sealed class ActiveDirectorySecurityAgentTest
{
    /// <summary>
    /// 驗證 Kerberoasting 弱加密 (RC4-HMAC / 0x17) TGS 請求能精準解析。
    /// </summary>
    [TestMethod]
    public void TryParseFields_KerberoastingRc4_ReturnsFailureEvent()
    {
        var fields = new Dictionary<string, string>
        {
            ["IpAddress"] = "::ffff:198.51.100.45",
            ["TargetUserName"] = "victim_admin",
            ["ServiceName"] = "MSSQLSvc/db01.corp.local:1433",
            ["TicketEncryptionType"] = "0x17",
            ["TargetDomainName"] = "CORP"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? result = ActiveDirectorySecurityAgent.TryParseFields(fields, now, 4769);

        Assert.IsNotNull(result);
        Assert.AreEqual("198.51.100.45", result.SourceAddress.ToString());
        Assert.AreEqual("ActiveDirectory.Kerberoasting", result.Category);
        Assert.AreEqual("victim_admin", result.AccountName);
        Assert.AreEqual("0x17", result.ErrorCode);
    }

    /// <summary>
    /// 驗證正常 Kerberos AES256 (0x12) TGS 請求不會誤判為攻擊。
    /// </summary>
    [TestMethod]
    public void TryParseFields_NormalKerberosAes_ReturnsNull()
    {
        var fields = new Dictionary<string, string>
        {
            ["IpAddress"] = "198.51.100.45",
            ["TargetUserName"] = "user01",
            ["ServiceName"] = "HTTP/web.corp.local",
            ["TicketEncryptionType"] = "0x12",
            ["TargetDomainName"] = "CORP"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? result = ActiveDirectorySecurityAgent.TryParseFields(fields, now, 4769);

        Assert.IsNull(result);
    }

    /// <summary>
    /// 驗證 Kerberos Pre-Authentication 失敗 (4771) 能精準解析。
    /// </summary>
    [TestMethod]
    public void TryParseFields_KerberosPreAuthFailed_ReturnsFailureEvent()
    {
        var fields = new Dictionary<string, string>
        {
            ["ClientAddress"] = "203.0.113.88",
            ["TargetUserName"] = "admin_user",
            ["Status"] = "0x18", // KDC_ERR_PREAUTH_FAILED
            ["TargetDomainName"] = "CORP"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? result = ActiveDirectorySecurityAgent.TryParseFields(fields, now, 4771);

        Assert.IsNotNull(result);
        Assert.AreEqual("203.0.113.88", result.SourceAddress.ToString());
        Assert.AreEqual("ActiveDirectory.PreAuthFailed", result.Category);
    }

    /// <summary>
    /// 驗證 AD 帳號鎖定 (4740) 觸發 DoS 防護事件。
    /// </summary>
    [TestMethod]
    public void TryParseFields_AccountLockout_ReturnsFailureEvent()
    {
        var fields = new Dictionary<string, string>
        {
            ["SourceNetworkAddress"] = "198.51.100.99",
            ["TargetUserName"] = "sales_rep",
            ["TargetDomainName"] = "CORP"
        };

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthenticationFailureEvent? result = ActiveDirectorySecurityAgent.TryParseFields(fields, now, 4740);

        Assert.IsNotNull(result);
        Assert.AreEqual("198.51.100.99", result.SourceAddress.ToString());
        Assert.AreEqual("ActiveDirectory.AccountLockout", result.Category);
    }
}
