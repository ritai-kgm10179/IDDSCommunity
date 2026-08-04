using System;
using System.Diagnostics;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace EventLogCleaner;

class Program
{
    /// <summary>
    /// Runs the application entry point.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        Console.WriteLine(Strings.Get("This program will remove the Cyberarms EventLog. This can not be undone."));
        Console.WriteLine(Strings.Get("Are you sure that you want to continue? y/N"));
        if (Console.ReadKey().Key == ConsoleKey.Y)
        {
            Console.WriteLine(Strings.Get("Are you really sure? (y/N)"));
            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                try
                {
                    if (EventLog.Exists("Cyberarms Intrusion Detection"))
                    {
                        EventLog.DeleteEventSource("Cyberarms Intrusion Detection");
                        Console.WriteLine(Strings.Get("EventSource 'Cyberarms Intrusion Detection' was deleted"));
                    }
                    else
                    {
                        Console.WriteLine(Strings.Get("EventSource 'Cyberarms Intrusion Detection' was not found on this computer"));
                    }
                    if (EventLog.Exists("Cyberarms"))
                    {
                        EventLog.Delete("Cyberarms");
                        Console.WriteLine(Strings.Get("Event Log 'Cyberarms' was deleted. You might have to restart your computer"));
                        Console.WriteLine(@"and delete the event log file at %systemroot%\system32\winevt\Logs\Cyberarms.evtx");
                    }
                    else
                    {
                        Console.WriteLine(Strings.Get("Event Log 'Cyberarms' was not found on this computer."));
                    }
                    Console.WriteLine(Strings.Get("The command has executed successfully"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Strings.Get("Sorry, we have a problem. Details:\r\n{0}"), ex.Message);
                }
                finally { }
                return;
            }


        }
        Console.WriteLine(Strings.Get("Please be sure to use this utility ONLY when advised by Cyberarms support personel."));
    }
}
