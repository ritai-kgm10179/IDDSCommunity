using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

/// <summary>
/// Executes Windows service state changes in a short-lived, elevated instance of the administration application.
/// </summary>
internal static class ElevatedServiceCommand
{
    private const string CommandSwitch = "--service-command";
    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Executes an elevated service command when the supplied arguments select command mode.
    /// </summary>
    /// <param name="args">The application command-line arguments.</param>
    /// <param name="exitCode">Receives the process exit code when command mode is selected.</param>
    /// <returns><see langword="true"/> when command mode was selected; otherwise, <see langword="false"/>.</returns>
    internal static bool TryExecute(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length != 3 || !string.Equals(args[0], CommandSwitch, StringComparison.Ordinal))
            return false;

        try
        {
            Execute(args[1], args[2]);
            return true;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Elevated service command failed: {0}", exception);
            exitCode = 1;
            return true;
        }
    }

    /// <summary>
    /// Starts an elevated process and waits asynchronously for the requested service operation.
    /// </summary>
    /// <param name="serviceName">The Windows service name.</param>
    /// <param name="command">The requested <c>start</c>, <c>stop</c>, or <c>restart</c> command.</param>
    /// <param name="cancellationToken">Cancels waiting for the elevated child process.</param>
    /// <returns>A task that completes after the elevated operation succeeds.</returns>
    /// <exception cref="InvalidOperationException">The application executable path is unavailable or the elevated operation fails.</exception>
    /// <exception cref="Win32Exception">Windows rejects process creation or the user cancels the UAC prompt.</exception>
    internal static async Task RunElevatedAsync(string serviceName, string command, CancellationToken cancellationToken)
    {
        string executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException(Strings.Get("The application executable path is unavailable."));
        ProcessStartInfo startInfo = new(executablePath)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory
        };
        startInfo.ArgumentList.Add(CommandSwitch);
        startInfo.ArgumentList.Add(serviceName);
        startInfo.ArgumentList.Add(command);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(Strings.Get("The elevated service command could not be started."));
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(Strings.Format("The elevated service command failed with exit code {0}.", process.ExitCode));
    }

    private static void Execute(string serviceName, string command)
    {
        using ServiceController controller = new(serviceName);
        controller.Refresh();
        switch (command)
        {
            case "start":
                Start(controller);
                break;
            case "stop":
                Stop(controller);
                break;
            case "restart":
                if (controller.Status != ServiceControllerStatus.Stopped)
                    Stop(controller);
                Start(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, Strings.Get("Unsupported service command."));
        }
    }

    private static void Start(ServiceController controller)
    {
        controller.Refresh();
        if (controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            return;
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
    }

    private static void Stop(ServiceController controller)
    {
        controller.Refresh();
        if (controller.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
            return;
        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
    }
}
