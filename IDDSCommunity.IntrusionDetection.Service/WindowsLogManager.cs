using System;
using System.Diagnostics;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service;

internal sealed class WindowsLogManager : IRuntimeLog
{
    // public override event AttackDetectedHandler AttackDetected;

    private readonly IWindowsEventLog eventLog;


    private static WindowsLogManager? _instance;
    internal static WindowsLogManager Instance
    {
        get
        {
            _instance ??= new WindowsLogManager(new WindowsEventLog());
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

    public void WriteEntry(string text, EventLogEntryType type, int eventId, short category)
    {
        //if (!EventLog.Exists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME) || !EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE)) {
        //    // did somebody delete the eventlog with event viewer?
        //    if (!EventLog.Exists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME) && EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE)) {
        //        // delete the source first
        //        EventLog.DeleteEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE);
        //    }
        //    EventLog.CreateEventSource(new EventSourceCreationData(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE, Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME));
        //}
        eventLog.WriteEntry(LogSanitizer.Sanitize(text), type, eventId, category);
    }



    /// <summary>
    /// Initializes a logger whose lifetime is managed by the Host container.
    /// </summary>
    public WindowsLogManager(IWindowsEventLog eventLog)
    {
        ArgumentNullException.ThrowIfNull(eventLog);
        this.eventLog = eventLog;
    }



}
