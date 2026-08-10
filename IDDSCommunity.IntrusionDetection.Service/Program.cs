using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IDDSCommunity.IntrusionDetection.Service;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The event data.</param>
    private static async System.Threading.Tasks.Task Main(string[] args)
    {
        System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
        try
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddIDDSCommunityWindowsService();
            builder.Services.AddIDDSCommunityOptions(builder.Configuration);
            builder.Services.AddIDDSCommunityRuntime();
            builder.Services.AddHostedService<ProtectionWorker>();
            using IHost host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            WriteFatalError("Service host terminated unexpectedly.", ex);
        }
    }
    /// <summary>
    /// 處理 thread exception 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) =>
        WriteFatalError("Unhandled service thread exception.", e.Exception);

    private static void WriteFatalError(string message, Exception exception)
    {
        _ = Shared.RollingDiagnosticLog.Write("Service-Host", message, exception);
        try
        {
            System.Diagnostics.EventLog.WriteEntry(
                Shared.Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE,
                $"{message}{Environment.NewLine}{exception}",
                System.Diagnostics.EventLogEntryType.Error);
        }
        catch (Exception eventLogException)
        {
            _ = Shared.RollingDiagnosticLog.Write("Service-Host-EventLog", message, eventLogException);
        }
    }
}
