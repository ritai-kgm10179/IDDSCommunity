using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared.Db;

internal static class SchemaMigrationRunner
{
    private const string CreateJournal = "CREATE TABLE IF NOT EXISTS SchemaMigrations (Version INTEGER PRIMARY KEY NOT NULL, AppliedUtc TEXT NOT NULL)";
    /// <summary>
    /// 原子化地套用所有未處理的結構描述移轉並紀錄其版本。
    /// </summary>
    /// <param name="connection">要進行移轉的已開啟 SQLite 資料庫連線。</param>
    internal static void Migrate(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();
        Execute(connection, transaction, CreateJournal);

        if (!TableExists(connection, transaction, "DbConfig"))
        {
            foreach (string command in InitialSchemaCommands)
                Execute(connection, transaction, command);
        }
        else
        {
            ValidateExistingSchema(connection, transaction);
        }

        Execute(connection, transaction, CreateProtectionAuditLog);
        if (TableExists(connection, transaction, "ProtectionAuditLog"))
        {
            if (!ColumnExists(connection, transaction, "ProtectionAuditLog", "AlertId"))
                Execute(connection, transaction, "ALTER TABLE ProtectionAuditLog ADD COLUMN AlertId TEXT NULL");
        }
        Execute(connection, transaction, CreateProtectionAuditLogIndex);
        Execute(connection, transaction, CreateProtectionAuditLogAlertIdIndex);
        Execute(connection, transaction, CreateProtectionEventInbox);
        Execute(connection, transaction, CreateProtectionEventInboxStatusIndex);
        Execute(connection, transaction, CreateIntrusionLogWindowIndex);
        Execute(connection, transaction, CreateObservationWatermarks);
        Execute(connection, transaction, CreateSecurityObservationEvents);
        if (TableExists(connection, transaction, "SecurityObservationEvents"))
        {
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "IdempotencyKey"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN IdempotencyKey TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "AlertEmitted"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN AlertEmitted INTEGER NOT NULL DEFAULT 0");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "IsCredentialFailure"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN IsCredentialFailure INTEGER NOT NULL DEFAULT 1");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "ActivityId"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN ActivityId TEXT NULL");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "TargetResource"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN TargetResource TEXT NULL");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "ErrorCode"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN ErrorCode TEXT NULL");
        }
        Execute(connection, transaction, CreateSecurityObservationEventsIdempotencyIndex);
        Execute(connection, transaction, CreateSecurityObservationEventsTimeIpIndex);
        Execute(connection, transaction, CreateSecurityObservationEventsCorrelationIndex);
        Execute(connection, transaction, CreateObservationAlertOutbox);
        Execute(connection, transaction, CreateObservationAlertOutboxStatusIndex);

        if (!MigrationApplied(connection, transaction, 5))
            MigrateLegacyLocalTimestampsToUtc(connection, transaction);

        using SqliteCommand journal = connection.CreateCommand();
        journal.Transaction = transaction;
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (1, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (2, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (3, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (4, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (5, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (6, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (7, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        transaction.Commit();
    }

    private static bool MigrationApplied(SqliteConnection connection, SqliteTransaction transaction, int version)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Version = $version";
        command.Parameters.AddWithValue("$version", version);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// 一次性地將既有以本機時區儲存的 <c>IntrusionLog.IncidentTime</c> 與 <c>Locks</c> 資料表時間欄位
    /// 轉換為 UTC，使其與應用程式改採 UTC 儲存/比較之新慣例一致，避免新舊資料混用時區語意。
    /// 換算採用目前機器的 UTC 位移量；僅在版本 5 尚未套用時執行一次，且與其餘結構描述異動同屬一個交易。
    /// </summary>
    /// <param name="connection">已開啟的 SQLite 資料庫連線。</param>
    /// <param name="transaction">作用中的移轉交易。</param>
    private static void MigrateLegacyLocalTimestampsToUtc(SqliteConnection connection, SqliteTransaction transaction)
    {
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
        if (offset == TimeSpan.Zero)
            return;
        string modifier = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{-offset.TotalMinutes} minutes");
        foreach ((string table, string column) in LegacyLocalTimestampColumns)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"UPDATE {table} SET {column} = datetime({column}, $modifier) WHERE {column} IS NOT NULL";
            command.Parameters.AddWithValue("$modifier", modifier);
            command.ExecuteNonQuery();
        }
    }

    private static IReadOnlyList<(string Table, string Column)> LegacyLocalTimestampColumns { get; } =
    [
        ("IntrusionLog", "IncidentTime"),
        ("Locks", "LockDate"),
        ("Locks", "UnlockDate"),
        ("Locks", "LastUpdate")
    ];

    private static bool TableExists(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info({tableName})";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(1);
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
    /// <summary>
    /// 拒絕不完整的舊版資料庫，而非錯誤地將其標記為已移轉。
    /// </summary>
    /// <param name="connection">已開啟的 SQLite 資料庫連線。</param>
    /// <param name="transaction">作用中的移轉交易。</param>
    private static void ValidateExistingSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (string tableName in RequiredInitialTables)
        {
            if (!TableExists(connection, transaction, tableName))
                throw new InvalidOperationException(string.Format(Localization.Strings.Get("The existing database is missing the required table '{0}'."), tableName));
        }
    }

    private static IReadOnlyList<string> RequiredInitialTables { get; } =
    [
        "DbConfig",
        "Configuration",
        "IntrusionLog",
        "Locks",
        "SecurityAgentConfig",
        "SecurityAgents",
        "AppConfig",
        "Whitelist",
        "AgentStatistics"
    ];

    private static IReadOnlyList<string> InitialSchemaCommands { get; } =
    [
        Version_2_1.TABLE_DB_CONFIG,
        Version_2_1.TABLE_CONFIGURATION,
        Version_2_1.CREATE_DEFAULT_DB_CONFIGURATION,
        Version_2_1.CREATE_DEFAULT_CONFIGURATION,
        Version_2_1.TABLE_INTRUSION_LOG,
        Version_2_1.TABLE_LOCKS,
        Version_2_1.TABLE_SECURITY_AGENT_CONFIG,
        Version_2_1.TABLE_SECURITY_AGENTS,
        Version_2_1.TABLE_APP_CONFIG,
        Version_2_1.TABLE_WHITE_LIST,
        Version_2_1.TABLE_AGENT_STATISTICS
    ];

    private const string CreateProtectionAuditLog = """
        CREATE TABLE IF NOT EXISTS ProtectionAuditLog (
            Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
            AlertId TEXT NULL,
            OccurredUtc TEXT NOT NULL,
            EventType TEXT NOT NULL,
            Outcome TEXT NOT NULL,
            Actor TEXT NOT NULL,
            Subject TEXT NOT NULL,
            Details TEXT NOT NULL
        )
        """;

    private const string CreateProtectionAuditLogIndex =
        "CREATE INDEX IF NOT EXISTS IX_ProtectionAuditLog_OccurredUtc ON ProtectionAuditLog(OccurredUtc)";

    private const string CreateProtectionAuditLogAlertIdIndex =
        "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProtectionAuditLog_AlertId ON ProtectionAuditLog(AlertId)";

    private const string CreateProtectionEventInbox = """
        CREATE TABLE IF NOT EXISTS ProtectionEventInbox (
            Id TEXT PRIMARY KEY NOT NULL,
            ReceivedUtc TEXT NOT NULL,
            AgentName TEXT NOT NULL,
            CreateDate TEXT NOT NULL,
            EventId INTEGER NOT NULL,
            IpAddress TEXT NOT NULL,
            EventMessage TEXT NOT NULL,
            Status INTEGER NOT NULL,
            Attempts INTEGER NOT NULL,
            LastError TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        )
        """;

    private const string CreateProtectionEventInboxStatusIndex =
        "CREATE INDEX IF NOT EXISTS IX_ProtectionEventInbox_Status_ReceivedUtc ON ProtectionEventInbox(Status, ReceivedUtc)";

    private const string CreateIntrusionLogWindowIndex =
        "CREATE INDEX IF NOT EXISTS IX_IntrusionLog_IncidentTime ON IntrusionLog(IncidentTime)";

    private const string CreateObservationWatermarks = """
        CREATE TABLE IF NOT EXISTS ObservationWatermarks (
            SourceAgentName TEXT NOT NULL,
            ProviderOrChannel TEXT NOT NULL,
            LastEventRecordId INTEGER NULL,
            LastTimestampUtc TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL,
            PRIMARY KEY (SourceAgentName, ProviderOrChannel)
        )
        """;

    private const string CreateSecurityObservationEvents = """
        CREATE TABLE IF NOT EXISTS SecurityObservationEvents (
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
            IsCredentialFailure INTEGER NOT NULL DEFAULT 1,
            ActivityId TEXT NULL,
            TargetResource TEXT NULL,
            ErrorCode TEXT NULL,
            AlertEmitted INTEGER NOT NULL DEFAULT 0
        )
        """;

    private const string CreateSecurityObservationEventsIdempotencyIndex =
        "CREATE UNIQUE INDEX IF NOT EXISTS IX_SecurityObservationEvents_IdempotencyKey ON SecurityObservationEvents(IdempotencyKey)";

    private const string CreateSecurityObservationEventsTimeIpIndex =
        "CREATE INDEX IF NOT EXISTS IX_SecurityObservationEvents_Time_Ip ON SecurityObservationEvents(EventTimeUtc, NormalizedIpAddress)";

    private const string CreateSecurityObservationEventsCorrelationIndex =
        "CREATE INDEX IF NOT EXISTS IX_SecurityObservationEvents_CorrelationGroupId ON SecurityObservationEvents(CorrelationGroupId)";

    private const string CreateObservationAlertOutbox = """
        CREATE TABLE IF NOT EXISTS ObservationAlertOutbox (
            AlertId TEXT PRIMARY KEY NOT NULL,
            ObservationId TEXT NOT NULL,
            OccurredUtc TEXT NOT NULL,
            EventType TEXT NOT NULL,
            Outcome TEXT NOT NULL,
            Actor TEXT NOT NULL,
            Subject TEXT NOT NULL,
            Details TEXT NOT NULL,
            Status INTEGER NOT NULL,
            DispatchedUtc TEXT NULL,
            CreatedUtc TEXT NOT NULL
        )
        """;

    private const string CreateObservationAlertOutboxStatusIndex =
        "CREATE INDEX IF NOT EXISTS IX_ObservationAlertOutbox_Status ON ObservationAlertOutbox(Status)";
}
