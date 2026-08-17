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
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version IN (1,2,3,4,5)";
            Assert.AreEqual(5L, Convert.ToInt64(command.ExecuteScalar()));
            command.CommandText = "SELECT MAX(Version) FROM SchemaMigrations";
            Assert.AreEqual(5L, Convert.ToInt64(command.ExecuteScalar()));
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
}
