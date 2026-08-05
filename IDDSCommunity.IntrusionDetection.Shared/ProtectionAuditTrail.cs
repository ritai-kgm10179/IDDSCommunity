using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// Persists and exports bounded, non-sensitive evidence for protection-control operation.
/// </summary>
public sealed class ProtectionAuditTrail(Database database, TimeProvider timeProvider)
{
    private const int MaximumFieldLength = 1024;
    private const int MaximumExportRecords = 10000;

    /// <summary>
    /// Records one protection event using parameterized storage.
    /// </summary>
    /// <param name="eventType">The stable machine-readable event type.</param>
    /// <param name="outcome">The stable outcome code.</param>
    /// <param name="actor">The service or user identity that initiated the action.</param>
    /// <param name="subject">The protected resource or address affected by the action.</param>
    /// <param name="details">Optional non-sensitive diagnostic details.</param>
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
    /// Reads a bounded audit window in deterministic chronological order.
    /// </summary>
    /// <param name="fromUtc">The inclusive UTC lower bound.</param>
    /// <param name="toUtc">The exclusive UTC upper bound.</param>
    /// <param name="maximumRecords">The maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The materialized audit records.</returns>
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
    /// Exports a bounded audit window as UTF-8 JSON for an external evidence repository or SIEM.
    /// </summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="fromUtc">The inclusive UTC lower bound.</param>
    /// <param name="toUtc">The exclusive UTC upper bound.</param>
    /// <param name="cancellationToken">Cancels querying or serialization.</param>
    /// <returns>A task that completes after the JSON document is written.</returns>
    public async Task ExportJsonAsync(Stream destination, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException(Localization.Strings.Get("The audit export destination must be writable."), nameof(destination));
        IReadOnlyList<ProtectionAuditEvent> records = await ReadAsync(fromUtc, toUtc, cancellationToken: cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(destination, records, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes audit evidence older than the approved retention boundary.
    /// </summary>
    /// <param name="retentionPeriod">The duration for which evidence must be retained.</param>
    /// <param name="cancellationToken">Cancels the database command.</param>
    /// <returns>The number of deleted records.</returns>
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
