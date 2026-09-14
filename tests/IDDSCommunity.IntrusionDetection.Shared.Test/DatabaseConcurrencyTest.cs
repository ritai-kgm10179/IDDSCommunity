using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
[DoNotParallelize]
public sealed class DatabaseConcurrencyTest
{
    private string testDirectory = null!;
    private Database database = null!;
    /// <summary>
    /// Creates an isolated WAL database for each concurrency test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "IDDSCommunity.DatabaseConcurrencyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "concurrency.db");
    }
    /// <summary>
    /// Closes and removes the isolated database.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);
    }
    /// <summary>
    /// Verifies concurrent readers and writers use independent connections without reader lifetime races.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task SyncOperations_ConcurrentReadersAndWriters_RemainConsistentAsync()
    {
        Task[] writers = Enumerable.Range(0, 40)
            .Select(index => Task.Run(() => database.ExecuteNonQuery(
                "INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",
                DateTimeOffset.UtcNow.ToString("O"), "Concurrency", "Succeeded", "test", index.ToString(), string.Empty)))
            .ToArray();
        Task[] readers = Enumerable.Range(0, 20)
            .Select(readerIndex => Task.Run(() =>
            {
                using IDataReader reader = database.ExecuteReader("SELECT Id,Subject FROM ProtectionAuditLog ORDER BY Id");
                while (reader.Read())
                    _ = reader["Subject"];
                _ = readerIndex;
            }))
            .ToArray();

        await Task.WhenAll(writers.Concat(readers));

        Assert.AreEqual(40L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionAuditLog")));
    }
    /// <summary>
    /// Verifies a failed independent transaction rolls back all of its writes.
    /// </summary>
    [TestMethod]
    public void ExecuteInTransaction_Failure_RollsBackAllWrites()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => database.ExecuteInTransaction((_, transaction) =>
        {
            database.ExecuteNonQuery(
                "INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",
                transaction,
                DateTimeOffset.UtcNow.ToString("O"), "Transaction", "Succeeded", "test", "one", string.Empty);
            throw new InvalidOperationException("expected");
        }));

        Assert.AreEqual(0L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionAuditLog")));
    }

    /// <summary>
    /// 驗證 Agent 交易失敗時不會提前改變記憶體中的識別碼或序號。
    /// </summary>
    [TestMethod]
    public void SecurityAgent_SaveFailure_DoesNotAdvanceInMemoryIdentityOrSerial()
    {
        database.ExecuteNonQuery("DROP TABLE SecurityAgentConfig");
        SecurityAgent agent = new()
        {
            DatabaseInstance = database,
            Name = "retry-safe-agent",
            DisplayName = "retry-safe-agent",
            Serial = 7,
            CustomConfiguration = new System.Collections.Generic.Dictionary<string, string> { ["Port"] = "25" }
        };

        Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() => agent.Save());

        Assert.AreEqual(Guid.Empty, agent.Id);
        Assert.AreEqual(7, agent.Serial);
        Assert.AreEqual(0L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM SecurityAgents")));
    }
}
