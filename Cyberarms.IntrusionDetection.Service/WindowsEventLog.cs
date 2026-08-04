using System.Diagnostics;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class WindowsEventLog : IWindowsEventLog
{
    private readonly EventLog eventLog = new(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME, ".", Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);

    /// <summary>
    /// Writes one entry to the Cyberarms Windows Event Log.
    /// </summary>
    /// <param name="text">The event message.</param>
    /// <param name="type">The event severity.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="category">The event category.</param>
    public void WriteEntry(string text, EventLogEntryType type, int eventId, short category) => eventLog.WriteEntry(text, type, eventId, category);

    /// <summary>
    /// Releases the Windows Event Log handle.
    /// </summary>
    public void Dispose() => eventLog.Dispose();
}
