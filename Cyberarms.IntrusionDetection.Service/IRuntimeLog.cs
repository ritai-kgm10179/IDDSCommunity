using System.Diagnostics;

namespace Cyberarms.IntrusionDetection.Service;

internal interface IRuntimeLog
{
    /// <summary>
    /// Writes a structured Windows runtime event.
    /// </summary>
    /// <param name="text">The sanitized event message.</param>
    /// <param name="type">The event severity.</param>
    /// <param name="eventId">The stable event identifier.</param>
    /// <param name="category">The event category.</param>
    void WriteEntry(string text, EventLogEntryType type, int eventId, short category);
}
