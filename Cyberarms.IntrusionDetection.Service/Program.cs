using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cyberarms.IntrusionDetection.Service;

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
            builder.Services.AddWindowsService(options => options.ServiceName = "Cyberarms Intrusion Detection Service");
            builder.Services.AddCyberarmsOptions(builder.Configuration);
            builder.Services.AddSingleton<IFirewallPolicy>(_ => FirewallPolicyManager.Instance);
            builder.Services.AddSingleton(provider => new Service(
                provider.GetRequiredService<IFirewallPolicy>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PluginOptions>>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReportOptions>>()));
            builder.Services.AddHostedService<PaladinWorker>();
            using IHost host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service", ex.Message);
        }
    }

    /// <summary>
    /// Handles the thread exception event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) => System.Diagnostics.EventLog.WriteEntry("Cyberarms Intrusion Detection Service Base", e.Exception.Message, System.Diagnostics.EventLogEntryType.Error);
}
