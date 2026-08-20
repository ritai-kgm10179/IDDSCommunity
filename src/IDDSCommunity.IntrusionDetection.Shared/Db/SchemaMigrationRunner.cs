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
        if (TableExists(connection, transaction, "ProtectionEventInbox"))
        {
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "IsAuthenticationEvent"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN IsAuthenticationEvent INTEGER NOT NULL DEFAULT 0");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "AccountName"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN AccountName TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "AccountDomain"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN AccountDomain TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "AccountSid"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN AccountSid TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "IsCredentialFailure"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN IsCredentialFailure INTEGER NOT NULL DEFAULT 1");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "ProviderOrChannel"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN ProviderOrChannel TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "ComputerName"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN ComputerName TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "SourceEventRecordId"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN SourceEventRecordId INTEGER NULL");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "ActivityId"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN ActivityId TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "ConfidenceScore"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN ConfidenceScore REAL NOT NULL DEFAULT 1.0");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "TargetResource"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN TargetResource TEXT NOT NULL DEFAULT ''");
            if (!ColumnExists(connection, transaction, "ProtectionEventInbox", "ErrorCode"))
                Execute(connection, transaction, "ALTER TABLE ProtectionEventInbox ADD COLUMN ErrorCode TEXT NOT NULL DEFAULT ''");
        }
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
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "AccountSid"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN AccountSid TEXT NULL");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "IsCrossSourceDuplicate"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN IsCrossSourceDuplicate INTEGER NOT NULL DEFAULT 0");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "DuplicateOfObservationId"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN DuplicateOfObservationId TEXT NULL");
            if (!ColumnExists(connection, transaction, "SecurityObservationEvents", "CorrelationProcessed"))
                Execute(connection, transaction, "ALTER TABLE SecurityObservationEvents ADD COLUMN CorrelationProcessed INTEGER NOT NULL DEFAULT 1");
        }
        Execute(connection, transaction, CreateSecurityObservationEventsIdempotencyIndex);
        Execute(connection, transaction, CreateSecurityObservationEventsTimeIpIndex);
        Execute(connection, transaction, CreateSecurityObservationEventsCorrelationIndex);
        Execute(connection, transaction, CreateSecurityObservationEventsDuplicateIndex);
        Execute(connection, transaction, CreateObservationAlertOutbox);
        Execute(connection, transaction, CreateObservationAlertOutboxStatusIndex);

        if (!MigrationApplied(connection, transaction, 5))
            MigrateLegacyLocalTimestampsToUtc(connection, transaction);
        if (!MigrationApplied(connection, transaction, 11))
            CanonicalizePersistedIpAddresses(connection, transaction);
        if (!MigrationApplied(connection, transaction, 12))
            CanonicalizeAgentIdentities(connection, transaction);

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
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (8, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (9, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (10, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (11, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        journal.Parameters.Clear();
        journal.CommandText = "INSERT OR IGNORE INTO SchemaMigrations(Version, AppliedUtc) VALUES (12, $appliedUtc)";
        journal.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        journal.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void CanonicalizeAgentIdentities(SqliteConnection connection, SqliteTransaction transaction)
    {
        Dictionary<string, string> idMap = new(StringComparer.OrdinalIgnoreCase);

        // 1. 先從 SecurityAgents 資料表建立舊 AgentId 與名稱/顯示名稱到 Canonical GUID 的對照
        if (TableExists(connection, transaction, "SecurityAgents") && ColumnExists(connection, transaction, "SecurityAgents", "AgentId"))
        {
            bool hasName = ColumnExists(connection, transaction, "SecurityAgents", "Name");
            bool hasDisplayName = ColumnExists(connection, transaction, "SecurityAgents", "DisplayName");
            bool hasAssemblyName = ColumnExists(connection, transaction, "SecurityAgents", "AssemblyName");

            using (SqliteCommand select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT AgentId" +
                    (hasName ? ", Name" : ", '' AS Name") +
                    (hasDisplayName ? ", DisplayName" : ", '' AS DisplayName") +
                    (hasAssemblyName ? ", AssemblyName" : ", '' AS AssemblyName") +
                    " FROM SecurityAgents";
                using SqliteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    string oldId = reader.GetValue(0)?.ToString() ?? string.Empty;
                    string name = reader.GetValue(1)?.ToString() ?? string.Empty;
                    string displayName = reader.GetValue(2)?.ToString() ?? string.Empty;
                    string assemblyName = reader.GetValue(3)?.ToString() ?? string.Empty;

                    if (WellKnownAgentIds.TryResolveCanonicalGuid(name, out Guid canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(displayName, out canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(assemblyName, out canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(oldId, out canonical))
                    {
                        if (!string.IsNullOrWhiteSpace(oldId)) idMap[oldId] = canonical.ToString();
                        if (!string.IsNullOrWhiteSpace(name)) idMap[name] = canonical.ToString();
                        if (!string.IsNullOrWhiteSpace(displayName)) idMap[displayName] = canonical.ToString();
                    }
                }
            }
        }

        // 2. IntrusionLog 資料表 AgentId 正規化
        if (TableExists(connection, transaction, "IntrusionLog") && ColumnExists(connection, transaction, "IntrusionLog", "AgentId"))
        {
            List<(string OldId, string CanonicalId)> updates = [];
            using (SqliteCommand select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT DISTINCT AgentId FROM IntrusionLog WHERE AgentId IS NOT NULL";
                using SqliteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    string oldId = reader.GetValue(0)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(oldId)) continue;

                    string? canonicalId = null;
                    if (idMap.TryGetValue(oldId, out string? mapped))
                        canonicalId = mapped;
                    else if (WellKnownAgentIds.TryResolveCanonicalGuid(oldId, out Guid resolved))
                        canonicalId = resolved.ToString();

                    if (!string.IsNullOrEmpty(canonicalId) && !string.Equals(oldId, canonicalId, StringComparison.OrdinalIgnoreCase))
                    {
                        updates.Add((oldId, canonicalId));
                    }
                }
            }
            foreach ((string oldId, string canonicalId) in updates)
            {
                using SqliteCommand update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE IntrusionLog SET AgentId=$canonicalId WHERE AgentId=$oldId";
                update.Parameters.AddWithValue("$canonicalId", canonicalId);
                update.Parameters.AddWithValue("$oldId", oldId);
                update.ExecuteNonQuery();
            }
        }

        // 3. AgentStatistics 資料表 AgentId 正規化與合併
        if (TableExists(connection, transaction, "AgentStatistics") &&
            ColumnExists(connection, transaction, "AgentStatistics", "AgentId") &&
            ColumnExists(connection, transaction, "AgentStatistics", "FailedLogins") &&
            ColumnExists(connection, transaction, "AgentStatistics", "HardLocks") &&
            ColumnExists(connection, transaction, "AgentStatistics", "SoftLocks"))
        {
            List<(string OldId, string CanonicalId, int FailedLogins, int HardLocks, int SoftLocks)> pendingMerges = [];
            using (SqliteCommand select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT AgentId, FailedLogins, HardLocks, SoftLocks FROM AgentStatistics";
                using SqliteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    string oldId = reader.GetValue(0)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(oldId)) continue;

                    string? canonicalId = null;
                    if (idMap.TryGetValue(oldId, out string? mapped))
                        canonicalId = mapped;
                    else if (WellKnownAgentIds.TryResolveCanonicalGuid(oldId, out Guid resolved))
                        canonicalId = resolved.ToString();

                    if (!string.IsNullOrEmpty(canonicalId) && !string.Equals(oldId, canonicalId, StringComparison.OrdinalIgnoreCase))
                    {
                        int failed = Convert.ToInt32(reader["FailedLogins"]);
                        int hard = Convert.ToInt32(reader["HardLocks"]);
                        int soft = Convert.ToInt32(reader["SoftLocks"]);
                        pendingMerges.Add((oldId, canonicalId, failed, hard, soft));
                    }
                }
            }

            foreach ((string oldId, string canonicalId, int failed, int hard, int soft) in pendingMerges)
            {
                bool targetExists = false;
                using (SqliteCommand check = connection.CreateCommand())
                {
                    check.Transaction = transaction;
                    check.CommandText = "SELECT COUNT(*) FROM AgentStatistics WHERE AgentId=$canonicalId";
                    check.Parameters.AddWithValue("$canonicalId", canonicalId);
                    targetExists = Convert.ToInt64(check.ExecuteScalar()) > 0;
                }

                if (targetExists)
                {
                    using SqliteCommand update = connection.CreateCommand();
                    update.Transaction = transaction;
                    update.CommandText = @"UPDATE AgentStatistics
                                           SET FailedLogins=FailedLogins+$failed,
                                               HardLocks=HardLocks+$hard,
                                               SoftLocks=SoftLocks+$soft
                                           WHERE AgentId=$canonicalId";
                    update.Parameters.AddWithValue("$failed", failed);
                    update.Parameters.AddWithValue("$hard", hard);
                    update.Parameters.AddWithValue("$soft", soft);
                    update.Parameters.AddWithValue("$canonicalId", canonicalId);
                    update.ExecuteNonQuery();

                    using SqliteCommand delete = connection.CreateCommand();
                    delete.Transaction = transaction;
                    delete.CommandText = "DELETE FROM AgentStatistics WHERE AgentId=$oldId";
                    delete.Parameters.AddWithValue("$oldId", oldId);
                    delete.ExecuteNonQuery();
                }
                else
                {
                    using SqliteCommand update = connection.CreateCommand();
                    update.Transaction = transaction;
                    update.CommandText = "UPDATE AgentStatistics SET AgentId=$canonicalId WHERE AgentId=$oldId";
                    update.Parameters.AddWithValue("$canonicalId", canonicalId);
                    update.Parameters.AddWithValue("$oldId", oldId);
                    update.ExecuteNonQuery();
                }
            }
        }

        // 4. SecurityAgentConfig 資料表 AgentId 正規化
        if (TableExists(connection, transaction, "SecurityAgentConfig") &&
            ColumnExists(connection, transaction, "SecurityAgentConfig", "AgentId") &&
            ColumnExists(connection, transaction, "SecurityAgentConfig", "PropertyName"))
        {
            List<(string OldId, string CanonicalId, string PropertyName, string? PropertyValue)> configs = [];
            using (SqliteCommand select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT AgentId, PropertyName, PropertyValueString FROM SecurityAgentConfig";
                using SqliteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    string oldId = reader.GetValue(0)?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(oldId)) continue;

                    string? canonicalId = null;
                    if (idMap.TryGetValue(oldId, out string? mapped))
                        canonicalId = mapped;
                    else if (WellKnownAgentIds.TryResolveCanonicalGuid(oldId, out Guid resolved))
                        canonicalId = resolved.ToString();

                    if (!string.IsNullOrEmpty(canonicalId) && !string.Equals(oldId, canonicalId, StringComparison.OrdinalIgnoreCase))
                    {
                        string prop = reader.GetValue(1)?.ToString() ?? string.Empty;
                        string? val = reader.GetValue(2)?.ToString();
                        configs.Add((oldId, canonicalId, prop, val));
                    }
                }
            }

            foreach ((string oldId, string canonicalId, string prop, string? val) in configs)
            {
                using SqliteCommand delete = connection.CreateCommand();
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM SecurityAgentConfig WHERE AgentId=$oldId AND PropertyName=$prop";
                delete.Parameters.AddWithValue("$oldId", oldId);
                delete.Parameters.AddWithValue("$prop", prop);
                delete.ExecuteNonQuery();

                using SqliteCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = @"INSERT OR REPLACE INTO SecurityAgentConfig (AgentId, PropertyName, PropertyValueString)
                                       VALUES ($canonicalId, $prop, $val)";
                insert.Parameters.AddWithValue("$canonicalId", canonicalId);
                insert.Parameters.AddWithValue("$prop", prop);
                insert.Parameters.AddWithValue("$val", (object?)val ?? DBNull.Value);
                insert.ExecuteNonQuery();
            }
        }

        // 5. SecurityAgents 資料表 AgentId 正規化
        if (TableExists(connection, transaction, "SecurityAgents") && ColumnExists(connection, transaction, "SecurityAgents", "AgentId"))
        {
            bool hasName = ColumnExists(connection, transaction, "SecurityAgents", "Name");
            bool hasDisplayName = ColumnExists(connection, transaction, "SecurityAgents", "DisplayName");
            bool hasAssemblyName = ColumnExists(connection, transaction, "SecurityAgents", "AssemblyName");

            List<(string OldId, string CanonicalId)> agentRows = [];
            using (SqliteCommand select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT AgentId" +
                    (hasName ? ", Name" : ", '' AS Name") +
                    (hasDisplayName ? ", DisplayName" : ", '' AS DisplayName") +
                    (hasAssemblyName ? ", AssemblyName" : ", '' AS AssemblyName") +
                    " FROM SecurityAgents";
                using SqliteDataReader reader = select.ExecuteReader();
                while (reader.Read())
                {
                    string oldId = reader.GetValue(0)?.ToString() ?? string.Empty;
                    string name = reader.GetValue(1)?.ToString() ?? string.Empty;
                    string displayName = reader.GetValue(2)?.ToString() ?? string.Empty;
                    string assemblyName = reader.GetValue(3)?.ToString() ?? string.Empty;

                    if (WellKnownAgentIds.TryResolveCanonicalGuid(name, out Guid canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(displayName, out canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(assemblyName, out canonical) ||
                        WellKnownAgentIds.TryResolveCanonicalGuid(oldId, out canonical))
                    {
                        if (!string.Equals(oldId, canonical.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            agentRows.Add((oldId, canonical.ToString()));
                        }
                    }
                }
            }

            foreach ((string oldId, string canonicalId) in agentRows)
            {
                bool targetExists = false;
                using (SqliteCommand check = connection.CreateCommand())
                {
                    check.Transaction = transaction;
                    check.CommandText = "SELECT COUNT(*) FROM SecurityAgents WHERE AgentId=$canonicalId";
                    check.Parameters.AddWithValue("$canonicalId", canonicalId);
                    targetExists = Convert.ToInt64(check.ExecuteScalar()) > 0;
                }

                if (targetExists)
                {
                    using SqliteCommand delete = connection.CreateCommand();
                    delete.Transaction = transaction;
                    delete.CommandText = "DELETE FROM SecurityAgents WHERE AgentId=$oldId";
                    delete.Parameters.AddWithValue("$oldId", oldId);
                    delete.ExecuteNonQuery();
                }
                else
                {
                    using SqliteCommand update = connection.CreateCommand();
                    update.Transaction = transaction;
                    update.CommandText = "UPDATE SecurityAgents SET AgentId=$canonicalId WHERE AgentId=$oldId";
                    update.Parameters.AddWithValue("$canonicalId", canonicalId);
                    update.Parameters.AddWithValue("$oldId", oldId);
                    update.ExecuteNonQuery();
                }
            }
        }
    }

    private static void CanonicalizePersistedIpAddresses(SqliteConnection connection, SqliteTransaction transaction)
    {
        CanonicalizeColumn(connection, transaction, "IntrusionLog", "Id", "ClientIP");
        CanonicalizeColumn(connection, transaction, "Locks", "LockId", "IpAddress");
        CanonicalizeColumn(connection, transaction, "Locks", "Id", "IpAddress");
        CanonicalizeColumn(connection, transaction, "SecurityObservationEvents", "Id", "NormalizedIpAddress");
        CanonicalizeColumn(connection, transaction, "ProtectionEventInbox", "Id", "IpAddress");
    }

    private static void CanonicalizeColumn(SqliteConnection connection, SqliteTransaction transaction, string table, string keyColumn, string addressColumn)
    {
        if (!TableExists(connection, transaction, table)
            || !ColumnExists(connection, transaction, table, keyColumn)
            || !ColumnExists(connection, transaction, table, addressColumn))
            return;

        List<(object Key, string Address)> updates = [];
        using (SqliteCommand select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = $"SELECT {keyColumn}, {addressColumn} FROM {table} WHERE {addressColumn} IS NOT NULL";
            using SqliteDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                string original = reader.GetValue(1)?.ToString() ?? string.Empty;
                if (IpAddressCanonicalizer.TryCanonicalize(original, out string canonical) && !string.Equals(original, canonical, StringComparison.Ordinal))
                    updates.Add((reader.GetValue(0), canonical));
            }
        }
        foreach ((object key, string address) in updates)
        {
            using SqliteCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = $"UPDATE {table} SET {addressColumn}=$address WHERE {keyColumn}=$key";
            update.Parameters.AddWithValue("$address", address);
            update.Parameters.AddWithValue("$key", key);
            update.ExecuteNonQuery();
        }
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
            UpdatedUtc TEXT NOT NULL,
            IsAuthenticationEvent INTEGER NOT NULL DEFAULT 0,
            AccountName TEXT NOT NULL DEFAULT '',
            AccountDomain TEXT NOT NULL DEFAULT '',
            AccountSid TEXT NOT NULL DEFAULT '',
            IsCredentialFailure INTEGER NOT NULL DEFAULT 1,
            ProviderOrChannel TEXT NOT NULL DEFAULT '',
            ComputerName TEXT NOT NULL DEFAULT '',
            SourceEventRecordId INTEGER NULL,
            ActivityId TEXT NOT NULL DEFAULT '',
            ConfidenceScore REAL NOT NULL DEFAULT 1.0,
            TargetResource TEXT NOT NULL DEFAULT '',
            ErrorCode TEXT NOT NULL DEFAULT ''
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
            AccountSid TEXT NULL,
            IsCrossSourceDuplicate INTEGER NOT NULL DEFAULT 0,
            DuplicateOfObservationId TEXT NULL,
            CorrelationProcessed INTEGER NOT NULL DEFAULT 1,
            AlertEmitted INTEGER NOT NULL DEFAULT 0
        )
        """;

    private const string CreateSecurityObservationEventsIdempotencyIndex =
        "CREATE UNIQUE INDEX IF NOT EXISTS IX_SecurityObservationEvents_IdempotencyKey ON SecurityObservationEvents(IdempotencyKey)";

    private const string CreateSecurityObservationEventsTimeIpIndex =
        "CREATE INDEX IF NOT EXISTS IX_SecurityObservationEvents_Time_Ip ON SecurityObservationEvents(EventTimeUtc, NormalizedIpAddress)";

    private const string CreateSecurityObservationEventsCorrelationIndex =
        "CREATE INDEX IF NOT EXISTS IX_SecurityObservationEvents_CorrelationGroupId ON SecurityObservationEvents(CorrelationGroupId)";

    private const string CreateSecurityObservationEventsDuplicateIndex =
        "CREATE INDEX IF NOT EXISTS IX_SecurityObservationEvents_DuplicateOfObservationId ON SecurityObservationEvents(DuplicateOfObservationId)";

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
