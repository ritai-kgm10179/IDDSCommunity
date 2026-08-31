using System;
using IDDSCommunity.IntrusionDetection.Shared.Deception;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 HoneyAccountDetector 誘餌帳號欺敵偵測引擎功能。
/// </summary>
[TestClass]
public sealed class HoneyAccountDetectorTest
{
    /// <summary>
    /// 驗證誘餌帳號判定與不同網域格式正規化比對。
    /// </summary>
    [TestMethod]
    public void IsHoneyAccount_ValidAndInvalidAccounts_RecognizedCorrectly()
    {
        var detector = new HoneyAccountDetector("admin_backup,root_trap;canary_user");

        Assert.AreEqual(3, detector.Count);
        Assert.IsTrue(detector.IsHoneyAccount("admin_backup"));
        Assert.IsTrue(detector.IsHoneyAccount("ADMIN_BACKUP"));
        Assert.IsTrue(detector.IsHoneyAccount("CORP\\root_trap"));
        Assert.IsTrue(detector.IsHoneyAccount("canary_user@corp.local"));
        Assert.IsFalse(detector.IsHoneyAccount("legitimate_user"));
        Assert.IsFalse(detector.IsHoneyAccount(null));
        Assert.IsFalse(detector.IsHoneyAccount(""));
    }

    /// <summary>
    /// 驗證 CheckAndReport 觸發 HoneyAccountBreached 事件。
    /// </summary>
    [TestMethod]
    public void CheckAndReport_BreachDetected_TriggersEvent()
    {
        var detector = new HoneyAccountDetector("honeypot_admin");
        bool eventFired = false;
        string breachedIp = string.Empty;
        string breachedAccount = string.Empty;

        detector.HoneyAccountBreached += (ip, account, agent) =>
        {
            eventFired = true;
            breachedIp = ip;
            breachedAccount = account;
        };

        bool result = detector.CheckAndReport("198.51.100.77", "CORP\\honeypot_admin", "WindowsLogon");

        Assert.IsTrue(result);
        Assert.IsTrue(eventFired);
        Assert.AreEqual("198.51.100.77", breachedIp);
        Assert.AreEqual("CORP\\honeypot_admin", breachedAccount);
    }
}
