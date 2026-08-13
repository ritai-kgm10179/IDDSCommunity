using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared;

public sealed record DatabaseMaintenanceStatus(
    string DataSource,
    long DatabaseBytes,
    long WalBytes,
    long SharedMemoryBytes,
    long PageCount,
    long FreePageCount,
    long SchemaVersion,
    string JournalMode,
    string IntegrityResult);

public sealed record DatabaseBackupResult(string FilePath, long Length, string Sha256, DateTimeOffset CreatedUtc);

public sealed record DatabaseBackupInfo(string FilePath, long Length, DateTimeOffset CreatedUtc);

public sealed record DatabaseMaintenanceHistory(DateTimeOffset OccurredUtc, string EventType, string Outcome, string Subject, string Details);

public sealed record DatabaseRetentionPolicy(int IntrusionLogDays = 180, int UnlockedLockDays = 180, int AuditDays = 365, int CompletedInboxDays = 30, int BatchSize = 1000);
/// <summary>
/// 提供界限內且可稽核的 SQLite 維護作業。
/// </summary>
public sealed class SqliteMaintenanceService(Database database)
{
    private readonly Database database = database ?? throw new ArgumentNullException(nameof(database));

    public DatabaseMaintenanceStatus GetStatus(bool fullIntegrityCheck = false)
    {
        EnsureConfigured();
        string dataSource = Path.GetFullPath(database.DataSource);
        return new DatabaseMaintenanceStatus(
            dataSource,
            GetLength(dataSource),
            GetLength(dataSource + "-wal"),
            GetLength(dataSource + "-shm"),
            ReadInt64("PRAGMA page_count"),
            ReadInt64("PRAGMA freelist_count"),
            ReadInt64("PRAGMA user_version"),
            Convert.ToString(database.ExecuteScalar("PRAGMA journal_mode"), CultureInfo.InvariantCulture) ?? string.Empty,
            RunIntegrityCheck(fullIntegrityCheck));
    }

    public string RunIntegrityCheck(bool full = false)
    {
        EnsureConfigured();
        string command = full ? "PRAGMA integrity_check" : "PRAGMA quick_check";
        return Convert.ToString(database.ExecuteScalar(command), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public DatabaseBackupResult CreateVerifiedBackup(string backupDirectory)
    {
        EnsureConfigured();
        ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
        Directory.CreateDirectory(backupDirectory);
        string fileName = $"iddscommunity-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db";
        string destinationPath = Path.GetFullPath(Path.Combine(backupDirectory, fileName));

        using SqliteConnection destination = database.CreateEncryptedConnection(destinationPath, SqliteOpenMode.ReadWriteCreate);
        destination.Open();
        database.Connection.BackupDatabase(destination);
        using SqliteCommand check = destination.CreateCommand();
        check.CommandText = "PRAGMA integrity_check";
        string result = Convert.ToString(check.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            destination.Close();
            File.Delete(destinationPath);
            throw new InvalidDataException($"{MaintenanceError.BackupIntegrityCheckFailed}:{result}");
        }

        destination.Close();
        FileInfo backup = new(destinationPath);
        DatabaseBackupResult backupResult = new(destinationPath, backup.Length, ComputeSha256(destinationPath), DateTimeOffset.UtcNow);
        RecordAudit("Database.Backup", destinationPath, backupResult.Sha256);
        return backupResult;
    }

    public DatabaseBackupResult VerifyBackup(string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        if (!File.Exists(backupPath)) throw new FileNotFoundException(MaintenanceError.BackupFileNotFound.ToString(), backupPath);
        ValidateDatabaseFile(backupPath);
        FileInfo file = new(backupPath);
        return new DatabaseBackupResult(file.FullName, file.Length, ComputeSha256(file.FullName), file.CreationTimeUtc);
    }

    public IReadOnlyList<DatabaseBackupInfo> ListBackups(string backupDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
        if (!Directory.Exists(backupDirectory)) return [];
        return Directory.EnumerateFiles(backupDirectory, "iddscommunity-*.db", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new DatabaseBackupInfo(file.FullName, file.Length, file.CreationTimeUtc))
            .ToArray();
    }

    public int PruneBackups(string backupDirectory, int retentionDays = 30, int maximumCount = 10, DateTimeOffset? now = null)
    {
        if (retentionDays < 1) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        if (maximumCount < 1) throw new ArgumentOutOfRangeException(nameof(maximumCount));
        DateTimeOffset boundary = (now ?? DateTimeOffset.UtcNow).AddDays(-retentionDays);
        DatabaseBackupInfo[] backups = [.. ListBackups(backupDirectory)];
        int deleted = 0;
        for (int index = 0; index < backups.Length; index++)
        {
            if (index < maximumCount && backups[index].CreatedUtc >= boundary) continue;
            File.Delete(backups[index].FilePath);
            deleted++;
        }
        RecordAudit("Database.BackupRetention", backupDirectory, deleted.ToString(CultureInfo.InvariantCulture));
        return deleted;
    }

    public IReadOnlyList<DatabaseMaintenanceHistory> GetHistory(int maximumRows = 50)
    {
        EnsureConfigured();
        if (maximumRows is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(maximumRows));
        return database.Connection.Query<DatabaseMaintenanceHistoryRow>(
            "SELECT OccurredUtc,EventType,Outcome,Subject,Details FROM ProtectionAuditLog WHERE EventType LIKE 'Database.%' ORDER BY OccurredUtc DESC LIMIT @MaximumRows",
            new { MaximumRows = maximumRows })
            .Select(row => new DatabaseMaintenanceHistory(
                DateTimeOffset.Parse(row.OccurredUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                row.EventType, row.Outcome, row.Subject, row.Details))
            .ToArray();
    }

    public void Optimize()
    {
        EnsureConfigured();
        database.ExecuteNonQuery("PRAGMA optimize");
        database.ExecuteNonQuery("PRAGMA wal_checkpoint(PASSIVE)");
        RecordAudit("Database.Optimize", database.DataSource, string.Empty);
    }

    public IReadOnlyDictionary<string, int> PurgeExpired(DatabaseRetentionPolicy policy, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ValidatePolicy(policy);
        DateTimeOffset current = now ?? DateTimeOffset.UtcNow;
        Dictionary<string, int> results = new(StringComparer.Ordinal);
        database.ExecuteInTransaction((connection, transaction) =>
        {
            results["Locks"] = DeleteBatch(connection, transaction,
                "DELETE FROM Locks WHERE LockId IN (SELECT LockId FROM Locks WHERE Status=@Unlocked AND UnlockDate < @Boundary ORDER BY UnlockDate LIMIT @BatchSize)",
                current.AddDays(-policy.UnlockedLockDays), policy.BatchSize, Lock.LOCK_STATUS_UNLOCKED);
            results["IntrusionLog"] = DeleteBatch(connection, transaction,
                "DELETE FROM IntrusionLog WHERE Id IN (SELECT i.Id FROM IntrusionLog i WHERE i.IncidentTime < @Boundary AND NOT EXISTS (SELECT 1 FROM Locks l WHERE l.TriggerIncident=i.Id) ORDER BY i.IncidentTime LIMIT @BatchSize)",
                current.AddDays(-policy.IntrusionLogDays), policy.BatchSize);
            results["ProtectionAuditLog"] = DeleteBatch(connection, transaction,
                "DELETE FROM ProtectionAuditLog WHERE Id IN (SELECT Id FROM ProtectionAuditLog WHERE OccurredUtc < @Boundary ORDER BY OccurredUtc LIMIT @BatchSize)",
                current.AddDays(-policy.AuditDays), policy.BatchSize);
            results["ProtectionEventInbox"] = DeleteBatch(connection, transaction,
                "DELETE FROM ProtectionEventInbox WHERE Id IN (SELECT Id FROM ProtectionEventInbox WHERE Status=2 AND UpdatedUtc < @Boundary ORDER BY UpdatedUtc LIMIT @BatchSize)",
                current.AddDays(-policy.CompletedInboxDays), policy.BatchSize);
        });
        RecordAudit("Database.RetentionCleanup", database.DataSource, string.Join(",", results));
        return results;
    }

    public DatabaseBackupResult RestoreVerifiedBackup(string backupPath, string rollbackDirectory)
    {
        EnsureConfigured();
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        if (!File.Exists(backupPath)) throw new FileNotFoundException(MaintenanceError.BackupFileNotFound.ToString(), backupPath);
        ValidateDatabaseFile(backupPath);

        string dataSource = Path.GetFullPath(database.DataSource);
        string directory = Path.GetDirectoryName(dataSource) ?? throw new InvalidOperationException(MaintenanceError.DatabaseDirectoryUnavailable.ToString());
        string fileName = Path.GetFileName(dataSource);
        EnsureFreeSpace(directory, checked(Math.Max(GetLength(backupPath) + GetLength(dataSource), 64L * 1024 * 1024)));
        string candidate = Path.Combine(directory, $".{fileName}.restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rollbackDirectory);
        string rollback = Path.Combine(rollbackDirectory, $"{fileName}.rollback-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        File.Copy(backupPath, candidate, true);

        database.Close();
        try
        {
            File.Replace(candidate, dataSource, rollback, true);
            DeleteSidecar(dataSource + "-wal");
            DeleteSidecar(dataSource + "-shm");
            database.Configure(directory, fileName);
            if (!string.Equals(RunIntegrityCheck(true), "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(MaintenanceError.RestoredDatabaseIntegrityCheckFailed.ToString());
            DatabaseBackupResult rollbackResult = new(rollback, GetLength(rollback), ComputeSha256(rollback), DateTimeOffset.UtcNow);
            RecordAudit("Database.Restore", backupPath, rollbackResult.Sha256);
            return rollbackResult;
        }
        catch
        {
            database.Close();
            if (File.Exists(rollback))
            {
                File.Replace(rollback, dataSource, null, true);
                DeleteSidecar(dataSource + "-wal");
                DeleteSidecar(dataSource + "-shm");
            }
            if (File.Exists(dataSource)) database.Configure(directory, fileName);
            throw;
        }
        finally
        {
            DeleteSidecar(candidate);
        }
    }

    public DatabaseBackupResult CompactAndReplace(string backupDirectory, bool exclusiveAccessConfirmed)
    {
        EnsureConfigured();
        if (!exclusiveAccessConfirmed) throw new InvalidOperationException(MaintenanceError.ExclusiveAccessRequired.ToString());
        string dataSource = Path.GetFullPath(database.DataSource);
        string directory = Path.GetDirectoryName(dataSource) ?? throw new InvalidOperationException(MaintenanceError.DatabaseDirectoryUnavailable.ToString());
        EnsureFreeSpace(directory, checked(Math.Max(GetLength(dataSource) * 2, 64L * 1024 * 1024)));
        DatabaseBackupResult safetyBackup = CreateVerifiedBackup(backupDirectory);
        string fileName = Path.GetFileName(dataSource);
        string candidate = Path.Combine(directory, $".{fileName}.compact-{Guid.NewGuid():N}");
        string rollback = Path.Combine(backupDirectory, "Rollback", $"{fileName}.precompact-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.GetDirectoryName(rollback)!);

        database.ExecuteNonQuery("PRAGMA wal_checkpoint(TRUNCATE)");
        using (SqliteCommand command = database.Connection.CreateCommand())
        {
            command.CommandText = "VACUUM INTO $path";
            command.Parameters.AddWithValue("$path", candidate);
            command.ExecuteNonQuery();
        }
        ValidateDatabaseFile(candidate);
        database.Close();
        try
        {
            File.Replace(candidate, dataSource, rollback, true);
            DeleteSidecar(dataSource + "-wal");
            DeleteSidecar(dataSource + "-shm");
            database.Configure(directory, fileName);
            if (!string.Equals(RunIntegrityCheck(true), "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(MaintenanceError.CompactedDatabaseIntegrityCheckFailed.ToString());
            RecordAudit("Database.Compact", dataSource, safetyBackup.Sha256);
            return safetyBackup;
        }
        catch
        {
            database.Close();
            if (File.Exists(rollback)) File.Replace(rollback, dataSource, null, true);
            DeleteSidecar(dataSource + "-wal");
            DeleteSidecar(dataSource + "-shm");
            if (File.Exists(dataSource)) database.Configure(directory, fileName);
            throw;
        }
        finally
        {
            DeleteSidecar(candidate);
        }
    }

    private static int DeleteBatch(SqliteConnection connection, SqliteTransaction transaction, string sql, DateTimeOffset boundary, int batchSize, int? unlocked = null)
    {
        return connection.Execute(sql, new { Boundary = boundary.UtcDateTime, BatchSize = batchSize, Unlocked = unlocked }, transaction);
    }

    private static void ValidatePolicy(DatabaseRetentionPolicy policy)
    {
        if (policy.IntrusionLogDays < 1 || policy.UnlockedLockDays < 1 || policy.AuditDays < 1 || policy.CompletedInboxDays < 1)
            throw new ArgumentOutOfRangeException(nameof(policy));
        if (policy.BatchSize is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(policy));
    }

    private void ValidateDatabaseFile(string path)
    {
        using SqliteConnection connection = database.CreateEncryptedConnection(path, SqliteOpenMode.ReadOnly);
        connection.Open();
        string result = Convert.ToString(connection.ExecuteScalar("PRAGMA integrity_check"), CultureInfo.InvariantCulture) ?? string.Empty;
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"{MaintenanceError.IntegrityCheckFailed}:{result}");
    }

    private static void EnsureFreeSpace(string directory, long requiredBytes)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(directory)) ?? throw new InvalidOperationException(MaintenanceError.DatabaseDirectoryUnavailable.ToString());
        if (new DriveInfo(root).AvailableFreeSpace < requiredBytes)
            throw new IOException(MaintenanceError.InsufficientDiskSpace.ToString());
    }

    private long ReadInt64(string sql) => Convert.ToInt64(database.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    private void EnsureConfigured() { if (!database.IsConfigured) throw new InvalidOperationException(MaintenanceError.DatabaseNotConfigured.ToString()); }
    private static long GetLength(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    private static void DeleteSidecar(string path) { if (File.Exists(path)) File.Delete(path); }

    private void RecordAudit(string eventType, string subject, string details) => database.ExecuteNonQuery(
        "INSERT INTO ProtectionAuditLog(OccurredUtc, EventType, Outcome, Actor, Subject, Details) VALUES (@p0,@p1,@p2,@p3,@p4,@p5)",
        DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture), eventType, "Succeeded", "DatabaseMaintenance", subject, details);

    private sealed record DatabaseMaintenanceHistoryRow(string OccurredUtc, string EventType, string Outcome, string Subject, string Details);

    private enum MaintenanceError
    {
        BackupIntegrityCheckFailed,
        BackupFileNotFound,
        DatabaseDirectoryUnavailable,
        RestoredDatabaseIntegrityCheckFailed,
        IntegrityCheckFailed,
        DatabaseNotConfigured,
        ExclusiveAccessRequired,
        CompactedDatabaseIntegrityCheckFailed,
        InsufficientDiskSpace
    }
}
