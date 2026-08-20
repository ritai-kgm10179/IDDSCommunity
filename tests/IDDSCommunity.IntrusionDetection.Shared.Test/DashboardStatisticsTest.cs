using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class DashboardStatisticsTest
{
    [TestMethod]
    public void FailedLoginStatisticsUsesOneHalfOpenWindowForTotalAndAgents()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-count-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddDays(30);
            Guid firstAgent = Guid.NewGuid();
            Guid secondAgent = Guid.NewGuid();

            InsertIncident(database, start, firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddDays(1), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL);
            InsertIncident(database, start.AddDays(2), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE);
            InsertIncident(database, end.AddTicks(-1), secondAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddDays(3), firstAgent, IntrusionLog.STATUS_SOFT_LOCKED);
            InsertIncident(database, start.AddTicks(-1), firstAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, end, secondAgent, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,SoftLocks,HardLocks) VALUES(@p0,999,4,2)",
                firstAgent);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);
            IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatistics = Locks.ReadAgentLockStatistics();

            Assert.AreEqual(4, snapshot.Total);
            Assert.AreEqual(3, snapshot.AttemptsByAgent[firstAgent]);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[secondAgent]);
            Assert.AreEqual(snapshot.Total, snapshot.AttemptsByAgent.Values.Sum());
            Assert.AreEqual(2, lockStatistics[firstAgent].HardLocks);
            Assert.AreEqual(4, lockStatistics[firstAgent].SoftLocks);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void FailedLoginStatisticsRejectsAnEmptyOrReversedWindow()
    {
        DateTime boundary = new(2026, 8, 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Locks.ReadFailedLoginStatistics(boundary, boundary));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Locks.ReadFailedLoginStatistics(boundary, boundary.AddTicks(-1)));
    }

    [TestMethod]
    public void DashboardStatisticsIncludesLegacyAgentNames()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-legacy-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = start.AddDays(30);
            Guid agentId = Guid.NewGuid();
            const string legacyName = "Legacy.Mail.SmtpAgent";
            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents
                  (AgentId,Name,AssemblyName,OverwriteConfiguration,DisplayName,Enabled,Serial)
                  VALUES(@p0,@p1,@p2,0,@p3,1,0)",
                agentId,
                legacyName,
                "Legacy.Mail",
                "郵件伺服器 SMTP 安全性代理程式");
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                start.AddDays(1),
                legacyName,
                "192.0.2.10",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,SoftLocks,HardLocks) VALUES(@p0,1,4,2)",
                legacyName);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);
            IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatistics = Locks.ReadAgentLockStatistics();

            Assert.AreEqual(1, snapshot.Total);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[agentId]);
            Assert.AreEqual(2, lockStatistics[agentId].HardLocks);
            Assert.AreEqual(4, lockStatistics[agentId].SoftLocks);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void DashboardStatisticsResolvesLegacyEnglishNamesWithLocalizedConfig()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-legacy-localized-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = start.AddDays(30);

            Guid sqlAgentId = WellKnownAgentIds.SqlServer;
            Guid winBaseAgentId = WellKnownAgentIds.WindowsBase;
            Guid ftpAgentId = WellKnownAgentIds.Ftp;

            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents
                  (AgentId,Name,AssemblyName,OverwriteConfiguration,DisplayName,Enabled,Serial)
                  VALUES(@p0,@p1,@p2,0,@p3,1,0)",
                sqlAgentId,
                "SqlFailedLoginWatcher",
                "IDDSCommunity.Agents.SqlServer.dll",
                "SQL Server 安全性代理程式");

            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents
                  (AgentId,Name,AssemblyName,OverwriteConfiguration,DisplayName,Enabled,Serial)
                  VALUES(@p0,@p1,@p2,0,@p3,1,0)",
                winBaseAgentId,
                "WindowsSecurityBase",
                "IDDSCommunity.IntrusionDetection.Base.dll",
                "Windows 基礎安全性代理程式");

            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents
                  (AgentId,Name,AssemblyName,OverwriteConfiguration,DisplayName,Enabled,Serial)
                  VALUES(@p0,@p1,@p2,0,@p3,1,0)",
                ftpAgentId,
                "FtpAgent",
                "IDDSCommunity.Agents.FtpServer.dll",
                "FTP 安全性代理程式");

            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                start.AddDays(1),
                "SQL Server Security Agent",
                "192.0.2.10",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                start.AddDays(2),
                "Windows Base Security Agent",
                "192.0.2.11",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,SoftLocks,HardLocks) VALUES(@p0,5,3,1)",
                "FTP Security Agent");

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);
            IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatistics = Locks.ReadAgentLockStatistics();

            Assert.AreEqual(2, snapshot.Total);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[sqlAgentId]);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[winBaseAgentId]);
            Assert.AreEqual(1, lockStatistics[ftpAgentId].HardLocks);
            Assert.AreEqual(3, lockStatistics[ftpAgentId].SoftLocks);
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void DashboardCountsCrossAgentAlertsInTheSameThirtyDayWindow()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-cross-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = start.AddDays(30);
            database.ExecuteNonQuery(
                "INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",
                new DateTimeOffset(start.AddDays(1)).ToString("O"),
                "CrossAgentSprayDetected",
                "AlertOnly",
                "Agent",
                "192.0.2.20",
                "test");
            database.ExecuteNonQuery(
                "INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",
                new DateTimeOffset(end).ToString("O"),
                "CrossAgentSprayDetected",
                "AlertOnly",
                "Agent",
                "192.0.2.21",
                "outside-window");

            Assert.AreEqual(1, Locks.ReadCrossAgentAlertCount(start, end));
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void GroupedSecurityLogUsesHalfOpenWindowAndExactFailureStatuses()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-security-log-window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            Assert.AreEqual(1L, Db.DbValueConverter.ToInt64(database.ExecuteScalar(
                "select count(*) from sqlite_master where type='index' and name='IX_IntrusionLog_IncidentTime'")));
            DateTime start = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Local);
            DateTime end = start.AddHours(24);
            Guid agentId = Guid.NewGuid();
            InsertIncident(database, start, agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, end.AddTicks(-1), agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            InsertIncident(database, start.AddHours(1), agentId, 101);
            InsertIncident(database, end, agentId, IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            int total = 0;
            using System.Data.IDataReader reader = IntrusionLog.ReadIntervalGrouped(start, end);
            while (reader.Read())
                total += Db.DbValueConverter.ToInt(reader["NumberOfEvents"]);

            Assert.AreEqual(3, total);
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT));
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL));
            Assert.IsTrue(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE));
            Assert.IsFalse(IntrusionLog.IsFailedLoginAction(101));
            Assert.IsFalse(IntrusionLog.IsFailedLoginAction(IntrusionLog.STATUS_SOFT_LOCKED));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT,
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL,
                    IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE
                },
                IntrusionLog.FailedLoginActions.ToArray());
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void WellKnownAgentIds_ResolvesAllKnownAgentsSuccessfully()
    {
        Assert.IsTrue(WellKnownAgentIds.TryResolveCanonicalGuid("Windows Base Security Agent", out Guid winBase));
        Assert.AreEqual(WellKnownAgentIds.WindowsBase, winBase);

        Assert.IsTrue(WellKnownAgentIds.TryResolveCanonicalGuid("SQL Server Security Agent", out Guid sql));
        Assert.AreEqual(WellKnownAgentIds.SqlServer, sql);

        Assert.IsTrue(WellKnownAgentIds.TryResolveCanonicalGuid("IDDSCommunity.Agents.MailServer.SmtpAgent", out Guid smtp));
        Assert.AreEqual(WellKnownAgentIds.Smtp, smtp);

        Assert.IsTrue(WellKnownAgentIds.TryResolveCanonicalGuid("FTP Security Agent", out Guid ftp));
        Assert.AreEqual(WellKnownAgentIds.Ftp, ftp);

        Assert.IsTrue(WellKnownAgentIds.TryResolveCanonicalGuid("遠端桌面安全性代理程式", out Guid rdp));
        Assert.AreEqual(WellKnownAgentIds.TerminalServer, rdp);
    }

    [TestMethod]
    public void Migration12_CanonicalizesLegacyAgentIdentities_MergesStatisticsCorrectly()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-migration12-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);

            // 模擬已套用至 Migration 11 的狀態，寫入舊版未正規化 AgentId
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                DateTime.UtcNow,
                "SQL Server Security Agent",
                "192.0.2.50",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            // 既有 AgentStatistics 包含舊名稱 (5, 2, 1) 與 新 GUID (3, 1, 2)
            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,HardLocks,SoftLocks) VALUES(@p0,5,2,1)",
                "SQL Server Security Agent");

            database.ExecuteNonQuery(
                "INSERT OR REPLACE INTO AgentStatistics(AgentId,FailedLogins,HardLocks,SoftLocks) VALUES(@p0,3,1,2)",
                WellKnownAgentIds.SqlServer.ToString());

            // 模擬 Migration 12 執行
            database.ExecuteNonQuery("DELETE FROM SchemaMigrations WHERE Version = 12");
            Db.SchemaMigrationRunner.Migrate(database.Connection);

            // 驗證 IntrusionLog 已轉換為標準 GUID
            object? intrusionAgentId = database.ExecuteScalar("SELECT AgentId FROM IntrusionLog WHERE ClientIP='192.0.2.50'");
            Assert.AreEqual(WellKnownAgentIds.SqlServer.ToString(), intrusionAgentId?.ToString());

            // 驗證 AgentStatistics 已合併且無重複列
            object? count = database.ExecuteScalar("SELECT COUNT(*) FROM AgentStatistics WHERE AgentId LIKE '%SQL%'");
            Assert.AreEqual(0L, Convert.ToInt64(count));

            using System.Data.IDataReader reader = database.ExecuteReader("SELECT FailedLogins, HardLocks, SoftLocks FROM AgentStatistics WHERE AgentId=@p0", WellKnownAgentIds.SqlServer.ToString());
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(8, Convert.ToInt32(reader["FailedLogins"])); // 5 + 3
            Assert.AreEqual(3, Convert.ToInt32(reader["HardLocks"]));    // 2 + 1
            Assert.AreEqual(3, Convert.ToInt32(reader["SoftLocks"]));    // 1 + 2
        }
        finally
        {
            database.Close();
            try { Directory.Delete(directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void InsertIncident(Database database, DateTime time, Guid agentId, int action) =>
        database.ExecuteNonQuery(
            "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
            time,
            agentId,
            "192.0.2.10",
            action);
}
