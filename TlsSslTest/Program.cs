using System;

using Cyberarms.Agents.TerminalServer;

namespace TlsSslTest;

class Program
{
    /// <summary>
    /// Runs the application entry point.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        TlsSslAgent agent = new();
        agent.Trace += new EventHandler(agent_Trace);
        agent.Tracing = false;
        agent.AttackDetected += new Cyberarms.IntrusionDetection.Api.Plugin.AttackDetectedHandler(agent_AttackDetected);
        ((TslSslConfig)agent.Configuration.AgentSettings!).RdpPort = 3389;
        agent.Start();
        Console.WriteLine("Press any key to abort...");
        Console.ReadKey();
    }

    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    static void agent_AttackDetected(object sender, Cyberarms.IntrusionDetection.Api.Plugin.INotificationEventArgs data) => Console.WriteLine("AttackDetected from " + data.IpAddress);

    /// <summary>
    /// Handles the trace event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    static void agent_Trace(object? sender, EventArgs e)
    {
        if (sender is not IPHeader tls)
            return;
        //Console.WriteLine("{0} {1} {2} {3}", tls.TlsHeader.ContentType, tls.TlsHeader.MajorVersion, tls.TlsHeader.MinorVersion, tls.TlsHeader.Length);
        for (int i = 0; i < int.Parse(tls.TotalLength); i++)
        {
            Console.Write("{0:X}", tls.Data[i]);
        }
        Console.WriteLine();
    }
}
