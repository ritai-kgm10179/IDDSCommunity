using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class SqliteMaintenanceServiceTest
{
    private string testDirectory = null!;
    private Database database = null!;

    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(TestContext.TestRunDirectory ?? AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "maintenance.db");
    }

    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
    }

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void StatusBackupAndOptimizeProduceVerifiedResults()
    {
        SqliteMaintenanceService maintenance = new(database);

        DatabaseMaintenanceStatus status = maintenance.GetStatus();
        DatabaseBackupResult backup = maintenance.CreateVerifiedBackup(Path.Combine(testDirectory, "backups"));
        maintenance.Optimize();

        Assert.AreEqual("ok", status.IntegrityResult, true);
        Assert.AreEqual("wal", status.JournalMode, true);
        Assert.IsGreaterThan(0, status.DatabaseBytes);
        Assert.IsTrue(File.Exists(backup.FilePath));
        Assert.IsGreaterThan(0, backup.Length);
        Assert.AreEqual(64, backup.Sha256.Length);
    }

    [TestMethod]
    public void RestoreVerifiedBackupRestoresContentsAndCreatesRollbackCopy()
    {
        SqliteMaintenanceService maintenance = new(database);
        database.ExecuteNonQuery("CREATE TABLE MaintenanceMarker(Value TEXT NOT NULL)");
        database.ExecuteNonQuery("INSERT INTO MaintenanceMarker(Value) VALUES (@p0)", "before");
        DatabaseBackupResult backup = maintenance.CreateVerifiedBackup(Path.Combine(testDirectory, "backups"));
        database.ExecuteNonQuery("UPDATE MaintenanceMarker SET Value=@p0", "after");

        DatabaseBackupResult rollback = maintenance.RestoreVerifiedBackup(backup.FilePath, Path.Combine(testDirectory, "rollback"));

        Assert.AreEqual("before", Convert.ToString(database.ExecuteScalar("SELECT Value FROM MaintenanceMarker")));
        Assert.IsTrue(File.Exists(rollback.FilePath));
        Assert.AreEqual("ok", maintenance.RunIntegrityCheck(true), true);
    }

    [TestMethod]
    public void CorruptBackupIsRejectedWithoutClosingActiveDatabase()
    {
        string corrupt = Path.Combine(testDirectory, "corrupt.db");
        File.WriteAllText(corrupt, "not a sqlite database");
        SqliteMaintenanceService maintenance = new(database);

        Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() =>
            maintenance.RestoreVerifiedBackup(corrupt, Path.Combine(testDirectory, "rollback")));
        Assert.IsTrue(database.IsConfigured);
        Assert.AreEqual("ok", maintenance.RunIntegrityCheck(), true);
    }

    [TestMethod]
    public void PurgeExpiredUsesBatchesAndPreservesFailedInboxItems()
    {
        DateTimeOffset now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        DateTime old = now.AddDays(-400).UtcDateTime;
        database.ExecuteNonQuery("INSERT INTO IntrusionLog(IncidentTime,AgentId,ClientIP,Action,ActionTriggeredByUser) VALUES(@p0,@p1,@p2,@p3,@p4)", old, Guid.NewGuid(), "192.0.2.1", 0, false);
        database.ExecuteNonQuery("INSERT INTO ProtectionAuditLog(OccurredUtc,EventType,Outcome,Actor,Subject,Details) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", now.AddDays(-400).ToString("O"), "test", "Succeeded", "test", "test", "test");
        database.ExecuteNonQuery("INSERT INTO ProtectionEventInbox(Id,ReceivedUtc,AgentName,CreateDate,EventId,IpAddress,EventMessage,Status,Attempts,LastError,UpdatedUtc) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)", Guid.NewGuid().ToString(), now.AddDays(-60).ToString("O"), "agent", old.ToString("O"), 1, "192.0.2.2", "completed", 2, 1, "", now.AddDays(-60).ToString("O"));
        database.ExecuteNonQuery("INSERT INTO ProtectionEventInbox(Id,ReceivedUtc,AgentName,CreateDate,EventId,IpAddress,EventMessage,Status,Attempts,LastError,UpdatedUtc) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)", Guid.NewGuid().ToString(), now.AddDays(-60).ToString("O"), "agent", old.ToString("O"), 2, "192.0.2.3", "failed", 3, 1, "failure", now.AddDays(-60).ToString("O"));
        SqliteMaintenanceService maintenance = new(database);

        var results = maintenance.PurgeExpired(new DatabaseRetentionPolicy(BatchSize: 100), now);

        Assert.AreEqual(1, results["IntrusionLog"]);
        Assert.AreEqual(1, results["ProtectionAuditLog"]);
        Assert.AreEqual(1, results["ProtectionEventInbox"]);
        Assert.AreEqual(1L, Convert.ToInt64(database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionEventInbox WHERE Status=3")));
    }
}
