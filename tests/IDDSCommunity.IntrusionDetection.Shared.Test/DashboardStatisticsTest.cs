using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class DashboardStatisticsTest
{
    [TestMethod]
    public void ResolveDefaultDataDirectory_MovesCompleteLegacyDirectoryWhenTargetIsEmpty()
    {
        string root = Path.Combine(Path.GetTempPath(), "idds-data-directory-" + Guid.NewGuid().ToString("N"));
        string legacy = Path.Combine(root, "IDDSCommunity");
        string target = Path.Combine(root, "IDDS Community");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(legacy, "iddscommunity.dbf"), "database");
        File.WriteAllText(Path.Combine(legacy, "iddscommunity.dbf.key"), "key");
        try
        {
            Assert.AreEqual(target, IddsConfig.ResolveDefaultDataDirectory(root));
            Assert.IsFalse(Directory.Exists(legacy));
            Assert.IsTrue(File.Exists(Path.Combine(target, "iddscommunity.dbf")));
            Assert.IsTrue(File.Exists(Path.Combine(target, "iddscommunity.dbf.key")));
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void ResolveDefaultDataDirectory_RejectsTwoPopulatedDirectories()
    {
        string root = Path.Combine(Path.GetTempPath(), "idds-data-conflict-" + Guid.NewGuid().ToString("N"));
        string legacy = Path.Combine(root, "IDDSCommunity");
        string target = Path.Combine(root, "IDDS Community");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(legacy, "iddscommunity.dbf"), "legacy");
        File.WriteAllText(Path.Combine(target, "iddscommunity.dbf"), "current");
        try
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => IddsConfig.ResolveDefaultDataDirectory(root));
            Assert.IsTrue(File.Exists(Path.Combine(legacy, "iddscommunity.dbf")));
            Assert.IsTrue(File.Exists(Path.Combine(target, "iddscommunity.dbf")));
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [TestMethod]
    public void DatabaseDiagnosticExporter_ExportsCountsWithoutSensitiveEventFields()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-diagnostic-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string outputPath = Path.Combine(directory, "diagnostics.json");
        Database database = new();
        try
        {
            database.Configure(directory);
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                DateTime.UtcNow,
                WellKnownAgentIds.TerminalServer,
                "203.0.113.199",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            database.Close();

            DatabaseDiagnosticExporter.Export(Path.Combine(directory, "iddscommunity.dbf"), outputPath);

            string json = File.ReadAllText(outputPath);
            Assert.DoesNotContain("203.0.113.199", json);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual(1L, root.GetProperty("tableCounts").GetProperty("IntrusionLog").GetInt64());
            JsonElement group = root.GetProperty("intrusionSummary").GetProperty("byAgentAndAction")[0];
            Assert.AreEqual(
                WellKnownAgentIds.TerminalServer.ToString(),
                group.GetProperty("agentId").GetString(),
                ignoreCase: true,
                culture: System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual((long)IntrusionLog.STATUS_INTRUSION_ATTEMPT, group.GetProperty("action").GetInt64());
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
            Assert.AreEqual(1, snapshot.AttemptsByAgent[WellKnownAgentIds.Smtp]);
            Assert.AreEqual(2, lockStatistics[WellKnownAgentIds.Smtp].HardLocks);
            Assert.AreEqual(4, lockStatistics[WellKnownAgentIds.Smtp].SoftLocks);
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

        Assert.IsFalse(WellKnownAgentIds.IsWellKnown(Guid.NewGuid()));
        Assert.IsTrue(WellKnownAgentIds.IsWellKnown(WellKnownAgentIds.TerminalServer));
    }

    [TestMethod]
    public void DashboardStatisticsMapsLegacyRandomGuidThroughConfiguredAgentIdentity()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-dashboard-random-guid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            DateTime start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = start.AddDays(30);
            Guid legacyRandomGuid = Guid.NewGuid();
            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents
                  (AgentId,Name,AssemblyName,OverwriteConfiguration,DisplayName,Enabled,Serial)
                  VALUES(@p0,@p1,@p2,0,@p3,1,0)",
                legacyRandomGuid,
                "TlsSslAgent",
                "IDDSCommunity.Agents.TerminalServer.dll",
                "遠端桌面安全性代理程式");
            InsertIncident(database, start.AddDays(1), legacyRandomGuid, IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(start, end);

            Assert.AreEqual(1, snapshot.Total);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[WellKnownAgentIds.TerminalServer]);
            Assert.IsFalse(snapshot.AttemptsByAgent.ContainsKey(legacyRandomGuid));
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

    [TestMethod]
    public void Migration12_MigratesLegacyRandomGuidsToInvariantGuids()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-migration12-guid-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);

            Guid legacyRandomGuid = Guid.NewGuid();

            // 模擬舊版 SecurityAgents 存有舊隨機 GUID
            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents (AgentId, Name, DisplayName, AssemblyName, Enabled, Serial, OverwriteConfiguration)
                  VALUES (@p0, 'TlsSslAgent', '遠端桌面安全性代理程式', 'IDDSCommunity.Agents.TerminalServer.dll', 1, 0, 0)",
                legacyRandomGuid.ToString());

            // 模擬 IntrusionLog 與 AgentStatistics 使用此舊隨機 GUID
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                DateTime.UtcNow,
                legacyRandomGuid.ToString(),
                "192.0.2.60",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,HardLocks,SoftLocks) VALUES(@p0,10,2,3)",
                legacyRandomGuid.ToString());

            // 執行 Migration 12
            database.ExecuteNonQuery("DELETE FROM SchemaMigrations WHERE Version = 12");
            Db.SchemaMigrationRunner.Migrate(database.Connection);

            // 驗證 SecurityAgents 已正規化為 WellKnownAgentIds.TerminalServer
            object? agentIdInDb = database.ExecuteScalar("SELECT AgentId FROM SecurityAgents WHERE Name='TlsSslAgent'");
            Assert.AreEqual(WellKnownAgentIds.TerminalServer.ToString(), agentIdInDb?.ToString());

            // 驗證 IntrusionLog 已正規化為 WellKnownAgentIds.TerminalServer
            object? logAgentId = database.ExecuteScalar("SELECT AgentId FROM IntrusionLog WHERE ClientIP='192.0.2.60'");
            Assert.AreEqual(WellKnownAgentIds.TerminalServer.ToString(), logAgentId?.ToString());

            // 驗證 AgentStatistics 已正規化
            using var reader = database.ExecuteReader("SELECT FailedLogins, HardLocks, SoftLocks FROM AgentStatistics WHERE AgentId=@p0", WellKnownAgentIds.TerminalServer.ToString());
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(10, Convert.ToInt32(reader["FailedLogins"]));
            Assert.AreEqual(2, Convert.ToInt32(reader["HardLocks"]));
            Assert.AreEqual(3, Convert.ToInt32(reader["SoftLocks"]));
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
    public void MergeDbInformation_ForcesInvariantGuidAndSavesToDb()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-mergedb-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);
            IddsConfig config = new(database);

            Guid legacyRandomGuid = Guid.NewGuid();
            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents (AgentId, Name, DisplayName, AssemblyName, Enabled, Serial, OverwriteConfiguration)
                  VALUES (@p0, 'TlsSslAgent', '遠端桌面安全性代理程式', 'IDDSCommunity.Agents.TerminalServer.dll', 1, 0, 0)",
                legacyRandomGuid.ToString());

            SecurityAgents securityAgents = new(database, config);
            securityAgents.InitializeAgents();

            Assert.AreEqual(1, securityAgents.Count);
            Assert.AreEqual(legacyRandomGuid, securityAgents[0].Id);

            // 模擬磁碟載入之元件（具有確定性 Invariant GUID）
            List<SecurityAgent> diskAgents =
            [
                new SecurityAgent("TlsSslAgent", WellKnownAgentIds.TerminalServer)
                {
                    DisplayName = "遠端桌面安全性代理程式",
                    AssemblyName = "IDDSCommunity.Agents.TerminalServer.dll",
                    AssemblyFilename = "IDDSCommunity.Agents.TerminalServer.dll"
                }
            ];

            securityAgents.MergeDbInformation(diskAgents);

            // 驗證記憶體中之 AgentId 已同步為 Invariant GUID
            Assert.AreEqual(WellKnownAgentIds.TerminalServer, securityAgents[0].Id);

            // 驗證資料庫中之 AgentId 亦已持久化為 Invariant GUID
            object? dbAgentId = database.ExecuteScalar("SELECT AgentId FROM SecurityAgents WHERE Name='TlsSslAgent'");
            Assert.AreEqual(WellKnownAgentIds.TerminalServer.ToString(), dbAgentId?.ToString());
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
    public void Migration13_UpgradesFromVersion12_NormalizesDatesAndRandomGuids()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds-migration13-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = new();
        try
        {
            database.Configure(directory);

            Guid legacyRandomGuid = Guid.NewGuid();

            // 模擬已套用至 Version 12 但遺留隨機 GUID 與斜線日期的狀態
            database.ExecuteNonQuery(
                @"INSERT INTO SecurityAgents (AgentId, Name, DisplayName, AssemblyName, Enabled, Serial, OverwriteConfiguration)
                  VALUES (@p0, 'TlsSslAgent', '遠端桌面安全性代理程式', 'IDDSCommunity.Agents.TerminalServer.dll', 1, 0, 0)",
                legacyRandomGuid.ToString());

            // 寫入斜線格式日期之事件
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,0)",
                "2026/08/19 14:44:00",
                legacyRandomGuid.ToString(),
                "192.0.2.70",
                IntrusionLog.STATUS_INTRUSION_ATTEMPT);

            database.ExecuteNonQuery(
                "INSERT INTO AgentStatistics(AgentId,FailedLogins,HardLocks,SoftLocks) VALUES(@p0,7,1,2)",
                legacyRandomGuid.ToString());

            // 確保 SchemaMigrations 包含 Version 12
            database.ExecuteNonQuery("DELETE FROM SchemaMigrations WHERE Version = 13");
            Db.SchemaMigrationRunner.Migrate(database.Connection);

            // 驗證 IntrusionLog 的日期已被正規化為破折號 ISO 格式
            object? rawIncidentTime = database.ExecuteScalar("SELECT IncidentTime FROM IntrusionLog WHERE ClientIP='192.0.2.70'");
            Assert.IsNotNull(rawIncidentTime);
            Assert.IsTrue(rawIncidentTime.ToString()!.StartsWith("2026-08-19"), $"預期為 ISO 破折號格式，實際為: {rawIncidentTime}");

            // 驗證 Locks.ReadFailedLoginStatistics 在 30 天查詢視窗內能成功查詢到此筆事件
            DateTime now = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(now.AddDays(-30), now.AddDays(1));
            Assert.AreEqual(1, snapshot.Total);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[WellKnownAgentIds.TerminalServer]);

            // 驗證 AgentStatistics 已正確歸併至 TerminalServer
            IReadOnlyDictionary<Guid, AgentLockStatistics> lockStats = Locks.ReadAgentLockStatistics();
            Assert.AreEqual(1, lockStats[WellKnownAgentIds.TerminalServer].HardLocks);
            Assert.AreEqual(2, lockStats[WellKnownAgentIds.TerminalServer].SoftLocks);
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
    public void Configure_WhenDatabaseIsReadOnly_FallsBackToReadOnlyAndAllowsQuerying()
    {
        string directory = Path.Combine(Path.GetTempPath(), "idds_ro_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Database database = Database.Instance;

        try
        {
            database.Configure(directory);
            DateTime now = DateTime.UtcNow;
            InsertIncident(database, now, WellKnownAgentIds.TerminalServer, IntrusionLog.STATUS_INTRUSION_ATTEMPT);
            database.Close();

            // 模擬唯讀環境：將檔案設定為唯讀屬性
            string dbPath = Path.Combine(directory, "iddscommunity.dbf");
            File.SetAttributes(dbPath, FileAttributes.ReadOnly);

            // 測試：Configure 應自動捕捉 SQLITE_READONLY 並回退至 ReadOnly 模式
            database.Configure(directory);

            FailedLoginStatisticsSnapshot snapshot = Locks.ReadFailedLoginStatistics(now.AddDays(-30), now.AddDays(1));
            Assert.AreEqual(1, snapshot.Total);
            Assert.AreEqual(1, snapshot.AttemptsByAgent[WellKnownAgentIds.TerminalServer]);
        }
        finally
        {
            database.Close();
            string dbPath = Path.Combine(directory, "iddscommunity.dbf");
            if (File.Exists(dbPath))
            {
                File.SetAttributes(dbPath, FileAttributes.Normal);
            }
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
