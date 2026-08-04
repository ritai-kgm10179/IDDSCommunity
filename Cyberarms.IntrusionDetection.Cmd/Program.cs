using System;
using System.Collections.Generic;
namespace Cyberarms.IntrusionDetection.Cmd;

class Program
{
    static readonly LogAlerts logAlerts = new();

    /// <summary>
    /// Starts agent.
    /// </summary>
    /// <param name="agentName">The agent name value.</param>

    static void StartAgent(string agentName)
    {
        foreach (Agent agent in Agents)
        {
            if (agent.Name == agentName)
            {
                agent.Assembly!.Start();
                agent.Assembly.AttackDetected += new Api.Plugin.AttackDetectedHandler(Assembly_AttackDetected);
            }
        }
    }

    /// <summary>
    /// Runs the application entry point.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        try
        {
            // Default: Load WindowsBaseSecurity
            Console.WriteLine("Cyberarms Intrusion Detection Command line plugin test tool");
            var p = System.Diagnostics.Process.GetCurrentProcess();
            string executablePath = p.MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
            Agents.Load(executablePath[..executablePath.LastIndexOf('\\')] + "\\Plugins\\Cyberarms.IntrusionDetection.Base.Plugins.dll");
            StartAgent("WindowsSecurityBase");

            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith("-assemblyName="))
                    {
                        Agents.Load(arg[14..]);
                    }
                }
                foreach (string arg in args)
                {
                    if (arg.StartsWith("-startAgent="))
                    {
                        string agentName = arg[12..];
                        StartAgent(agentName);
                    }
                }
            }

            while (true) ;
        }
        catch
        {
            ShowUsage();
        }
    }

    /// <summary>
    /// Executes the show usage operation.
    /// </summary>

    static void ShowUsage()
    {
        Console.WriteLine("One or some invalid parameters were passed");
        Console.WriteLine("Usage:");
        Console.WriteLine("CyberarmsIdsCmd -assemblyName=assembly-to-load.dll -startAgent=agent1 [-startAgent=agent2]");
        Console.WriteLine("");
        Console.WriteLine("(c) 2012 Cyberarms, isiCore");
    }
    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    static void Assembly_AttackDetected(object sender, Api.Plugin.INotificationEventArgs data)
    {
        string ipAddress = data.IpAddress;
        logAlerts.AddAlert(data.IpAddress, data.CreateDate, data.EventId);
        LogAlert alert = logAlerts.GetAlertFor(ipAddress);
        if (alert.Count <= 3)
        {
            Console.WriteLine(string.Format("Possible intrusion at {0} from {1}. Tried to log in for {2} times.", alert.LastEventDate, alert.IpAddress, alert.Count));
        }
        if (alert.Count > 3) Console.WriteLine(string.Format("\r\nAlert: {0}. login attempt from {1} - {2}\r\n", alert.Count, alert.IpAddress, alert.LastEventDate));

    }

    private static Agents? _agents;
    public static Agents Agents
    {
        get
        {
            _agents ??= [];
            return _agents;
        }
    }


    class LogAlerts : List<LogAlert>
    {
        /// <summary>
        /// Adds alert.
        /// </summary>
        /// <param name="ipAddress">The ip address value.</param>
        /// <param name="eventDate">The event date value.</param>
        /// <param name="eventId">The event id value.</param>

        public void AddAlert(string ipAddress, DateTime eventDate, int eventId)
        {
            bool found = false;
            foreach (LogAlert logAlert in this)
            {
                if (logAlert.EventId == eventId && logAlert.IpAddress == ipAddress)
                {
                    /*if (eventDate.AddMinutes(-60) < logAlert.EventDate) {
                        logAlert.Count++;
                    } */
                    found = true;
                    logAlert.Count++;
                    if (eventDate > logAlert.LastEventDate) logAlert.LastEventDate = eventDate;
                    if (eventDate < logAlert.FirstEventDate) logAlert.FirstEventDate = eventDate;
                }
            }
            if (!found)
            {
                LogAlert logAlert = new()
                {
                    Count = 1,
                    FirstEventDate = eventDate,
                    LastEventDate = eventDate,
                    EventId = eventId,
                    IpAddress = ipAddress
                };
                Add(logAlert);
            }
        }
        /// <summary>
        /// Gets alert for.
        /// </summary>
        /// <param name="ipAddress">The ip address value.</param>
        /// <returns>The get alert for result.</returns>

        public LogAlert GetAlertFor(string ipAddress)
        {
            foreach (LogAlert alert in this)
            {
                if (alert.IpAddress == ipAddress) return alert;
            }
            throw new InvalidOperationException($"Alert for {ipAddress} was not found.");
        }
    }



    class LogAlert
    {
        public string IpAddress { get; set; } = string.Empty;
        public DateTime FirstEventDate { get; set; }
        public DateTime LastEventDate { get; set; }
        public int EventId { get; set; }
        public int Count { get; set; }
    }
}
