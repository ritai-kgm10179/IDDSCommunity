using System;
using System.Diagnostics;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service.Test;

internal class WindowsLogManager
{
    private DateTime lastSearchDate;

    // public override event AttackDetectedHandler AttackDetected;

    private EventLog eventLogCyberarms;


    private static WindowsLogManager _instance;
    internal static WindowsLogManager Instance
    {
        get
        {
            _instance ??= new WindowsLogManager
                {
                    lastSearchDate = DateTime.Now
                };
            return _instance;
        }
    }


    internal void WriteEntry(string text, EventLogEntryType type, int eventId, short category)
    {
        // Delete old log
        //try {
        //    EventLog.DeleteEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
        //    EventLog.Delete(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME);
        //} catch { }
        //if (!EventLog.Exists(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME) || !EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE)) {
        //    // did somebody delete the eventlog with event viewer?
        //    if (!EventLog.Exists(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME) && EventLog.SourceExists(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE)) {
        //        // delete the source first
        //        EventLog.DeleteEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE);
        //    }
        //    EventLog.CreateEventSource(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE, Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME);
        //    System.Diagnostics.EventLogInstaller installer = new EventLogInstaller();

        //} 
        eventLogCyberarms ??= new EventLog(Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME);
        eventLogCyberarms.Source = Globals.CYBERARMS_WINDOWS_EVENT_SOURCE;
        eventLogCyberarms.Log = Globals.CYBERARMS_WINDOWS_EVENT_LOG_NAME;

        EventLog.WriteEntry(Globals.CYBERARMS_WINDOWS_EVENT_SOURCE, text, type, eventId, category);
        eventLogCyberarms.WriteEntry(text, type, eventId, category);
    }




    /// <summary>
    /// Keep it private to avoid multiple instances
    /// </summary>
    private WindowsLogManager()
    {

    }



}
