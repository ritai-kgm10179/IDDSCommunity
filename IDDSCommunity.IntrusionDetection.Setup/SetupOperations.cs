using System;
using System.Diagnostics;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class SetupOperations
{
    private const string ServiceName = "IDDSCommunityProtection";
    private const string ServiceDisplayName = "IDDS Community Protection Service";
    private static readonly string InstallDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IDDS Community");

    /// <summary>Deploys the packaged payload and registers the Windows service.</summary>
    internal static void Install()
    {
        string payload = Path.Combine(AppContext.BaseDirectory, "payload");
        if (!Directory.Exists(payload)) throw new DirectoryNotFoundException(SetupText.Get("PayloadMissing"));
        string parent = Directory.GetParent(InstallDirectory)?.FullName ?? throw new InvalidOperationException();
        string staging = InstallDirectory + ".staging-" + Guid.NewGuid().ToString("N");
        string backup = InstallDirectory + ".backup-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(parent);
        try
        {
            CopyDirectory(payload, staging);
            string stagedService = Path.Combine(staging, "IDDSCommunity.IntrusionDetection.Service.exe");
            if (!File.Exists(stagedService)) throw new FileNotFoundException(SetupText.Get("ServiceExecutableMissing"), stagedService);
            RunSc("stop", ServiceName, acceptMissing: true);
            RunSc("delete", ServiceName, acceptMissing: true);
            if (Directory.Exists(InstallDirectory)) Directory.Move(InstallDirectory, backup);
            Directory.Move(staging, InstallDirectory);
            string service = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe");
            RunSc("create", ServiceName, "binPath=", service, "start=", "auto", "DisplayName=", ServiceDisplayName);
            RunSc("description", ServiceName, SetupText.Get("ServiceDescription"));
            RunSc("failure", ServiceName, "reset=", "86400", "actions=", "restart/5000/restart/15000/none/0");
            ConfigureEventLog();
            RunSc("start", ServiceName);
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (Directory.Exists(backup))
            {
                if (Directory.Exists(InstallDirectory)) Directory.Delete(InstallDirectory, true);
                Directory.Move(backup, InstallDirectory);
            }
            throw;
        }
    }

    private static void ConfigureEventLog()
    {
        if (!EventLog.SourceExists(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE))
            EventLog.CreateEventSource(new EventSourceCreationData(
                Globals.IDDSCOMMUNITY_WINDOWS_EVENT_SOURCE,
                Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME));
        using EventLog log = new(Globals.IDDSCOMMUNITY_WINDOWS_EVENT_LOG_NAME);
        log.MaximumKilobytes = 20 * 1024;
        log.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 0);
    }

    /// <summary>Stops and unregisters the Windows service.</summary>
    internal static void Uninstall()
    {
        RunSc("stop", ServiceName, acceptMissing: true);
        RunSc("delete", ServiceName, acceptMissing: true);
        if (Directory.Exists(InstallDirectory)) Directory.Delete(InstallDirectory, true);
    }

    private static void CopyDirectory(string source, string destination)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException(SetupText.Get("ReparsePointRejected"));
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), true);
        }
    }

    private static void RunSc(string command, string serviceName, params string[] arguments) => RunSc(command, serviceName, false, arguments);

    private static void RunSc(string command, string serviceName, bool acceptMissing, params string[] arguments)
    {
        ProcessStartInfo startInfo = new(Path.Combine(Environment.SystemDirectory, "sc.exe")) { UseShellExecute = false, CreateNoWindow = true };
        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add(serviceName);
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException(SetupText.Get("ServiceControlStartFailed"));
        process.WaitForExit();
        if (process.ExitCode != 0 && !(acceptMissing && process.ExitCode == 1060))
            throw new InvalidOperationException(SetupText.Format("ServiceControlFailed", process.ExitCode));
    }
}
