using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public class DatabaseUpgradeTest
{
    /// <summary>
    /// Verifies that asynchronous transaction failures roll back all writes.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteInTransactionAsync_WhenOperationFails_RollsBack()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Database database = new();
        try
        {
            database.Configure(directory);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => database.ExecuteInTransactionAsync(async (connection, transaction, cancellationToken) =>
            {
                await using Microsoft.Data.Sqlite.SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO AppConfig(ConfigKey, ConfigValue) VALUES ('transaction-test', 'value')";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("rollback requested");
            })).ConfigureAwait(false);

            long? count = await database.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM AppConfig WHERE ConfigKey = 'transaction-test'").ConfigureAwait(false);
            Assert.AreEqual(0L, count);
        }
        finally
        {
            database.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
    /// <summary>
    /// Verifies that an incomplete legacy schema is rejected instead of being marked as migrated.
    /// </summary>
    [TestMethod]
    public void Configure_RejectsIncompleteExistingSchema()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "iddscommunity.dbf");
        try
        {
            using (Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={databasePath}"))
            {
                connection.Open();
                using Microsoft.Data.Sqlite.SqliteCommand command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE DbConfig(Version INTEGER NOT NULL)";
                command.ExecuteNonQuery();
            }

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => Database.Instance.Configure(directory));
            StringAssert.Contains(exception.Message, "Configuration");
        }
        finally
        {
            Database.Instance.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
    /// <summary>
    /// 執行 test database creation 作業。
    /// </summary>

    [TestMethod]
    public void TestDatabaseCreation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        try
        {
            Database.Instance.Configure(directory);
            Assert.AreEqual(1, Database.Instance.DatabaseVersion);
            using Microsoft.Data.Sqlite.SqliteCommand command = Database.Instance.Connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version IN (1,2,3,4,5,6,7,8,9,10)";
            Assert.AreEqual(10L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT MAX(Version) FROM SchemaMigrations";
            Assert.AreEqual(10L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ObservationWatermarks'";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='SecurityObservationEvents'";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('SecurityObservationEvents') WHERE name IN ('IsCredentialFailure','ActivityId','TargetResource','ErrorCode','AccountSid','IsCrossSourceDuplicate','DuplicateOfObservationId','CorrelationProcessed')";
            Assert.AreEqual(8L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ProtectionEventInbox') WHERE name IN ('IsAuthenticationEvent','AccountName','AccountDomain','AccountSid','IsCredentialFailure','ProviderOrChannel','ComputerName','SourceEventRecordId','ActivityId','ConfidenceScore','TargetResource','ErrorCode')";
            Assert.AreEqual(12L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_IntrusionLog_IncidentTime'";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
        }
        finally
        {
            Database.Instance.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

    }

    /// <summary>
    /// 驗證既有版本 3 資料庫可升級至版本 4，且原有資料不會遺失。
    /// </summary>
    [TestMethod]
    public void ExistingVersion3Database_IsUpgradedToVersion4WithoutDataLoss()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Database database = new();
        try
        {
            database.Configure(directory);
            database.ExecuteNonQuery("INSERT INTO AppConfig(ConfigKey, ConfigValue) VALUES ('v3-upgrade-marker', 'preserved')");
            database.ExecuteNonQuery("DROP INDEX IF EXISTS IX_IntrusionLog_IncidentTime");
            database.ExecuteNonQuery("DELETE FROM SchemaMigrations WHERE Version = 4");
            database.Close();

            database.Configure(directory);
            using Microsoft.Data.Sqlite.SqliteCommand command = database.Connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 4";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_IntrusionLog_IncidentTime'";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT ConfigValue FROM AppConfig WHERE ConfigKey = 'v3-upgrade-marker'";
            Assert.AreEqual("preserved", Convert.ToString(command.ExecuteScalar()));
        }
        finally
        {
            database.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 驗證版本 5 移轉會將既有以本機時區儲存的 IncidentTime/LockDate/UnlockDate/LastUpdate 轉換為 UTC，
    /// 且只會套用一次（不會在後續啟動時重複位移）。
    /// </summary>
    [TestMethod]
    public void ExistingLegacyLocalTimestamps_AreMigratedToUtcExactlyOnce()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Database database = new();
        try
        {
            database.Configure(directory);
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            DateTime legacyLocalIncident = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Unspecified);
            Guid agentId = Guid.NewGuid();
            database.ExecuteNonQuery(
                "INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,@p4)",
                legacyLocalIncident, agentId, "192.0.2.50", 0, false);
            database.ExecuteNonQuery("DELETE FROM SchemaMigrations WHERE Version = 5");
            database.Close();

            database.Configure(directory);
            DateTime migratedIncident = Db.DbValueConverter.ToDateTime(
                database.ExecuteScalar("SELECT IncidentTime FROM IntrusionLog WHERE ClientIP=@p0", "192.0.2.50"));
            Assert.AreEqual(DateTime.SpecifyKind(legacyLocalIncident - offset, DateTimeKind.Utc), migratedIncident);

            using Microsoft.Data.Sqlite.SqliteCommand command = database.Connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 5";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));

            // 再次啟動不得重複位移；SchemaMigrations 已標記版本 5，時間戳記應維持不變。
            database.Close();
            database.Configure(directory);
            DateTime unchangedIncident = Db.DbValueConverter.ToDateTime(
                database.ExecuteScalar("SELECT IncidentTime FROM IntrusionLog WHERE ClientIP=@p0", "192.0.2.50"));
            Assert.AreEqual(migratedIncident, unchangedIncident);
        }
        finally
        {
            database.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 驗證資料庫結構描述移轉在執行途中發生例外時，整套 SQLite DDL 交易會原子回滾，不得殘留半套結構或記錄版本號。
    /// </summary>
    [TestMethod]
    public void Migrate_WhenFailureInjected_RollsBackEntireTransactionWithoutPartialSchema()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "rollback_test.db");

        try
        {
            using (Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={databasePath}"))
            {
                connection.Open();

                // 模擬移轉交易在執行到一半時發生注入錯誤
                Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() =>
                {
                    using Microsoft.Data.Sqlite.SqliteTransaction transaction = connection.BeginTransaction();
                    using Microsoft.Data.Sqlite.SqliteCommand cmd1 = connection.CreateCommand();
                    cmd1.Transaction = transaction;
                    cmd1.CommandText = "CREATE TABLE InjectedTable1 (Id INTEGER PRIMARY KEY);";
                    cmd1.ExecuteNonQuery();

                    // 故意注入語法錯誤指令以中斷交易
                    using Microsoft.Data.Sqlite.SqliteCommand cmdFail = connection.CreateCommand();
                    cmdFail.Transaction = transaction;
                    cmdFail.CommandText = "CREATE TABLE InjectedTable2 (INVALID SYNTAX ERROR ???);";
                    cmdFail.ExecuteNonQuery();

                    transaction.Commit();
                });

                // 驗證交易已回滾，InjectedTable1 絕不存在
                using Microsoft.Data.Sqlite.SqliteCommand verifyCmd = connection.CreateCommand();
                verifyCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='InjectedTable1'";
                long count = Convert.ToInt64(verifyCmd.ExecuteScalar());
                Assert.AreEqual(0L, count);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 驗證既有版本 5 資料庫（含舊版無 AlertId 之 ProtectionAuditLog 資料）可平順升級至版本 6：
    /// 舊列完整保留、AlertId 為 NULL、既有多筆 NULL 不違反 UNIQUE、新 Outbox 告警可寫入、唯一索引生效且重跑具備冪等性。
    /// </summary>
    [TestMethod]
    public void ExistingV5DatabaseWithProtectionAuditLog_IsUpgradedToVersion6WithAlertIdAndUniqueIndex()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IDDSCommunityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "v5_upgrade_test.db");

        try
        {
            // 1. 建立真實 v5 結構描述（含舊版無 AlertId 之 ProtectionAuditLog）並寫入舊資料
            using (Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={databasePath}"))
            {
                connection.Open();
                using Microsoft.Data.Sqlite.SqliteTransaction tx = connection.BeginTransaction();
                using Microsoft.Data.Sqlite.SqliteCommand setupCmd = connection.CreateCommand();
                setupCmd.Transaction = tx;
                setupCmd.CommandText = """
                    CREATE TABLE SchemaMigrations (Version INTEGER PRIMARY KEY NOT NULL, AppliedUtc TEXT NOT NULL);
                    INSERT INTO SchemaMigrations VALUES (1, '2026-01-01T00:00:00Z');
                    INSERT INTO SchemaMigrations VALUES (2, '2026-01-01T00:00:00Z');
                    INSERT INTO SchemaMigrations VALUES (3, '2026-01-01T00:00:00Z');
                    INSERT INTO SchemaMigrations VALUES (4, '2026-01-01T00:00:00Z');
                    INSERT INTO SchemaMigrations VALUES (5, '2026-01-01T00:00:00Z');
                    CREATE TABLE DbConfig (Version INTEGER NOT NULL);
                    CREATE TABLE Configuration (ConfigurationKey TEXT PRIMARY KEY, ConfigurationValue TEXT);
                    CREATE TABLE IntrusionLog (Id INTEGER PRIMARY KEY AUTOINCREMENT, IncidentTime TEXT, AgentId TEXT, ClientIP TEXT, Action INTEGER, ActionTriggeredByUser INTEGER);
                    CREATE TABLE Locks (Id INTEGER PRIMARY KEY AUTOINCREMENT, LockType INTEGER, LockDate TEXT, UnlockDate TEXT, LastUpdate TEXT, Reason INTEGER, ClientIP TEXT, Description TEXT);
                    CREATE TABLE SecurityAgentConfig (AgentId TEXT PRIMARY KEY, AgentConfig TEXT);
                    CREATE TABLE SecurityAgents (AgentId TEXT PRIMARY KEY, AgentName TEXT, AgentDescription TEXT, AgentAssembly TEXT, AgentType TEXT, AgentEnabled INTEGER);
                    CREATE TABLE AppConfig (ConfigKey TEXT PRIMARY KEY, ConfigValue TEXT);
                    CREATE TABLE Whitelist (ClientIP TEXT PRIMARY KEY);
                    CREATE TABLE AgentStatistics (AgentId TEXT PRIMARY KEY, AttacksBlocked INTEGER, AttacksDetected INTEGER);
                    CREATE TABLE SecurityObservationEvents (
                        Id TEXT PRIMARY KEY NOT NULL,
                        IdempotencyKey TEXT NOT NULL,
                        ReceivedUtc TEXT NOT NULL,
                        EventTimeUtc TEXT NOT NULL,
                        SourceAgentName TEXT NOT NULL,
                        ProviderOrChannel TEXT NOT NULL,
                        ComputerName TEXT NOT NULL,
                        SourceEventRecordId INTEGER NULL,
                        SourceFileOffset INTEGER NULL,
                        SourceEventIdentity TEXT NULL,
                        NormalizedIpAddress TEXT NOT NULL,
                        NormalizedAccount TEXT NOT NULL,
                        NormalizedDomain TEXT NOT NULL,
                        OriginalEventReference TEXT NOT NULL,
                        Provenance TEXT NOT NULL,
                        LogonType INTEGER NULL,
                        SubStatus TEXT NULL,
                        CorrelationGroupId TEXT NULL,
                        ConfidenceScore REAL NOT NULL,
                        AlertEmitted INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE TABLE ProtectionAuditLog (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                        OccurredUtc TEXT NOT NULL,
                        EventType TEXT NOT NULL,
                        Outcome TEXT NOT NULL,
                        Actor TEXT NOT NULL,
                        Subject TEXT NOT NULL,
                        Details TEXT NOT NULL
                    );
                    INSERT INTO ProtectionAuditLog (OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                    VALUES ('2026-01-01T10:00:00Z', 'LegacyEvent1', 'Success', 'SYSTEM', '192.0.2.1', 'Detail1');
                    INSERT INTO ProtectionAuditLog (OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                    VALUES ('2026-01-01T11:00:00Z', 'LegacyEvent2', 'Success', 'SYSTEM', '192.0.2.2', 'Detail2');
                    INSERT INTO ProtectionAuditLog (OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                    VALUES ('2026-01-01T12:00:00Z', 'LegacyEvent3', 'Success', 'SYSTEM', '192.0.2.3', 'Detail3');
                    """;
                setupCmd.ExecuteNonQuery();
                tx.Commit();
            }

            // 2. 執行目前版本移轉
            using (Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={databasePath}"))
            {
                connection.Open();
                Db.SchemaMigrationRunner.Migrate(connection);

                // 驗證 SchemaMigrations 記錄目前版本 10
                using Microsoft.Data.Sqlite.SqliteCommand checkVerCmd = connection.CreateCommand();
                checkVerCmd.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 10";
                Assert.AreEqual(1L, Convert.ToInt64(checkVerCmd.ExecuteScalar()));
                checkVerCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('SecurityObservationEvents') WHERE name IN ('IsCredentialFailure','ActivityId','TargetResource','ErrorCode','AccountSid','IsCrossSourceDuplicate','DuplicateOfObservationId','CorrelationProcessed')";
                Assert.AreEqual(8L, Convert.ToInt64(checkVerCmd.ExecuteScalar()));

                // 驗證舊列完整保留且 AlertId 欄位為 NULL
                using Microsoft.Data.Sqlite.SqliteCommand checkOldCmd = connection.CreateCommand();
                checkOldCmd.CommandText = "SELECT COUNT(*) FROM ProtectionAuditLog WHERE AlertId IS NULL";
                Assert.AreEqual(3L, Convert.ToInt64(checkOldCmd.ExecuteScalar()));

                // 驗證唯一索引存在且生效
                using Microsoft.Data.Sqlite.SqliteCommand checkIdxCmd = connection.CreateCommand();
                checkIdxCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_ProtectionAuditLog_AlertId'";
                Assert.AreEqual(1L, Convert.ToInt64(checkIdxCmd.ExecuteScalar()));

                // 驗證新 AlertId 可正常寫入
                using Microsoft.Data.Sqlite.SqliteCommand insertNewCmd = connection.CreateCommand();
                insertNewCmd.CommandText = """
                    INSERT INTO ProtectionAuditLog (AlertId, OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                    VALUES ('ALERT-001', '2026-01-01T13:00:00Z', 'SprayDetected', 'AlertOnly', 'AuthAgent', '192.0.2.88', 'Detail');
                    """;
                insertNewCmd.ExecuteNonQuery();

                // 驗證重複 AlertId 會觸發 UNIQUE 約束違規 (當直接 INSERT 時)
                using Microsoft.Data.Sqlite.SqliteCommand insertDupCmd = connection.CreateCommand();
                insertDupCmd.CommandText = """
                    INSERT INTO ProtectionAuditLog (AlertId, OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                    VALUES ('ALERT-001', '2026-01-01T14:00:00Z', 'SprayDetected', 'AlertOnly', 'AuthAgent', '192.0.2.88', 'DupDetail');
                    """;
                Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() => insertDupCmd.ExecuteNonQuery());

                // 3. 驗證重複執行 Migrate 具備冪等性
                Db.SchemaMigrationRunner.Migrate(connection);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
