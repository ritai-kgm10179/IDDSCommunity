using System;
using System.ServiceProcess;

namespace Cyberarms.IntrusionDetection.Service;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The event data.</param>

    static void Main(string[] args)
    {
        ServiceBase[] ServicesToRun;
        ServicesToRun =
        [
            new Service()
        ];
        System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
        try
        {
            ServiceBase.Run(ServicesToRun);
        }
        catch (Exception ex)
        {
            System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service", ex.Message);
        }
    }

    /// <summary>
    /// Handles the thread exception event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) => System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service Base", e.Exception.Message, System.Diagnostics.EventLogEntryType.Error);
}
