using System;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 SyslogPayloadBuilder 產生之 RFC 5424、RFC 3164 與 ArcSight CEF 格式訊息。
/// </summary>
[TestClass]
public sealed class SyslogPayloadBuilderTest
{
    /// <summary>
    /// 驗證 RFC 5424 結構化 Syslog 訊息產生。
    /// </summary>
    [TestMethod]
    public void BuildMessage_Rfc5424_FormatsCorrectly()
    {
        DateTime time = new(2026, 8, 31, 5, 0, 0, DateTimeKind.Utc);
        string message = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Rfc5424,
            LockType.HardLock,
            "198.51.100.99",
            "TerminalServer",
            "Multiple RDP failed logons",
            time,
            "TEST-HOST");

        Assert.IsNotNull(message);
        Assert.IsTrue(message.StartsWith("<33>1 2026-08-31T05:00:00.000Z TEST-HOST IDDSCommunity", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("[intrusion@41123 srcIp=\"198.51.100.99\" agent=\"TerminalServer\" action=\"HardLock\"]", StringComparison.Ordinal));
        Assert.IsTrue(message.EndsWith("Multiple RDP failed logons", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 RFC 3164 傳統 BSD Syslog 訊息產生。
    /// </summary>
    [TestMethod]
    public void BuildMessage_Rfc3164_FormatsCorrectly()
    {
        DateTime time = new(2026, 8, 31, 5, 0, 0, DateTimeKind.Utc);
        string message = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Rfc3164,
            LockType.SoftLock,
            "198.51.100.99",
            "OpenSsh",
            "Auth failed",
            time,
            "TEST-HOST");

        Assert.IsNotNull(message);
        Assert.IsTrue(message.StartsWith("<36>", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("TEST-HOST IDDSCommunity[", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("[OpenSsh] SoftLock for IP 198.51.100.99 - Auth failed", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 ArcSight CEF 格式訊息產生。
    /// </summary>
    [TestMethod]
    public void BuildMessage_Cef_FormatsCorrectly()
    {
        DateTime time = new(2026, 8, 31, 5, 0, 0, DateTimeKind.Utc);
        string message = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Cef,
            LockType.HardLock,
            "198.51.100.99",
            "SqlServer",
            "SA login brute-force",
            time,
            "TEST-HOST");

        Assert.IsNotNull(message);
        Assert.IsTrue(message.StartsWith("CEF:0|IDDSCommunity|IntrusionDetection|1.0|HardLock|HardLock Applied|8|", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("src=198.51.100.99", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("cs1Label=Agent cs1=SqlServer", StringComparison.Ordinal));
        Assert.IsTrue(message.Contains("dhost=TEST-HOST", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證包含換行字元時全面被清洗為空白，杜絕 Syslog 日誌注入攻擊 (CWE-117)。
    /// </summary>
    [TestMethod]
    public void BuildMessage_SanitizesCrlf_PreventsLogInjection()
    {
        DateTime time = new(2026, 8, 31, 5, 0, 0, DateTimeKind.Utc);
        string maliciousDetails = "Normal message\r\n<0>1 Fake injected syslog header";
        string maliciousAgent = "Agent\r\nName";

        string rfc5424 = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Rfc5424, LockType.HardLock, "198.51.100.99", maliciousAgent, maliciousDetails, time, "TEST-HOST");
        Assert.IsFalse(rfc5424.Contains('\r'));
        Assert.IsFalse(rfc5424.Contains('\n'));

        string rfc3164 = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Rfc3164, LockType.HardLock, "198.51.100.99", maliciousAgent, maliciousDetails, time, "TEST-HOST");
        Assert.IsFalse(rfc3164.Contains('\r'));
        Assert.IsFalse(rfc3164.Contains('\n'));

        string cef = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Cef, LockType.HardLock, "198.51.100.99", maliciousAgent, maliciousDetails, time, "TEST-HOST");
        Assert.IsFalse(cef.Contains('\r'));
        Assert.IsFalse(cef.Contains('\n'));
    }

    /// <summary>
    /// 驗證 RFC 5424 結構化資料中特殊字元 (反斜線、雙引號、右中括號) 確實依規範轉義。
    /// </summary>
    [TestMethod]
    public void BuildMessage_Rfc5424_EscapesSdParamSpecialChars()
    {
        DateTime time = new(2026, 8, 31, 5, 0, 0, DateTimeKind.Utc);
        string agentWithSpecial = "Agent]With\"Special\\Chars";

        string rfc5424 = SyslogPayloadBuilder.BuildMessage(
            SyslogFormat.Rfc5424, LockType.HardLock, "198.51.100.99", agentWithSpecial, "Test details", time, "TEST-HOST");

        Assert.IsTrue(rfc5424.Contains("agent=\"Agent\\]With\\\"Special\\\\Chars\"", StringComparison.Ordinal));
    }
}
