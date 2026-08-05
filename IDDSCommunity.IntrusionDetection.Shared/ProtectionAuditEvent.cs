using System;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// Represents one persisted protection-control action or failure for operational review.
/// </summary>
public sealed class ProtectionAuditEvent
{
    /// <summary>
    /// Gets the database sequence number.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Gets the UTC occurrence time.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; init; }

    /// <summary>
    /// Gets the stable machine-readable event type.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the stable outcome code, such as Succeeded or Failed.
    /// </summary>
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    /// Gets the service or user identity that initiated the action.
    /// </summary>
    public string Actor { get; init; } = string.Empty;

    /// <summary>
    /// Gets the protected resource or address affected by the action.
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// Gets non-sensitive diagnostic details.
    /// </summary>
    public string Details { get; init; } = string.Empty;
}
