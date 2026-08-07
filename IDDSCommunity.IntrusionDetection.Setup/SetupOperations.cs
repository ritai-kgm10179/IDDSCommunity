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

    /// <summary>
    /// 定義安裝程式之動作型別。
    /// </summary>
    internal enum InstallAction
    {
        /// <summary>全新安裝。</summary>
        FreshInstall,
        /// <summary>升級安裝。</summary>
        Upgrade,
        /// <summary>重新安裝或修復。</summary>
        Reinstall,
        /// <summary>降級安裝。</summary>
        Downgrade
    }

    /// <summary>檢查 IDDS Community 服務執行檔是否已安裝。</summary>
    internal static bool IsInstalled =>
        Directory.Exists(InstallDirectory) &&
        File.Exists(Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe"));

    /// <summary>取得已安裝 IDDS Community 服務之版本，若未安裝則傳回 null。</summary>
    internal static Version? InstalledVersion
    {
        get
        {
            string servicePath = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe");
            if (!File.Exists(servicePath)) return null;
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(servicePath);
                string? verStr = fvi.ProductVersion ?? fvi.FileVersion;
                if (!string.IsNullOrEmpty(verStr) && Version.TryParse(verStr.Split('+')[0], out Version? version))
                {
                    return NormalizeVersion(version);
                }
            }
            catch { }
            return null;
        }
    }

    /// <summary>取得當前 Setup 安裝程式之套件版本。</summary>
    internal static Version CurrentSetupVersion
    {
        get
        {
            Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
            return NormalizeVersion(ver ?? new Version(3, 0, 0, 0));
        }
    }

    /// <summary>依據目前系統與當前套件版本判斷預期的安裝動作。</summary>
    internal static InstallAction CurrentInstallAction
    {
        get
        {
            Version? installed = InstalledVersion;
            if (installed == null) return InstallAction.FreshInstall;
            Version current = CurrentSetupVersion;
            int comp = CompareVersions(current, installed);
            if (comp > 0) return InstallAction.Upgrade;
            if (comp == 0) return InstallAction.Reinstall;
            return InstallAction.Downgrade;
        }
    }

    /// <summary>正規化比較兩個 Version，忽視 -1 與 0 在 Revision/Build 的維度差異。</summary>
    internal static int CompareVersions(Version v1, Version v2)
    {
        Version n1 = NormalizeVersion(v1);
        Version n2 = NormalizeVersion(v2);
        return n1.CompareTo(n2);
    }

    private static Version NormalizeVersion(Version v)
    {
        return new Version(
            v.Major < 0 ? 0 : v.Major,
            v.Minor < 0 ? 0 : v.Minor,
            v.Build < 0 ? 0 : v.Build,
            v.Revision < 0 ? 0 : v.Revision);
    }

    internal static readonly string DesktopShortcutPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "IDDS Community Admin.lnk");

    internal static readonly string StartMenuDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "IDDS Community");

    internal static readonly string StartMenuShortcutPath =
        Path.Combine(StartMenuDirectory, "IDDS Community Admin.lnk");

    /// <summary>檢查桌面捷徑是否存在。</summary>
    internal static bool HasDesktopShortcut => File.Exists(DesktopShortcutPath);

    /// <summary>檢查開始功能表捷徑是否存在。</summary>
    internal static bool HasStartMenuShortcut => File.Exists(StartMenuShortcutPath);

    /// <summary>建立或清理桌面與開始功能表捷徑。</summary>
    /// <param name="desktop">是否建立桌面捷徑。</param>
    /// <param name="startMenu">是否建立開始功能表捷徑。</param>
    internal static void CreateShortcuts(bool desktop, bool startMenu)
    {
        if (!IsInstalled) return;

        if (desktop)
        {
            CreateLnk(DesktopShortcutPath, AdminExecutablePath, "IDDS Community Management Admin Console");
        }
        else if (File.Exists(DesktopShortcutPath))
        {
            try { File.Delete(DesktopShortcutPath); } catch { }
        }

        if (startMenu)
        {
            Directory.CreateDirectory(StartMenuDirectory);
            CreateLnk(StartMenuShortcutPath, AdminExecutablePath, "IDDS Community Management Admin Console");
        }
        else if (File.Exists(StartMenuShortcutPath))
        {
            try { File.Delete(StartMenuShortcutPath); } catch { }
        }
    }

    /// <summary>移除桌面與開始功能表捷徑。</summary>
    internal static void RemoveShortcuts()
    {
        try { if (File.Exists(DesktopShortcutPath)) File.Delete(DesktopShortcutPath); } catch { }
        try
        {
            if (Directory.Exists(StartMenuDirectory))
                Directory.Delete(StartMenuDirectory, true);
        }
        catch { }
    }

    private static void CreateLnk(string shortcutPath, string targetPath, string description)
    {
        string script = $"$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('{shortcutPath}'); $Shortcut.TargetPath = '{targetPath}'; $Shortcut.WorkingDirectory = '{Path.GetDirectoryName(targetPath)}'; $Shortcut.Description = '{description}'; $Shortcut.Save()";
        ProcessStartInfo psi = new("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process p = Process.Start(psi)!;
        p.WaitForExit();
    }

    /// <summary>開啟 IDDS Community 安裝與使用說明文件。</summary>
    internal static void OpenUserGuide()
    {
        string localGuide = Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.zh-TW.md");
        if (!File.Exists(localGuide))
        {
            localGuide = Path.Combine(InstallDirectory, "docs", "USER-GUIDE.zh-TW.md");
        }

        if (File.Exists(localGuide))
        {
            Process.Start(new ProcessStartInfo(localGuide) { UseShellExecute = true });
        }
        else
        {
            Process.Start(new ProcessStartInfo("https://github.com/ritai-kgm10179/IDDSCommunity#readme") { UseShellExecute = true });
        }
    }

    /// <summary>Checks whether the administration UI executable is available for launching.</summary>
    internal static bool CanLaunchApp => IsInstalled && File.Exists(AdminExecutablePath);

    /// <summary>Launches the IDDS Community administration UI.</summary>
    internal static void LaunchApp()
    {
        if (!CanLaunchApp) return;
        Process.Start(new ProcessStartInfo(AdminExecutablePath) { UseShellExecute = true });
    }

    /// <summary>Deploys the packaged payload, registers the Windows service, and configures shortcuts.</summary>
    /// <param name="desktopShortcut">是否建立桌面捷徑。</param>
    /// <param name="startMenuShortcut">是否建立開始功能表捷徑。</param>
    internal static void Install(bool desktopShortcut = true, bool startMenuShortcut = true)
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
            CreateShortcuts(desktopShortcut, startMenuShortcut);
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

    /// <summary>停止並註銷 Windows 服務、終止相關程序，並移除安裝檔案、防火牆規則與捷徑。</summary>
    internal static void Uninstall()
    {
        RemoveShortcuts();
        CleanUpFirewallRules();
        RunSc("stop", ServiceName, acceptMissing: true);
        RunSc("delete", ServiceName, acceptMissing: true);
        KillRunningProcesses();
        System.Threading.Thread.Sleep(500);
        SafeDeleteDirectory(InstallDirectory);
    }

    private static void CleanUpFirewallRules()
    {
        try
        {
            ProcessStartInfo psi = new(Path.Combine(Environment.SystemDirectory, "netsh.exe"), "advfirewall firewall delete rule name=all group=\"IDDS Community\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(psi);
            process?.WaitForExit(2000);
        }
        catch { }
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
