using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 持久化並匯出保護控制作業所需界限內且非機密的證據。
/// </summary>
public sealed class ProtectionAuditTrail(Database database, TimeProvider timeProvider)
{
    private const int MaximumFieldLength = 1024;
    private const int MaximumExportRecords = 10000;

    /// <summary>
    /// 使用參數化儲存紀錄一個保護事件。
    /// </summary>
    /// <param name="eventType">穩定的機器可讀事件型別。</param>
    /// <param name="outcome">穩定的結果代碼。</param>
    /// <param name="actor">發起動作的服務或使用者識別碼。</param>
    /// <param name="subject">受動作影響的受保護資源或位址。</param>
    /// <param name="details">選擇性的非機密診斷詳細資訊。</param>
    public void Record(string eventType, string outcome, string actor, string subject, string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        database.ExecuteNonQuery(
            "INSERT INTO ProtectionAuditLog(OccurredUtc, EventType, Outcome, Actor, Subject, Details) VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
            timeProvider.GetUtcNow().ToString("O"),
            Limit(eventType),
            Limit(outcome),
            Limit(actor),
            Limit(subject),
            Limit(details ?? string.Empty));
    }

    /// <summary>
    /// 依確定性時間順序讀取界限內的稽核視窗。
    /// </summary>
    /// <param name="fromUtc">包含在內的 UTC 下限時間。</param>
    /// <param name="toUtc">不包含在內的 UTC 上限時間。</param>
    /// <param name="maximumRecords">傳回記錄的最大數量。</param>
    /// <param name="cancellationToken">取消資料庫查詢。</param>
    /// <returns>傳回實體化的稽核紀錄。</returns>
    public async Task<IReadOnlyList<ProtectionAuditEvent>> ReadAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int maximumRecords = MaximumExportRecords,
        CancellationToken cancellationToken = default)
    {
        if (toUtc <= fromUtc)
            throw new ArgumentOutOfRangeException(nameof(toUtc));
        if (maximumRecords is < 1 or > MaximumExportRecords)
            throw new ArgumentOutOfRangeException(nameof(maximumRecords));
        IEnumerable<ProtectionAuditRow> rows = await database.QueryAsync<ProtectionAuditRow>(
            "SELECT Id, OccurredUtc, EventType, Outcome, Actor, Subject, Details FROM ProtectionAuditLog WHERE OccurredUtc >= @FromUtc AND OccurredUtc < @ToUtc ORDER BY OccurredUtc, Id LIMIT @MaximumRecords",
            new { FromUtc = fromUtc.ToString("O"), ToUtc = toUtc.ToString("O"), MaximumRecords = maximumRecords },
            cancellationToken).ConfigureAwait(false);
        return rows.Select(row => new ProtectionAuditEvent
        {
            Id = row.Id,
            OccurredUtc = DateTimeOffset.Parse(row.OccurredUtc, System.Globalization.CultureInfo.InvariantCulture),
            EventType = row.EventType,
            Outcome = row.Outcome,
            Actor = row.Actor,
            Subject = row.Subject,
            Details = row.Details
        }).ToList();
    }

    /// <summary>
    /// 將界限內的稽核視窗匯出為 UTF-8 JSON，供外部證據儲存庫或 SIEM 使用。
    /// </summary>
    /// <param name="destination">可寫入的目標串流。</param>
    /// <param name="fromUtc">包含在內的 UTC 下限時間。</param>
    /// <param name="toUtc">不包含在內的 UTC 上限時間。</param>
    /// <param name="cancellationToken">取消查詢或序列化作業。</param>
    /// <returns>傳回待 JSON 文件寫入完成後結束的 Task。</returns>
    public async Task ExportJsonAsync(Stream destination, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException(Localization.Strings.Get("The audit export destination must be writable."), nameof(destination));
        IReadOnlyList<ProtectionAuditEvent> records = await ReadAsync(fromUtc, toUtc, cancellationToken: cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(destination, records, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 刪除早於核可保留界限的稽核證據。
    /// </summary>
    /// <param name="retentionPeriod">必須保留證據的持續時間。</param>
    /// <param name="cancellationToken">取消資料庫命令。</param>
    /// <returns>傳回刪除的紀錄數量。</returns>
    public async Task<int> PurgeOlderThanAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        if (retentionPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod));
        DateTimeOffset boundary = timeProvider.GetUtcNow() - retentionPeriod;
        return await database.ExecuteNonQueryAsync(
            "DELETE FROM ProtectionAuditLog WHERE OccurredUtc < @Boundary",
            new { Boundary = boundary.ToString("O") },
            cancellationToken).ConfigureAwait(false);
    }

    private static string Limit(string value) => value.Length <= MaximumFieldLength ? value : value[..MaximumFieldLength];

    private sealed class ProtectionAuditRow
    {
        public long Id { get; init; }
        public string OccurredUtc { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public string Outcome { get; init; } = string.Empty;
        public string Actor { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
    }
}
