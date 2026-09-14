using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class ReportCountingTest
{
    [TestMethod]
    public void ReportCountsAllIntrusionClassesOnceAndUsesHalfOpenInterval()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-report-count-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddDays(1);
            Guid agentId = Guid.NewGuid();
            Guid aliasAgentId = Guid.NewGuid();

            InsertAgent(database, agentId, "Test.Agent", "測試 Agent");
            InsertAgent(database, aliasAgentId, agentId.ToString(), "不應重複連接的 Agent");
            InsertIncident(database, start, agentId, "192.0.2.10", IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddHours(1), agentId, "192.0.2.10", IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL);
            InsertIncident(database, start.AddHours(2), agentId, "192.0.2.10", IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE);
            InsertIncident(database, start.AddHours(3), agentId, "192.0.2.10", IntrusionLog.STATUS_SOFT_LOCKED);
            InsertIncident(database, start.AddHours(4), agentId, "192.0.2.10", IntrusionLog.STATUS_HARD_LOCKED);
            InsertIncident(database, end, agentId, "192.0.2.10", IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            database.ExecuteNonQuery(
                "INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",
                new DateTimeOffset(start.AddHours(5).ToUniversalTime()).ToString("O"),
                "CrossAgentSprayDetected",
                "AlertOnly",
                "Test.Agent",
                "192.0.2.10",
                "1-to-N password spray detected");

            string eventsPerAgent = ReportGenerator.Instance.GetEventsPerAgent(start, end);
            StringAssert.Contains(eventsPerAgent, ">3</span>");
            string eventsPerIp = ReportGenerator.Instance.GetIncidentsByIP(IntrusionLog.STATUS_INTRUSION_ATTEMPT, start, end);
            StringAssert.Contains(eventsPerIp, ">3</span>");

            string report = ReportGenerator.Instance.GetReport("測試", "測試", "測試", start, end);

            Assert.AreEqual(3L, ReportGenerator.Instance.TotalIntrusionAttempts);
            Assert.AreEqual(1L, ReportGenerator.Instance.TotalSoftLocks);
            Assert.AreEqual(1L, ReportGenerator.Instance.TotalHardLocks);
            StringAssert.Contains(report, "192.0.2.10");
            StringAssert.Contains(report, "1-to-N password spray detected");
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// 驗證當指定區間內查無任何攻擊或告警資料時，報表能正確渲染結構完整之跨欄佔位列，杜絕表格塌陷。
    /// </summary>
    [TestMethod]
    public void Report_EmptyData_RendersCleanPlaceholders()
    {
        Localization.LanguageManager.Instance.Initialize("zh-TW");
        string directory = Path.Combine(Path.GetTempPath(), "idds-report-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddDays(1);

            string report = ReportGenerator.Instance.GetReport("每日安全報表", "無攻擊測試", "伺服器資訊", start, end);

            // 驗證各區塊均有友善之 Empty State 佔位列
            StringAssert.Contains(report, "期間內未偵測到跨代理程式密碼噴灑攻擊警告。");
            StringAssert.Contains(report, "期間內無事件記錄。");
            StringAssert.Contains(report, "無入侵嘗試記錄。");
            StringAssert.Contains(report, "無軟封鎖記錄。");
            StringAssert.Contains(report, "無硬封鎖記錄。");

            // 總計列仍維持 0
            Assert.AreEqual(0L, ReportGenerator.Instance.TotalIntrusionAttempts);
            Assert.AreEqual(0L, ReportGenerator.Instance.TotalSoftLocks);
            Assert.AreEqual(0L, ReportGenerator.Instance.TotalHardLocks);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void InsertAgent(Database database, Guid id, string name, string displayName) =>
        database.ExecuteNonQuery(
            "INSERT INTO SecurityAgents(AgentId,Name,AssemblyName,HardLockAttempts,HardLockTimeHours,LockForever,SoftLockAttempts,SoftLockTimeMinutes,OverwriteConfiguration,DisplayName,Enabled,Serial) VALUES(@p0,@p1,@p2,20,1,0,10,1,0,@p3,1,0)",
            id, name, "Test.Agent.dll", displayName);

    private static void InsertIncident(Database database, DateTime time, Guid agentId, string address, int action) =>
        database.ExecuteNonQuery(
            "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
            time, agentId, address, action);
}
