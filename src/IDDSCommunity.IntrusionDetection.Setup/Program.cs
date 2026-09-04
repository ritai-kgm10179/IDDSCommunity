using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class Program
{
    /// <summary>
    /// 啟動提升權限之安裝管理使用者介面或執行無人值守命令列作業。
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && string.Equals(args[0], "--verify-reinstall", StringComparison.OrdinalIgnoreCase))
            return VerifyReinstall();

        CommandLineOptions options = CommandLineOptions.Parse(args);
        if (options.IsQuiet)
        {
            return RunSilent(options);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SetupForm(options));
        return 0;
    }

    private static int RunSilent(CommandLineOptions options)
    {
        try
        {
            if (options.IsUninstall)
            {
                if (!SetupOperations.IsInstalled) return 0;
                SetupOperations.SetupOperationResult result = SetupOperations.Uninstall();
                return result.CleanupIncomplete ? 2 : (result.RestartRequired ? 3010 : 0);
            }
            else
            {
                SetupOperations.SetupOperationResult result = SetupOperations.Install(
                    desktopShortcut: !options.NoDesktop,
                    startMenuShortcut: !options.NoStartMenu);
                return result.CleanupIncomplete ? 2 : (result.RestartRequired ? 3010 : 0);
            }
        }
        catch (Exception exception)
        {
            _ = RollingDiagnosticLog.Write("Setup", "Silent setup operation failed", exception);
            return 1;
        }
    }

    private static int VerifyReinstall()
    {
        try
        {
            if (SetupOperations.IsInstalled)
                _ = SetupOperations.Uninstall(cancellationToken: CancellationToken.None);

            _ = SetupOperations.Install(cancellationToken: CancellationToken.None);
            VerifyInstalledState("fresh install");
            _ = SetupOperations.Install(cancellationToken: CancellationToken.None);
            VerifyInstalledState("reinstall");
            return 0;
        }
        catch (Exception exception)
        {
            _ = RollingDiagnosticLog.Write("Setup", "Automated reinstall verification failed", exception);
            return 1;
        }
    }

    private static void VerifyInstalledState(string operation)
    {
        if (!SetupOperations.IsInstalled)
            throw new IOException($"The {operation} did not produce a complete installation.");
        if (SetupOperations.InstalledVersion != SetupOperations.CurrentSetupVersion)
            throw new IOException($"The {operation} installed an unexpected version.");

        using ServiceController service = new(Globals.WINDOWS_SERVICE_NAME);
        service.Refresh();
        if (service.Status != ServiceControllerStatus.Running)
            throw new InvalidOperationException($"The service was not running after {operation}.");
    }
}
