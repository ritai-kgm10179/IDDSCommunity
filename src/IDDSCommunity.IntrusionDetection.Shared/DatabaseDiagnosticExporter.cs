using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 以唯讀方式匯出不含 IP、帳號、事件內容及資料庫金鑰的資料庫診斷摘要。
/// </summary>
public static class DatabaseDiagnosticExporter
{
    /// <summary>
    /// 從部署資料庫匯出首頁統計診斷所需的去識別化 JSON 摘要。
    /// </summary>
    /// <param name="databasePath">加密 SQLite 資料庫的完整路徑。</param>
    /// <param name="outputPath">診斷 JSON 的輸出路徑。</param>
    public static void Export(string databasePath, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        string fullDatabasePath = Path.GetFullPath(databasePath);
        string fullOutputPath = Path.GetFullPath(outputPath);
        if (!File.Exists(fullDatabasePath))
            throw new FileNotFoundException(Localization.Strings.Get("The specified encrypted database was not found."), fullDatabasePath);
        if (string.Equals(fullDatabasePath, fullOutputPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(Localization.Strings.Get("The diagnostic output path must not overwrite the database."), nameof(outputPath));

        SQLitePCL.Batteries_V2.Init();
        string password = DatabaseEncryptionKeyStore.ReadExistingPassword(fullDatabasePath);
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = fullDatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            Password = password
        }.ConnectionString);
        connection.Open();
        using (SqliteCommand queryOnly = connection.CreateCommand())
        {
            queryOnly.CommandText = "PRAGMA query_only=ON";
            queryOnly.ExecuteNonQuery();
        }

        Dictionary<string, object?> report = new(StringComparer.Ordinal)
        {
            ["formatVersion"] = 1,
            ["generatedUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["databaseFileName"] = Path.GetFileName(fullDatabasePath),
            ["schemaMigrations"] = ReadSchemaMigrations(connection),
            ["tableCounts"] = ReadTableCounts(connection),
            ["intrusionSummary"] = ReadIntrusionSummary(connection),
            ["correlationSummary"] = ReadCorrelationSummary(connection),
            ["agentStatistics"] = ReadAgentStatistics(connection),
            ["configuredAgents"] = ReadConfiguredAgents(connection)
        };

        string? outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullOutputPath, json, new UTF8Encoding(false));
    }

    private static List<Dictionary<string, object?>> ReadSchemaMigrations(SqliteConnection connection)
    {
        List<Dictionary<string, object?>> rows = [];
        if (!TableExists(connection, "SchemaMigrations")) return rows;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Version, AppliedUtc FROM SchemaMigrations ORDER BY Version";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
            rows.Add(new Dictionary<string, object?> { ["version"] = reader.GetInt64(0), ["appliedUtc"] = reader.GetValue(1)?.ToString() });
        return rows;
    }

    private static Dictionary<string, long> ReadTableCounts(SqliteConnection connection)
    {
        Dictionary<string, long> counts = new(StringComparer.Ordinal);
        foreach (string table in new[] { "IntrusionLog", "AgentStatistics", "SecurityAgents", "Locks", "SecurityObservationEvents", "ProtectionEventInbox", "ProtectionAuditLog" })
        {
            if (!TableExists(connection, table)) continue;
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            counts[table] = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        return counts;
    }

    private static Dictionary<string, object?> ReadIntrusionSummary(SqliteConnection connection)
    {
        Dictionary<string, object?> summary = new(StringComparer.Ordinal);
        if (!TableExists(connection, "IntrusionLog")) return summary;
        using (SqliteCommand range = connection.CreateCommand())
        {
            range.CommandText = "SELECT MIN(IncidentTime), MAX(IncidentTime) FROM IntrusionLog";
            using SqliteDataReader reader = range.ExecuteReader();
            if (reader.Read())
            {
                summary["minimumIncidentTime"] = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                summary["maximumIncidentTime"] = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString();
            }
        }

        List<Dictionary<string, object?>> groups = [];
        using SqliteCommand grouped = connection.CreateCommand();
        grouped.CommandText = "SELECT AgentId, Action, COUNT(*) FROM IntrusionLog GROUP BY AgentId, Action ORDER BY AgentId, Action";
        using SqliteDataReader groupedReader = grouped.ExecuteReader();
        while (groupedReader.Read())
        {
            string rawId = groupedReader.GetValue(0)?.ToString() ?? string.Empty;
            groups.Add(new Dictionary<string, object?>
            {
                ["agentId"] = rawId,
                ["isGuid"] = Guid.TryParse(rawId, out _),
                ["action"] = groupedReader.GetInt64(1),
                ["count"] = groupedReader.GetInt64(2)
            });
        }
        summary["byAgentAndAction"] = groups;
        return summary;
    }

    private static List<Dictionary<string, object?>> ReadCorrelationSummary(SqliteConnection connection)
    {
        List<Dictionary<string, object?>> rows = [];
        if (!TableExists(connection, "SecurityObservationEvents")) return rows;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT SourceAgentName,
                   COUNT(*),
                   SUM(CASE WHEN IsCredentialFailure <> 0 THEN 1 ELSE 0 END),
                   SUM(CASE WHEN IsCrossSourceDuplicate <> 0 THEN 1 ELSE 0 END)
            FROM SecurityObservationEvents
            GROUP BY SourceAgentName
            ORDER BY SourceAgentName
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["sourceAgentName"] = reader.GetValue(0)?.ToString() ?? string.Empty,
                ["observations"] = reader.GetInt64(1),
                ["credentialFailures"] = reader.GetInt64(2),
                ["crossSourceDuplicates"] = reader.GetInt64(3)
            });
        }
        return rows;
    }

    private static List<Dictionary<string, object?>> ReadAgentStatistics(SqliteConnection connection)
    {
        List<Dictionary<string, object?>> rows = [];
        if (!TableExists(connection, "AgentStatistics")) return rows;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT AgentId, FailedLogins, SoftLocks, HardLocks FROM AgentStatistics ORDER BY AgentId";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string rawId = reader.GetValue(0)?.ToString() ?? string.Empty;
            rows.Add(new Dictionary<string, object?>
            {
                ["agentId"] = rawId,
                ["isGuid"] = Guid.TryParse(rawId, out _),
                ["failedLogins"] = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                ["softLocks"] = Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture),
                ["hardLocks"] = Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture)
            });
        }
        return rows;
    }

    private static List<Dictionary<string, object?>> ReadConfiguredAgents(SqliteConnection connection)
    {
        List<Dictionary<string, object?>> rows = [];
        if (!TableExists(connection, "SecurityAgents")) return rows;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT AgentId, Name, DisplayName, AssemblyName, Enabled FROM SecurityAgents ORDER BY AgentId";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string rawId = reader.GetValue(0)?.ToString() ?? string.Empty;
            string name = reader.GetValue(1)?.ToString() ?? string.Empty;
            string displayName = reader.GetValue(2)?.ToString() ?? string.Empty;
            string assemblyName = reader.GetValue(3)?.ToString() ?? string.Empty;
            Guid canonicalId = Guid.Empty;
            bool resolved = WellKnownAgentIds.TryResolveCanonicalGuid(name, out canonicalId) ||
                WellKnownAgentIds.TryResolveCanonicalGuid(displayName, out canonicalId) ||
                WellKnownAgentIds.TryResolveCanonicalGuid(assemblyName, out canonicalId);
            rows.Add(new Dictionary<string, object?>
            {
                ["agentId"] = rawId,
                ["isGuid"] = Guid.TryParse(rawId, out _),
                ["resolvedCanonicalAgentId"] = resolved ? canonicalId.ToString() : null,
                ["matchesCanonicalAgentId"] = resolved && string.Equals(rawId, canonicalId.ToString(), StringComparison.OrdinalIgnoreCase),
                ["name"] = name,
                ["displayName"] = displayName,
                ["assemblyName"] = assemblyName,
                ["enabled"] = Convert.ToInt64(reader.GetValue(4), CultureInfo.InvariantCulture) != 0
            });
        }
        return rows;
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }
}
