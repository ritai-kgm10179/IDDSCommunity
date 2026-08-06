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
        Execute(connection, transaction, CreateProtectionAuditLogIndex);
        Execute(connection, transaction, CreateProtectionEventInbox);
        Execute(connection, transaction, CreateProtectionEventInboxStatusIndex);

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
        transaction.Commit();
    }

    private static bool TableExists(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
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
}
