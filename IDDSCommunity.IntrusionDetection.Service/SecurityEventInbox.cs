using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// Persists accepted protection events so an interrupted runtime can replay unfinished work.
/// </summary>
internal sealed class SecurityEventInbox(Database database, TimeProvider timeProvider)
{
    private const int PendingStatus = 0;
    private const int ProcessingStatus = 1;
    private const int CompletedStatus = 2;
    private const int FailedStatus = 3;

    /// <summary>
    /// Persists a new event before it enters the in-memory channel.
    /// </summary>
    /// <param name="agentName">The stable Agent name used to resolve a plug-in after restart.</param>
    /// <param name="eventArgs">The copied detection event.</param>
    /// <returns>The durable event identifier.</returns>
    internal Guid Add(string agentName, INotificationEventArgs eventArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(eventArgs);
        Guid id = Guid.NewGuid();
        string now = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        database.ExecuteNonQuery(
            """
            INSERT INTO ProtectionEventInbox
                (Id, ReceivedUtc, AgentName, CreateDate, EventId, IpAddress, EventMessage, Status, Attempts, LastError, UpdatedUtc)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, 0, '', @p1)
            """,
            id.ToString("D"), now, agentName, eventArgs.CreateDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            eventArgs.EventId, eventArgs.IpAddress, eventArgs.EventMessage, PendingStatus);
        return id;
    }

    /// <summary>
    /// Returns unfinished events in original receipt order and resets interrupted processing rows for replay.
    /// </summary>
    /// <param name="maximumCount">The maximum number of events to recover.</param>
    /// <returns>The durable events to replay.</returns>
    internal IReadOnlyList<SecurityEventInboxItem> ReadPending(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);
        return database.Query<SecurityEventInboxRow>(
            """
            SELECT Id, ReceivedUtc, AgentName, CreateDate, EventId, IpAddress, EventMessage
            FROM ProtectionEventInbox
            WHERE Status IN (@Pending, @Processing, @Failed)
            ORDER BY ReceivedUtc, Id
            LIMIT @MaximumCount
            """,
            new { Pending = PendingStatus, Processing = ProcessingStatus, Failed = FailedStatus, MaximumCount = maximumCount })
            .Select(static row => row.ToItem())
            .ToList();
    }

    /// <summary>
    /// Marks one event as actively processing and increments its attempt count.
    /// </summary>
    /// <param name="id">The durable event identifier.</param>
    internal void MarkProcessing(Guid id) => Update(id, ProcessingStatus, incrementAttempts: true, string.Empty);

    /// <summary>
    /// Marks one event as successfully processed.
    /// </summary>
    /// <param name="id">The durable event identifier.</param>
    internal void MarkCompleted(Guid id) => Update(id, CompletedStatus, incrementAttempts: false, string.Empty);

    /// <summary>
    /// Marks one event as failed while retaining it for a later replay.
    /// </summary>
    /// <param name="id">The durable event identifier.</param>
    /// <param name="exception">The processing failure.</param>
    internal void MarkFailed(Guid id, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Update(id, FailedStatus, incrementAttempts: false, exception.GetType().Name);
    }

    /// <summary>
    /// Removes completed inbox rows older than the configured evidence retention period.
    /// </summary>
    /// <param name="retentionPeriod">The minimum completed-row age to retain.</param>
    internal void PurgeCompleted(TimeSpan retentionPeriod)
    {
        if (retentionPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod));
        string boundary = timeProvider.GetUtcNow().Subtract(retentionPeriod).ToString("O", CultureInfo.InvariantCulture);
        database.ExecuteNonQuery("DELETE FROM ProtectionEventInbox WHERE Status=@p0 AND UpdatedUtc<@p1", CompletedStatus, boundary);
    }

    /// <summary>
    /// Counts durable events that have not completed successfully.
    /// </summary>
    /// <returns>The unfinished event count.</returns>
    internal long CountUnfinished() => Convert.ToInt64(
        database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionEventInbox WHERE Status<>@p0", CompletedStatus),
        CultureInfo.InvariantCulture);

    /// <summary>
    /// Updates the processing state and diagnostic details for one durable event.
    /// </summary>
    /// <param name="id">The durable event identifier.</param>
    /// <param name="status">The persisted processing status.</param>
    /// <param name="incrementAttempts">Whether to increment the processing-attempt counter.</param>
    /// <param name="error">The non-sensitive failure category, or an empty string after success.</param>
    private void Update(Guid id, int status, bool incrementAttempts, string error)
    {
        string attempts = incrementAttempts ? "Attempts=Attempts+1," : string.Empty;
        database.ExecuteNonQuery(
            $"UPDATE ProtectionEventInbox SET Status=@p0, {attempts} LastError=@p1, UpdatedUtc=@p2 WHERE Id=@p3",
            status, error, timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture), id.ToString("D"));
    }

    private sealed class SecurityEventInboxRow
    {
        public string Id { get; init; } = string.Empty;
        public string ReceivedUtc { get; init; } = string.Empty;
        public string AgentName { get; init; } = string.Empty;
        public string CreateDate { get; init; } = string.Empty;
        public int EventId { get; init; }
        public string IpAddress { get; init; } = string.Empty;
        public string EventMessage { get; init; } = string.Empty;

        internal SecurityEventInboxItem ToItem() => new(
            Guid.Parse(Id),
            DateTimeOffset.Parse(ReceivedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            AgentName,
            new NotificationEventArgs
            {
                CreateDate = DateTime.Parse(CreateDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                EventId = EventId,
                IpAddress = IpAddress,
                EventMessage = EventMessage
            });
    }
}

internal sealed record SecurityEventInboxItem(Guid Id, DateTimeOffset ReceivedUtc, string AgentName, INotificationEventArgs EventArgs);
