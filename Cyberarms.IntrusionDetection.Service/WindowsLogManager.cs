using System;
using System.Diagnostics;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class WindowsLogManager : IDisposable
{
    // public override event AttackDetectedHandler AttackDetected;

    private EventLog? eventLogCyberarms;


    private static WindowsLogManager? _instance;
    internal static WindowsLogManager Instance
    {
        get
        {
            _instance ??= new WindowsLogManager
            { };
            return _instance;
        }
    }


    /// <summary>
    /// Writes entry.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="eventId">The event id value.</param>
    /// <param name="category">The category value.</param>

    internal void WriteEntry(string text, EventLogEntryType type, int eventId, short category)
    {
        //if (!EventLog.Exists(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME) || !EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE)) {
        //    // did somebody delete the eventlog with event viewer?
        //    if (!EventLog.Exists(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME) && EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE)) {
        //        // delete the source first
        //        EventLog.DeleteEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
        //    }
        //    EventLog.CreateEventSource(new EventSourceCreationData(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE, Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME));
        //}
        eventLogCyberarms ??= new EventLog(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME, ".", Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);

        eventLogCyberarms.WriteEntry(text, type, eventId, category);
    }



    /// <summary>
    /// Initializes a logger whose lifetime is managed by the Host container.
    /// </summary>
    public WindowsLogManager()
    {

    }

    /// <summary>
    /// Releases the cached Windows Event Log handle.
    /// </summary>
    public void Dispose()
    {
        eventLogCyberarms?.Dispose();
        eventLogCyberarms = null;
    }



}
