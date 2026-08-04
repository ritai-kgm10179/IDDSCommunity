using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Cyberarms.IntrusionDetection.Shared.Db;

internal static class SchemaMigrationRunner
{
    private const string CreateJournal = "CREATE TABLE IF NOT EXISTS SchemaMigrations (Version INTEGER PRIMARY KEY NOT NULL, AppliedUtc TEXT NOT NULL)";

    /// <summary>
    /// Applies all pending schema migrations atomically and records their versions.
    /// </summary>
    /// <param name="connection">The open SQLite connection to migrate.</param>
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

        using SqliteCommand journal = connection.CreateCommand();
        journal.Transaction = transaction;
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (1, $appliedUtc)";
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
    /// Rejects incomplete legacy databases instead of incorrectly marking them as migrated.
    /// </summary>
    /// <param name="connection">The open SQLite connection.</param>
    /// <param name="transaction">The active migration transaction.</param>
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
}
