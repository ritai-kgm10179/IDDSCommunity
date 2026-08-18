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
    private const int DeadLetterStatus = 4;
    private const int MaximumProcessingAttempts = 5;
    /// <summary>
    /// Persists a new event before it enters the in-memory channel.
    /// </summary>
    /// <param name="agentName">The stable Agent name used to resolve a plug-in after restart.</param>
    /// <param name="eventArgs">The copied detection event.</param>
    /// <returns>傳回 durable event identifier 的結果。</returns>
    internal Guid Add(string agentName, INotificationEventArgs eventArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(eventArgs);
        Guid id = Guid.NewGuid();
        string now = timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        database.ExecuteNonQuery(
            """
            INSERT INTO ProtectionEventInbox
                (Id, ReceivedUtc, AgentName, CreateDate, EventId, IpAddress, EventMessage, Status, Attempts, LastError, UpdatedUtc,
                 IsAuthenticationEvent, AccountName, AccountDomain, AccountSid, IsCredentialFailure, ProviderOrChannel,
                 ComputerName, SourceEventRecordId, ActivityId, ConfidenceScore, TargetResource, ErrorCode)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, 0, '', @p1,
                    @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19)
            """,
            id.ToString("D"), now, agentName, eventArgs.CreateDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            eventArgs.EventId, eventArgs.IpAddress, eventArgs.EventMessage, PendingStatus,
            eventArgs is AuthenticationNotificationEventArgs ? 1 : 0,
            eventArgs is AuthenticationNotificationEventArgs authentication ? authentication.AccountName : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationDomain ? authenticationDomain.AccountDomain : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationSid ? authenticationSid.AccountSid ?? string.Empty : string.Empty,
            eventArgs is not AuthenticationNotificationEventArgs authenticationFailure || authenticationFailure.IsCredentialFailure ? 1 : 0,
            eventArgs is AuthenticationNotificationEventArgs authenticationProvider ? authenticationProvider.ProviderOrChannel : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationComputer ? authenticationComputer.ComputerName : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationRecord && authenticationRecord.SourceEventRecordId.HasValue
                ? authenticationRecord.SourceEventRecordId.Value
                : DBNull.Value,
            eventArgs is AuthenticationNotificationEventArgs authenticationActivity ? authenticationActivity.ActivityId ?? string.Empty : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationConfidence ? authenticationConfidence.ConfidenceScore : 1.0,
            eventArgs is AuthenticationNotificationEventArgs authenticationTarget ? authenticationTarget.TargetResource ?? string.Empty : string.Empty,
            eventArgs is AuthenticationNotificationEventArgs authenticationError ? authenticationError.ErrorCode ?? string.Empty : string.Empty);
        return id;
    }
    /// <summary>
    /// Returns unfinished events in original receipt order and resets interrupted processing rows for replay.
    /// </summary>
    /// <param name="maximumCount">The maximum number of events to recover.</param>
    /// <returns>傳回 durable events to replay 的結果。</returns>
    internal IReadOnlyList<SecurityEventInboxItem> ReadPending(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);
        return database.Query<SecurityEventInboxRow>(
            """
            SELECT Id, ReceivedUtc, AgentName, CreateDate, EventId, IpAddress, EventMessage,
                   IsAuthenticationEvent, AccountName, AccountDomain, AccountSid, IsCredentialFailure,
                   ProviderOrChannel, ComputerName, SourceEventRecordId, ActivityId, ConfidenceScore,
                   TargetResource, ErrorCode
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
        database.ExecuteNonQuery(
            "UPDATE ProtectionEventInbox SET Status=CASE WHEN Attempts>=@p0 THEN @p1 ELSE @p2 END, LastError=@p3, UpdatedUtc=@p4 WHERE Id=@p5",
            MaximumProcessingAttempts,
            DeadLetterStatus,
            FailedStatus,
            exception.GetType().Name,
            timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture),
            id.ToString("D"));
    }
    /// <summary>
    /// 移除尚未交付至記憶體佇列的待處理事件。
    /// </summary>
    /// <param name="id">持久化事件識別碼。</param>
    internal void RemovePending(Guid id) =>
        database.ExecuteNonQuery("DELETE FROM ProtectionEventInbox WHERE Id=@p0 AND Status=@p1", id.ToString("D"), PendingStatus);
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
    /// <returns>傳回 unfinished event count 的結果。</returns>
    internal long CountUnfinished() => Convert.ToInt64(
        database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionEventInbox WHERE Status<>@p0", CompletedStatus),
        CultureInfo.InvariantCulture);

    /// <summary>
    /// 計算已超過處理重試上限並隔離的毒性事件數量。
    /// </summary>
    /// <returns>已進入無法自動重試狀態的事件數量。</returns>
    internal long CountDeadLettered() => Convert.ToInt64(
        database.ExecuteScalar("SELECT COUNT(*) FROM ProtectionEventInbox WHERE Status=@p0", DeadLetterStatus),
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
                /// <summary>
        /// 取得或設定 Id。
        /// </summary>
public string Id { get; init; } = string.Empty;
                /// <summary>
        /// 取得或設定 ReceivedUtc。
        /// </summary>
public string ReceivedUtc { get; init; } = string.Empty;
                /// <summary>
        /// 取得或設定 AgentName。
        /// </summary>
public string AgentName { get; init; } = string.Empty;
                /// <summary>
        /// 取得或設定 CreateDate。
        /// </summary>
public string CreateDate { get; init; } = string.Empty;
                /// <summary>
        /// 取得或設定 EventId。
        /// </summary>
public int EventId { get; init; }
                /// <summary>
        /// 取得或設定 IpAddress。
        /// </summary>
public string IpAddress { get; init; } = string.Empty;
                /// <summary>
        /// 取得或設定 EventMessage。
        /// </summary>
public string EventMessage { get; init; } = string.Empty;
        public bool IsAuthenticationEvent { get; init; }
        public string AccountName { get; init; } = string.Empty;
        public string AccountDomain { get; init; } = string.Empty;
        public string AccountSid { get; init; } = string.Empty;
        public bool IsCredentialFailure { get; init; }
        public string ProviderOrChannel { get; init; } = string.Empty;
        public string ComputerName { get; init; } = string.Empty;
        public long? SourceEventRecordId { get; init; }
        public string ActivityId { get; init; } = string.Empty;
        public double ConfidenceScore { get; init; } = 1.0;
        public string TargetResource { get; init; } = string.Empty;
        public string ErrorCode { get; init; } = string.Empty;

        internal SecurityEventInboxItem ToItem()
        {
            NotificationEventArgs eventArgs = IsAuthenticationEvent
                ? new AuthenticationNotificationEventArgs
                {
                    AccountName = AccountName,
                    AccountDomain = AccountDomain,
                    AccountSid = string.IsNullOrEmpty(AccountSid) ? null : AccountSid,
                    IsCredentialFailure = IsCredentialFailure,
                    ProviderOrChannel = ProviderOrChannel,
                    ComputerName = ComputerName,
                    SourceEventRecordId = SourceEventRecordId,
                    ActivityId = string.IsNullOrEmpty(ActivityId) ? null : ActivityId,
                    ConfidenceScore = ConfidenceScore,
                    TargetResource = string.IsNullOrEmpty(TargetResource) ? null : TargetResource,
                    ErrorCode = string.IsNullOrEmpty(ErrorCode) ? null : ErrorCode
                }
                : new NotificationEventArgs();
            eventArgs.CreateDate = DateTime.Parse(CreateDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            eventArgs.EventId = EventId;
            eventArgs.IpAddress = IpAddress;
            eventArgs.EventMessage = EventMessage;
            return new SecurityEventInboxItem(
                Guid.Parse(Id),
                DateTimeOffset.Parse(ReceivedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                AgentName,
                eventArgs);
        }
    }
}

internal sealed record SecurityEventInboxItem(Guid Id, DateTimeOffset ReceivedUtc, string AgentName, INotificationEventArgs EventArgs);
