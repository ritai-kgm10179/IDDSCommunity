using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class SetupOperations
{
    private const string ServiceName = "IDDSCommunityProtection";
    private const string ServiceDisplayName = "IDDS Community Protection Service";
    internal static readonly string InstallDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IDDS Community");
    internal static readonly string AdminExecutablePath = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Admin.exe");

    /// <summary>Checks whether the IDDS Community service executable is installed.</summary>
    internal static bool IsInstalled =>
        Directory.Exists(InstallDirectory) &&
        File.Exists(Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe"));

    /// <summary>Checks whether the administration UI executable is available for launching.</summary>
    internal static bool CanLaunchApp => IsInstalled && File.Exists(AdminExecutablePath);

    /// <summary>Launches the IDDS Community administration UI.</summary>
    internal static void LaunchApp()
    {
        if (!CanLaunchApp) return;
        Process.Start(new ProcessStartInfo(AdminExecutablePath) { UseShellExecute = true });
    }

    /// <summary>Deploys the packaged payload and registers the Windows service.</summary>
    internal static void Install()
    {
        string payloadDir = Path.Combine(AppContext.BaseDirectory, "payload");
        string tempExtractedPayload = string.Empty;

        try
        {
            if (!Directory.Exists(payloadDir))
            {
                string payloadZip = Path.Combine(AppContext.BaseDirectory, "payload.zip");
                if (File.Exists(payloadZip))
                {
                    tempExtractedPayload = Path.Combine(Path.GetTempPath(), "idds_payload_" + Guid.NewGuid().ToString("N"));
                    System.IO.Compression.ZipFile.ExtractToDirectory(payloadZip, tempExtractedPayload);
                    payloadDir = tempExtractedPayload;
                }
                else
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    string[] resNames = asm.GetManifestResourceNames();
                    string? payloadRes = Array.Find(resNames, r => r.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));
                    if (payloadRes != null)
                    {
                        using Stream? resourceStream = asm.GetManifestResourceStream(payloadRes);
                        if (resourceStream != null)
                        {
                            tempExtractedPayload = Path.Combine(Path.GetTempPath(), "idds_payload_" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(tempExtractedPayload);
                            string tempZipPath = Path.Combine(tempExtractedPayload, "payload.zip");
                            using (FileStream fs = File.Create(tempZipPath))
                            {
                                resourceStream.CopyTo(fs);
                            }
                            string extractedDir = Path.Combine(tempExtractedPayload, "extracted");
                            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractedDir);
                            payloadDir = extractedDir;
                        }
                    }
                }
            }

            if (!Directory.Exists(payloadDir)) throw new DirectoryNotFoundException(SetupText.Get("PayloadMissing"));
            string parent = Directory.GetParent(InstallDirectory)?.FullName ?? throw new InvalidOperationException();
            Directory.CreateDirectory(parent);

            RunSc("stop", ServiceName, acceptMissing: true);
            RunSc("delete", ServiceName, acceptMissing: true);
            KillRunningProcesses();
            System.Threading.Thread.Sleep(500);

            Directory.CreateDirectory(InstallDirectory);
            CopyDirectoryOverwrite(payloadDir, InstallDirectory);

            string service = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe");
            if (!File.Exists(service)) throw new FileNotFoundException(SetupText.Get("ServiceExecutableMissing"), service);

            RunSc("create", ServiceName, "binPath=", service, "start=", "auto", "DisplayName=", ServiceDisplayName);
            RunSc("description", ServiceName, SetupText.Get("ServiceDescription"));
            RunSc("failure", ServiceName, "reset=", "86400", "actions=", "restart/5000/restart/15000/none/0");
            ConfigureEventLog();
            RunSc("start", ServiceName);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempExtractedPayload) && Directory.Exists(tempExtractedPayload))
            {
                try { Directory.Delete(tempExtractedPayload, true); } catch { }
            }
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

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);
    private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

    /// <summary>Stops and unregisters the Windows service, terminates associated processes, and removes files.</summary>
    internal static void Uninstall()
    {
        RunSc("stop", ServiceName, acceptMissing: true);
        RunSc("delete", ServiceName, acceptMissing: true);
        KillRunningProcesses();
        System.Threading.Thread.Sleep(500);
        SafeDeleteDirectory(InstallDirectory);
    }

    private static void KillRunningProcesses()
    {
        string[] targetProcessNames = ["IDDSCommunity.IntrusionDetection.Service", "IDDSCommunity.IntrusionDetection.Admin"];
        foreach (string name in targetProcessNames)
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        string? mainModulePath = p.MainModule?.FileName;
                        if (mainModulePath != null && mainModulePath.StartsWith(InstallDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill(true);
                            p.WaitForExit(2000);
                        }
                    }
                    catch
                    {
                        // Fallback if MainModule inspect fails (e.g. process exiting)
                        try { p.Kill(true); } catch { }
                    }
                }
            }
            catch { }
        }
    }

    private static void SafeDeleteDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        foreach (string file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch (Exception)
            {
                // Handle file locks (e.g. runtime DLLs like clrjit.dll) via Win32 delay until reboot
                try
                {
                    MoveFileEx(file, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch { }
            }
        }

        foreach (string subDir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
        {
            try
            {
                Directory.Delete(subDir, false);
            }
            catch { }
        }

        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch { }
    }

    private static void CopyDirectoryOverwrite(string source, string destination)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException(SetupText.Get("ReparsePointRejected"));

        Directory.CreateDirectory(destination);

        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            string destFile = Path.Combine(destination, Path.GetRelativePath(source, file));
            try
            {
                if (File.Exists(destFile))
                {
                    File.SetAttributes(destFile, FileAttributes.Normal);
                }
                File.Copy(file, destFile, true);
            }
            catch (Exception)
            {
                // Fallback via MoveFileEx for locked binaries
                try
                {
                    MoveFileEx(destFile, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                }
                catch { }
            }
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
        if (process.ExitCode != 0 && !(acceptMissing && (process.ExitCode == 1060 || process.ExitCode == 1062)))
            throw new InvalidOperationException(SetupText.Format("ServiceControlFailed", process.ExitCode));
    }
}
