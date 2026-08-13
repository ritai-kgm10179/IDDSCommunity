using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;
/// <summary>
/// 於短生命週期提權之管理應用程式執行個體中執行 Windows 服務狀態變更作業。
/// </summary>
internal static class ElevatedServiceCommand
{
    private const string CommandSwitch = "--service-command";
    private const string AllowedServiceName = global::IDDSCommunity.IntrusionDetection.Shared.Globals.WINDOWS_SERVICE_NAME;
    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);
    /// <summary>
    /// 當傳入引數選取命令模式時，執行提升權限之服務命令。
    /// </summary>
    /// <param name="args">應用程式命令列引數。</param>
    /// <param name="exitCode">當選取命令模式時傳回處理程序結束碼。</param>
    /// <returns>若選取命令模式傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
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
    /// 啟動提權處理程序並非同步等待要求的服務操作完成。
    /// </summary>
    /// <param name="serviceName">Windows 服務名稱參數。</param>
    /// <param name="command">要求的安裝、解除安裝、啟動、停止或重新啟動命令參數。</param>
    /// <param name="cancellationToken">取消等待提權子處理程序之取消權杖。</param>
    /// <returns>傳回於提權操作成功後完成之工作。</returns>
    /// <exception cref="InvalidOperationException">當應用程式執行檔路徑不可用或提權操作失敗時拋出。</exception>
    /// <exception cref="Win32Exception">當 Windows 拒絕建立處理程序或使用者取消 UAC 提示時拋出。</exception>
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
        if (!string.Equals(serviceName, AllowedServiceName, StringComparison.Ordinal))
            throw new ArgumentException(Strings.Get("Unsupported service name."), nameof(serviceName));
        if (command == "install")
        {
            Install(serviceName);
            return;
        }
        if (command == "uninstall")
        {
            Uninstall(serviceName);
            return;
        }
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

    private static void Install(string serviceName)
    {
        string servicePath = ResolveServiceExecutablePath();
        RunServiceControl("create", serviceName, "binPath=", servicePath, "start=", "auto", "DisplayName=", global::IDDSCommunity.IntrusionDetection.Shared.Globals.WINDOWS_SERVICE_DISPLAY_NAME);
        using ServiceController controller = new(serviceName);
        Start(controller);
    }

    private static void Uninstall(string serviceName)
    {
        try
        {
            using ServiceController controller = new(serviceName);
            Stop(controller);
        }
        catch (InvalidOperationException)
        {
            // The service may already be absent; sc.exe delete provides the authoritative result.
        }
        RunServiceControl("delete", serviceName);
    }

    private static string ResolveServiceExecutablePath()
    {
        const string executableName = "IDDSCommunity.IntrusionDetection.Service.exe";
        string installedPath = Path.Combine(AppContext.BaseDirectory, executableName);
        if (File.Exists(installedPath)) return installedPath;

        string developmentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "IDDSCommunity.IntrusionDetection.Service", "bin",
#if DEBUG
            "Debug",
#else
            "Release",
#endif
            "net10.0-windows", executableName));
        if (File.Exists(developmentPath)) return developmentPath;
        throw new FileNotFoundException(Strings.Get("The IDDSCommunity service executable could not be found."), installedPath);
    }

    private static void RunServiceControl(params string[] arguments)
    {
        ProcessStartInfo startInfo = new(Path.Combine(Environment.SystemDirectory, "sc.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(Strings.Get("The Windows service management command could not be started."));
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(Strings.Format("The Windows service management command failed with exit code {0}.", process.ExitCode));
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
