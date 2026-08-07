using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
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
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = 1";
            Assert.AreEqual(1L, Convert.ToInt64(command.ExecuteScalar()));
        }
        finally
        {
            Database.Instance.Close();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

    }
}
