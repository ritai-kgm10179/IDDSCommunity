using System;
using Cyberarms.IntrusionDetection.Api.Plugin;
using Cyberarms.Agents.SqlServer;

namespace SqlServerAgentTest;

class Program
{
    /// <summary>
    /// Runs the application entry point.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        SqlFailedLoginWatcher watcher = new();
        watcher.AttackDetected += new AttackDetectedHandler(watcher_AttackDetected);
        watcher.Start();
        Console.ReadKey();
        watcher.Stop();
    }

    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    static void watcher_AttackDetected(object sender, INotificationEventArgs data)
    {
        Console.WriteLine("{0}: {1}", data.EventMessage, data.IpAddress);
    }
}
