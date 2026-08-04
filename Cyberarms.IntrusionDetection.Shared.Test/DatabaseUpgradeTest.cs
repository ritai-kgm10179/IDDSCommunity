using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public class DatabaseUpgradeTest
{
    /// <summary>
    /// Verifies that an incomplete legacy schema is rejected instead of being marked as migrated.
    /// </summary>
    [TestMethod]
    public void Configure_RejectsIncompleteExistingSchema()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CyberarmsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "cyberarms.idds.dbf");
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
    /// Executes the test database creation operation.
    /// </summary>

    [TestMethod]
    public void TestDatabaseCreation()
    {
        string directory = Path.Combine(Path.GetTempPath(), "CyberarmsTests", Guid.NewGuid().ToString("N"));
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
