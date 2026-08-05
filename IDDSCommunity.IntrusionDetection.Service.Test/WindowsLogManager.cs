using System;
using System.Diagnostics;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

internal class WindowsLogManager
{
    // public override event AttackDetectedHandler AttackDetected;

    private EventLog? eventLogIDDSCommunity;


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
        // Delete old log
        //try {
        //    EventLog.DeleteEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE);
        //    EventLog.Delete(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME);
        //} catch { }
        //if (!EventLog.Exists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME) || !EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE)) {
        //    // did somebody delete the eventlog with event viewer?
        //    if (!EventLog.Exists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME) && EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE)) {
        //        // delete the source first
        //        EventLog.DeleteEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE);
        //    }
        //    EventLog.CreateEventSource(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE, Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME);
        //    System.Diagnostics.EventLogInstaller installer = new EventLogInstaller();

        //}
        eventLogIDDSCommunity ??= new EventLog(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME);
        eventLogIDDSCommunity.Source = Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE;
        eventLogIDDSCommunity.Log = Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME;

        EventLog.WriteEntry(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE, text, type, eventId, category);
        eventLogIDDSCommunity.WriteEntry(text, type, eventId, category);
    }




    /// <summary>
    /// Keep it private to avoid multiple instances
    /// </summary>
    private WindowsLogManager()
    {

    }



}
