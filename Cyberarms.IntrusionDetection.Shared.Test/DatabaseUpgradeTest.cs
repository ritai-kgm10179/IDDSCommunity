using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.IO;

namespace Cyberarms.IntrusionDetection.Shared.Test;

[TestClass]
public class DatabaseUpgradeTest
{
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
