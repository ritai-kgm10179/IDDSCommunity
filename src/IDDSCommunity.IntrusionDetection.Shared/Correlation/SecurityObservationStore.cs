using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

/// <summary>
/// 提供安全性觀察事件、確定性冪等去重狀態、來源水位點與告警 Outbox 狀態機之 SQLite 交易持久化儲存服務。
/// </summary>
public static class SecurityObservationStore
{
    private const int MaximumFieldLength = 1024;

    /// <summary>
    /// 以單一 SQLite 資料庫交易原子化地持久化觀察事件並更新來源代理之水位點。若相同冪等鍵之事件已存在，則傳回重播重複識別。
    /// </summary>
    /// <param name="observation">正規化安全性觀察事件。</param>
    /// <param name="database">應用程式資料庫執行個體。</param>
    /// <returns>包含是否為重播事件及是否已發送過告警之結果元組。</returns>
    public static (bool IsDuplicate, bool AlreadyAlerted) PersistObservationAndWatermark(
        SecurityObservationEvent observation,
        Database database)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(database);

        string idempotencyKey = observation.ComputeIdempotencyKey();
        string nowUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        string eventTimeUtc = observation.EventTimeUtc.ToString("O", CultureInfo.InvariantCulture);
        string receivedTimeUtc = observation.ReceivedTimeUtc.ToString("O", CultureInfo.InvariantCulture);

        SqliteConnection conn = database.Connection;
        using SqliteTransaction tx = conn.BeginTransaction();
        try
        {
            // 檢查資料庫是否已存在該確定性冪等鍵
            using (SqliteCommand checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = "SELECT AlertEmitted FROM SecurityObservationEvents WHERE IdempotencyKey = $key";
                checkCmd.Parameters.AddWithValue("$key", idempotencyKey);
                object? existingAlertEmitted = checkCmd.ExecuteScalar();

                if (existingAlertEmitted != null && existingAlertEmitted != DBNull.Value)
                {
                    tx.Commit();
                    bool alerted = Convert.ToInt32(existingAlertEmitted, CultureInfo.InvariantCulture) == 1;
                    return (IsDuplicate: true, AlreadyAlerted: alerted);
                }
            }

            // 寫入新觀察事件
            using (SqliteCommand insertCmd = conn.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = """
                    INSERT INTO SecurityObservationEvents
                        (Id, IdempotencyKey, ReceivedUtc, EventTimeUtc, SourceAgentName, ProviderOrChannel,
                         ComputerName, SourceEventRecordId, SourceFileOffset, SourceEventIdentity,
                         NormalizedIpAddress, NormalizedAccount, NormalizedDomain, OriginalEventReference,
                         Provenance, LogonType, SubStatus, CorrelationGroupId, ConfidenceScore, AlertEmitted)
                    VALUES
                        ($id, $idemp, $recv, $evtTime, $srcAgent, $provider,
                         $computer, $recId, $offset, $identity,
                         $ip, $account, $domain, $origRef,
                         $provenance, $logonType, $subStatus, $corrId, $score, 0)
                    """;
                insertCmd.Parameters.AddWithValue("$id", observation.Id.ToString("D"));
                insertCmd.Parameters.AddWithValue("$idemp", idempotencyKey);
                insertCmd.Parameters.AddWithValue("$recv", receivedTimeUtc);
                insertCmd.Parameters.AddWithValue("$evtTime", eventTimeUtc);
                insertCmd.Parameters.AddWithValue("$srcAgent", observation.SourceAgentName);
                insertCmd.Parameters.AddWithValue("$provider", observation.ProviderOrChannel);
                insertCmd.Parameters.AddWithValue("$computer", observation.ComputerName);
                insertCmd.Parameters.AddWithValue("$recId", observation.SourceEventRecordId.HasValue ? (object)observation.SourceEventRecordId.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("$offset", observation.SourceFileOffset.HasValue ? (object)observation.SourceFileOffset.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("$identity", observation.SourceEventIdentity ?? string.Empty);
                insertCmd.Parameters.AddWithValue("$ip", observation.NormalizedIpAddress);
                insertCmd.Parameters.AddWithValue("$account", observation.NormalizedAccount);
                insertCmd.Parameters.AddWithValue("$domain", observation.NormalizedDomain);
                insertCmd.Parameters.AddWithValue("$origRef", observation.OriginalEventReference);
                insertCmd.Parameters.AddWithValue("$provenance", observation.Provenance);
                insertCmd.Parameters.AddWithValue("$logonType", observation.LogonType.HasValue ? (object)observation.LogonType.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("$subStatus", observation.SubStatus ?? string.Empty);
                insertCmd.Parameters.AddWithValue("$corrId", observation.CorrelationGroupId.HasValue ? (object)observation.CorrelationGroupId.Value.ToString("D") : DBNull.Value);
                insertCmd.Parameters.AddWithValue("$score", observation.ConfidenceScore);
                insertCmd.ExecuteNonQuery();
            }

            // 單調更新來源水位點
            using (SqliteCommand watermarkCmd = conn.CreateCommand())
            {
                watermarkCmd.Transaction = tx;
                watermarkCmd.CommandText = """
                    INSERT INTO ObservationWatermarks
                        (SourceAgentName, ProviderOrChannel, LastEventRecordId, LastTimestampUtc, UpdatedUtc)
                    VALUES
                        ($srcAgent, $provider, $recId, $lastTime, $updated)
                    ON CONFLICT(SourceAgentName, ProviderOrChannel) DO UPDATE SET
                        LastEventRecordId = MAX(COALESCE(ObservationWatermarks.LastEventRecordId, 0), COALESCE(excluded.LastEventRecordId, 0)),
                        LastTimestampUtc = CASE WHEN excluded.LastTimestampUtc > ObservationWatermarks.LastTimestampUtc THEN excluded.LastTimestampUtc ELSE ObservationWatermarks.LastTimestampUtc END,
                        UpdatedUtc = excluded.UpdatedUtc
                    """;
                watermarkCmd.Parameters.AddWithValue("$srcAgent", observation.SourceAgentName);
                watermarkCmd.Parameters.AddWithValue("$provider", observation.ProviderOrChannel);
                watermarkCmd.Parameters.AddWithValue("$recId", observation.SourceEventRecordId.HasValue ? (object)observation.SourceEventRecordId.Value : DBNull.Value);
                watermarkCmd.Parameters.AddWithValue("$lastTime", eventTimeUtc);
                watermarkCmd.Parameters.AddWithValue("$updated", nowUtc);
                watermarkCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return (IsDuplicate: false, AlreadyAlerted: false);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 計算密碼噴灑攻擊之確定性唯一 AlertId。使用規則版本、攻擊型態、正規化主體與首次達門檻所需且已排序之觀察事件冪等鍵集合計算 SHA-256。
    /// </summary>
    /// <param name="sprayType">噴灑攻擊型態。</param>
    /// <param name="normalizedSubject">正規化目標 IP 或帳號主體。</param>
    /// <param name="contributingIdempotencyKeys">貢獻達成門檻之觀察事件確定性冪等鍵集合。</param>
    /// <returns>確定性 AlertId 字串。</returns>
    public static string ComputeAlertId(
        SprayAttackType sprayType,
        string normalizedSubject,
        IEnumerable<string>? contributingIdempotencyKeys = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedSubject);
        List<string> keys = contributingIdempotencyKeys != null
            ? contributingIdempotencyKeys.Where(k => !string.IsNullOrWhiteSpace(k)).OrderBy(k => k, StringComparer.Ordinal).ToList()
            : [];

        string joinedKeys = string.Join(";", keys);
        string raw = $"RULE_V1:{sprayType}:{normalizedSubject.Trim().ToUpperInvariant()}:{joinedKeys}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 以單一 SQLite 資料庫交易原子化地將噴灑告警寫入持久化 Outbox 佇列，並標記觀察事件之告警狀態。
    /// </summary>
    /// <param name="alertId">確定性告警識別碼。</param>
    /// <param name="observationId">關聯之觀察事件識別碼。</param>
    /// <param name="occurredUtc">事件發生時間。</param>
    /// <param name="eventType">事件型別。</param>
    /// <param name="outcome">結果代碼。</param>
    /// <param name="actor">發起動作者。</param>
    /// <param name="subject">主體目標。</param>
    /// <param name="details">診斷詳細訊息。</param>
    /// <param name="database">應用程式資料庫執行個體。</param>
    /// <returns>若成功將新告警寫入 Outbox 傳回 <see langword="true"/>；若相同 AlertId 已存在則傳回 <see langword="false"/>。</returns>
    public static bool EnqueueAlertOutbox(
        string alertId,
        Guid observationId,
        DateTimeOffset occurredUtc,
        string eventType,
        string outcome,
        string actor,
        string subject,
        string details,
        Database database)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alertId);
        ArgumentNullException.ThrowIfNull(database);

        string nowUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        string occurredStr = occurredUtc.ToString("O", CultureInfo.InvariantCulture);

        SqliteConnection conn = database.Connection;
        using SqliteTransaction tx = conn.BeginTransaction();
        try
        {
            using (SqliteCommand checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = tx;
                checkCmd.CommandText = "SELECT Status FROM ObservationAlertOutbox WHERE AlertId = $alertId";
                checkCmd.Parameters.AddWithValue("$alertId", alertId);
                object? existing = checkCmd.ExecuteScalar();

                if (existing != null && existing != DBNull.Value)
                {
                    tx.Commit();
                    return false;
                }
            }

            using (SqliteCommand insertCmd = conn.CreateCommand())
            {
                insertCmd.Transaction = tx;
                insertCmd.CommandText = """
                    INSERT INTO ObservationAlertOutbox
                        (AlertId, ObservationId, OccurredUtc, EventType, Outcome, Actor, Subject, Details, Status, DispatchedUtc, CreatedUtc)
                    VALUES
                        ($alertId, $obsId, $occurred, $evtType, $outcome, $actor, $subject, $details, 0, NULL, $created)
                    """;
                insertCmd.Parameters.AddWithValue("$alertId", alertId);
                insertCmd.Parameters.AddWithValue("$obsId", observationId.ToString("D"));
                insertCmd.Parameters.AddWithValue("$occurred", occurredStr);
                insertCmd.Parameters.AddWithValue("$evtType", eventType);
                insertCmd.Parameters.AddWithValue("$outcome", outcome);
                insertCmd.Parameters.AddWithValue("$actor", actor);
                insertCmd.Parameters.AddWithValue("$subject", subject);
                insertCmd.Parameters.AddWithValue("$details", details);
                insertCmd.Parameters.AddWithValue("$created", nowUtc);
                insertCmd.ExecuteNonQuery();
            }

            using (SqliteCommand updateCmd = conn.CreateCommand())
            {
                updateCmd.Transaction = tx;
                updateCmd.CommandText = "UPDATE SecurityObservationEvents SET AlertEmitted = 1 WHERE Id = $obsId";
                updateCmd.Parameters.AddWithValue("$obsId", observationId.ToString("D"));
                updateCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 依序分派 Outbox 佇列中待處理的告警至稽核日誌，並以單一交易原子更新 Outbox 狀態為已分派。
    /// </summary>
    /// <param name="database">應用程式資料庫執行個體。</param>
    /// <param name="auditTrail">稽核追蹤服務執行個體（選擇性）。</param>
    /// <param name="afterAuditInsertCallback">用於測試注入審計寫入後、狀態更新前中斷之回呼函式。</param>
    /// <returns>實際成功分派之告警數量。</returns>
    public static int DispatchPendingAlerts(
        Database database,
        ProtectionAuditTrail? auditTrail = null,
        Action? afterAuditInsertCallback = null)
    {
        ArgumentNullException.ThrowIfNull(database);

        SqliteConnection conn = database.Connection;
        List<AlertOutboxRow> pending = [];

        using (SqliteCommand selectCmd = conn.CreateCommand())
        {
            selectCmd.CommandText = "SELECT AlertId, ObservationId, OccurredUtc, EventType, Outcome, Actor, Subject, Details FROM ObservationAlertOutbox WHERE Status = 0 ORDER BY CreatedUtc, AlertId";
            using SqliteDataReader reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                pending.Add(new AlertOutboxRow
                {
                    AlertId = reader.GetString(0),
                    ObservationId = reader.GetString(1),
                    OccurredUtc = reader.GetString(2),
                    EventType = reader.GetString(3),
                    Outcome = reader.GetString(4),
                    Actor = reader.GetString(5),
                    Subject = reader.GetString(6),
                    Details = reader.GetString(7)
                });
            }
        }

        int dispatchedCount = 0;
        string nowUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        foreach (AlertOutboxRow row in pending)
        {
            using SqliteTransaction tx = conn.BeginTransaction();
            try
            {
                using (SqliteCommand auditCmd = conn.CreateCommand())
                {
                    auditCmd.Transaction = tx;
                    auditCmd.CommandText = """
                        INSERT INTO ProtectionAuditLog (AlertId, OccurredUtc, EventType, Outcome, Actor, Subject, Details)
                        VALUES ($alertId, $occurred, $evtType, $outcome, $actor, $subject, $details)
                        ON CONFLICT(AlertId) DO NOTHING
                        """;
                    auditCmd.Parameters.AddWithValue("$alertId", row.AlertId);
                    auditCmd.Parameters.AddWithValue("$occurred", row.OccurredUtc);
                    auditCmd.Parameters.AddWithValue("$evtType", Limit(row.EventType));
                    auditCmd.Parameters.AddWithValue("$outcome", Limit(row.Outcome));
                    auditCmd.Parameters.AddWithValue("$actor", Limit(row.Actor));
                    auditCmd.Parameters.AddWithValue("$subject", Limit(row.Subject));
                    auditCmd.Parameters.AddWithValue("$details", Limit(row.Details));
                    auditCmd.ExecuteNonQuery();
                }

                afterAuditInsertCallback?.Invoke();

                using (SqliteCommand updateCmd = conn.CreateCommand())
                {
                    updateCmd.Transaction = tx;
                    updateCmd.CommandText = "UPDATE ObservationAlertOutbox SET Status = 1, DispatchedUtc = $dispatched WHERE AlertId = $alertId AND Status = 0";
                    updateCmd.Parameters.AddWithValue("$dispatched", nowUtc);
                    updateCmd.Parameters.AddWithValue("$alertId", row.AlertId);
                    int affected = updateCmd.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        dispatchedCount++;
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        return dispatchedCount;
    }

    /// <summary>
    /// 從資料庫讀取所有來源代理與通道之持久化水位點記錄。
    /// </summary>
    /// <param name="database">應用程式資料庫執行個體。</param>
    /// <returns>水位點記錄字典，以「來源代理|通道」為鍵。</returns>
    public static Dictionary<string, ObservationWatermark> LoadWatermarks(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        Dictionary<string, ObservationWatermark> result = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<ObservationWatermarkRow> rows = database.Query<ObservationWatermarkRow>(
            "SELECT SourceAgentName, ProviderOrChannel, LastEventRecordId, LastTimestampUtc, UpdatedUtc FROM ObservationWatermarks");

        foreach (ObservationWatermarkRow row in rows)
        {
            DateTimeOffset lastTimestamp = DateTimeOffset.TryParse(row.LastTimestampUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed)
                ? parsed
                : DateTimeOffset.MinValue;

            ObservationWatermark watermark = new()
            {
                SourceAgentName = row.SourceAgentName,
                ProviderOrChannel = row.ProviderOrChannel,
                LastEventRecordId = row.LastEventRecordId,
                LastTimestampUtc = lastTimestamp
            };

            result[watermark.Key] = watermark;
        }

        return result;
    }

    private static string Limit(string value) => value.Length <= MaximumFieldLength ? value : value[..MaximumFieldLength];

    private sealed class AlertOutboxRow
    {
                /// <summary>
        /// 取得或設定 AlertId。
        /// </summary>
public string AlertId { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 ObservationId。
        /// </summary>
public string ObservationId { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 OccurredUtc。
        /// </summary>
public string OccurredUtc { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 EventType。
        /// </summary>
public string EventType { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 Outcome。
        /// </summary>
public string Outcome { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 Actor。
        /// </summary>
public string Actor { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 Subject。
        /// </summary>
public string Subject { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 Details。
        /// </summary>
public string Details { get; set; } = string.Empty;
    }

    private sealed class ObservationWatermarkRow
    {
                /// <summary>
        /// 取得或設定 SourceAgentName。
        /// </summary>
public string SourceAgentName { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 ProviderOrChannel。
        /// </summary>
public string ProviderOrChannel { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 LastEventRecordId。
        /// </summary>
public long? LastEventRecordId { get; set; }
                /// <summary>
        /// 取得或設定 LastTimestampUtc。
        /// </summary>
public string LastTimestampUtc { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 UpdatedUtc。
        /// </summary>
public string UpdatedUtc { get; set; } = string.Empty;
    }
}
