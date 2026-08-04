using System;
using System.Diagnostics;

namespace Cyberarms.IntrusionDetection.Service;

internal interface IWindowsEventLog : IDisposable
{
    /// <summary>
    /// Writes one entry to the configured Windows Event Log source.
    /// </summary>
    /// <param name="text">The event message.</param>
    /// <param name="type">The event severity.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="category">The event category.</param>
    void WriteEntry(string text, EventLogEntryType type, int eventId, short category);
}
