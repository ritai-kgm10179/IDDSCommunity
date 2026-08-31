using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 SlowAndLowAttackDetector 慢速隱蔽攻擊異常分析引擎。
/// </summary>
[TestClass]
public sealed class SlowAndLowAttackDetectorTest
{
    /// <summary>
    /// 驗證跨 48 小時慢速密碼噴灑事件逐步累積異常分數並觸發警告事件。
    /// </summary>
    [TestMethod]
    public void SlowAndLowAttackDetector_MultiDaySpraying_TriggersDetection()
    {
        var detector = new SlowAndLowAttackDetector(halfLifeHours: 24.0, anomalyThreshold: 8.0);
        string attackerIp = "198.51.100.200";
        bool detected = false;
        double finalScore = 0.0;

        detector.SlowAndLowAttackDetected += (ip, score, uniqueAccounts, agent) =>
        {
            if (ip == attackerIp)
            {
                detected = true;
                finalScore = score;
            }
        };

        DateTime baseTime = new(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);
        string[] targetAccounts = ["admin", "root", "user1", "user2", "backup", "vpn", "test", "support", "guest", "deploy"];

        // 模擬每 2 小時嘗試 1 個不同的帳號，持續 20 次 (共 40 小時)
        for (int i = 0; i < targetAccounts.Length * 2; i++)
        {
            DateTime eventTime = baseTime.AddHours(i * 2);
            string account = targetAccounts[i % targetAccounts.Length];
            detector.RecordEvent(attackerIp, account, "TerminalServer", eventTime);
        }

        Assert.IsTrue(detected, "Slow & Low attack across 40 hours should be detected.");
        Assert.IsTrue(finalScore >= 8.0, $"Final score {finalScore} must exceed threshold 8.0.");
    }

    /// <summary>
    /// 驗證長時間無活動之事件指數衰減，分數自然下降。
    /// </summary>
    [TestMethod]
    public void SlowAndLowAttackDetector_DecaysOverTime()
    {
        var detector = new SlowAndLowAttackDetector(halfLifeHours: 24.0, anomalyThreshold: 10.0);
        string ip = "198.51.100.201";
        DateTime start = new(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);

        // 第 0 小時發動 3 次攻擊
        detector.RecordEvent(ip, "user1", "OpenSsh", start);
        detector.RecordEvent(ip, "user1", "OpenSsh", start.AddMinutes(5));
        double scoreImmediate = detector.RecordEvent(ip, "user1", "OpenSsh", start.AddMinutes(10));

        // 經過 48 小時無任何活動 (2 個半衰期)
        double scoreAfter48Hours = detector.GetCurrentScore(ip, start.AddHours(48));

        Assert.IsTrue(scoreAfter48Hours < scoreImmediate, "Score should decay significantly over 48 hours.");
        Assert.IsTrue(scoreAfter48Hours < scoreImmediate * 0.35, "Score after 2 half-lives should be ~25% of immediate score.");
    }
}
