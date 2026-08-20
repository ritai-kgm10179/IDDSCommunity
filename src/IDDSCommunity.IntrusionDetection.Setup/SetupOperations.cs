using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Security.Principal;
using System.Threading;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal static class SetupOperations
{
    private const string ServiceName = Globals.WINDOWS_SERVICE_NAME;
    private const string ServiceDisplayName = Globals.WINDOWS_SERVICE_DISPLAY_NAME;
    private static readonly TimeSpan ServiceStartTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DirectoryMoveRetryDelay = TimeSpan.FromMilliseconds(500);
    private const int DirectoryMoveAttempts = 20;
    internal static readonly string InstallDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IDDS Community");
    internal static readonly string AdminExecutablePath = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Admin.exe");
    /// <summary>
    /// 定義安裝程式之動作型別。
    /// </summary>
    internal enum InstallAction
    {
        /// <summary>
        /// 全新安裝。
        /// </summary>
        FreshInstall,
        /// <summary>
        /// 升級安裝。
        /// </summary>
        Upgrade,
        /// <summary>
        /// 重新安裝或修復。
        /// </summary>
        Reinstall,
        /// <summary>
        /// 降級安裝。
        /// </summary>
        Downgrade
    }
    /// <summary>
    /// 檢查 IDDS 社群版服務執行檔是否已安裝。
    /// </summary>
    internal static bool IsInstalled =>
        Directory.Exists(InstallDirectory) &&
        File.Exists(Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe"));
    /// <summary>
    /// 取得已安裝 IDDS 社群版服務之版本，若未安裝則傳回 null。
    /// </summary>
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
            catch (Exception exception)
            {
                LogNonFatal("Read installed version", exception);
            }
            return null;
        }
    }
    /// <summary>
    /// 取得當前 Setup 安裝程式之套件版本。
    /// </summary>
    internal static Version CurrentSetupVersion
    {
        get
        {
            Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
            return NormalizeVersion(ver ?? new Version(3, 0, 0, 0));
        }
    }
    /// <summary>
    /// 依據目前系統與當前套件版本判斷預期的安裝動作。
    /// </summary>
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
    /// <summary>
    /// 正規化比較兩個 Version，忽視 -1 與 0 在 Revision/Build 的維度差異。
    /// </summary>
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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "IDDS Community Admin.lnk");

    internal static readonly string StartMenuDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "IDDS Community");

    internal static readonly string StartMenuShortcutPath =
        Path.Combine(StartMenuDirectory, "IDDS Community Admin.lnk");

    /// <summary>
    /// 檢查桌面捷徑是否存在。
    /// </summary>
    internal static bool HasDesktopShortcut => File.Exists(DesktopShortcutPath);

    /// <summary>
    /// 檢查開始功能表捷徑是否存在。
    /// </summary>
    internal static bool HasStartMenuShortcut => File.Exists(StartMenuShortcutPath);

    /// <summary>
    /// 建立或清理桌面與開始功能表捷徑。
    /// </summary>
    /// <param name="desktop">是否建立桌面捷徑。</param>
    /// <param name="startMenu">是否建立開始功能表捷徑。</param>
    internal static void CreateShortcuts(bool desktop, bool startMenu)
    {
        if (desktop)
        {
            CreateLnk(DesktopShortcutPath, AdminExecutablePath, "IDDS Community Management Admin Console");
        }
        else if (File.Exists(DesktopShortcutPath))
        {
            File.Delete(DesktopShortcutPath);
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }

        if (startMenu)
        {
            Directory.CreateDirectory(StartMenuDirectory);
            CreateLnk(StartMenuShortcutPath, AdminExecutablePath, "IDDS Community Management Admin Console");
        }
        else if (File.Exists(StartMenuShortcutPath))
        {
            File.Delete(StartMenuShortcutPath);
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>
    /// 移除桌面與開始功能表捷徑。
    /// </summary>
    internal static void RemoveShortcuts()
    {
        if (File.Exists(DesktopShortcutPath)) File.Delete(DesktopShortcutPath);
        if (Directory.Exists(StartMenuDirectory))
            Directory.Delete(StartMenuDirectory, true);
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
    }

    private static void CreateLnk(string shortcutPath, string targetPath, string description)
    {
        try
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
            link.SetDescription(description);

            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, true);

            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception primaryFailure)
        {
            try
            {
                Type? wshType = Type.GetTypeFromCLSID(new Guid("72C24DD5-5D27-11CF-A9F3-00B0C08FDFC0"));
                if (wshType is null) throw new InvalidOperationException(SetupText.Format("ShortcutCreationFailed", shortcutPath));
                dynamic? wsh = Activator.CreateInstance(wshType);
                if (wsh is null) throw new InvalidOperationException(SetupText.Format("ShortcutCreationFailed", shortcutPath));
                dynamic shortcut = wsh.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Description = description;
                shortcut.Save();
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception fallbackFailure)
            {
                throw new AggregateException(SetupText.Format("ShortcutCreationFailed", shortcutPath), primaryFailure, fallbackFailure);
            }
        }
    }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    [System.Runtime.InteropServices.Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
        void SetDescription([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out ushort pwHotkey);
        void SetHotkey(ushort wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFile);
    }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    [System.Runtime.InteropServices.Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_FLUSH = 0x1000;
    /// <summary>
    /// 開啟 IDDS 社群版安裝與使用說明文件。
    /// </summary>
    internal static void OpenUserGuide()
    {
        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, "USER-GUIDE.md"),
            Path.Combine(AppContext.BaseDirectory, "docs", "USER-GUIDE.zh-TW.md"),
            Path.Combine(InstallDirectory, "USER-GUIDE.md"),
            Path.Combine(InstallDirectory, "docs", "USER-GUIDE.zh-TW.md"),
            Path.Combine(AppContext.BaseDirectory, "README.md"),
            Path.Combine(InstallDirectory, "README.md")
        ];

        foreach (string path in candidatePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    return;
                }
                catch (Exception exception)
                {
                    LogNonFatal("Open local user guide", exception);
                }
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/ritai-kgm10179/IDDSCommunity#readme") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            LogNonFatal("Open online user guide", exception);
        }
    }
    /// <summary>
    /// 檢查管理控制台執行檔是否可供啟動。
    /// </summary>
    internal static bool CanLaunchApp => IsInstalled && File.Exists(AdminExecutablePath);
    /// <summary>
    /// 啟動 IDDS 社群版管理控制台 UI。
    /// </summary>
    internal static void LaunchApp()
    {
        if (!CanLaunchApp) return;
        ProcessStartInfo startInfo = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"))
        {
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(AdminExecutablePath);
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException(SetupText.Get("ApplicationLaunchFailed"));
    }
    /// <summary>
    /// 部署封裝之軟體資產、註冊 Windows 服務並設定系統捷徑。
    /// </summary>
    /// <param name="desktopShortcut">是否建立桌面捷徑。</param>
    /// <param name="startMenuShortcut">是否建立開始功能表捷徑。</param>
    /// <param name="progress">安裝進度回報介面。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>傳回安裝作業結果。</returns>
    internal static SetupOperationResult Install(
        bool desktopShortcut = true,
        bool startMenuShortcut = true,
        IProgress<SetupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string payloadDir = Path.Combine(AppContext.BaseDirectory, "payload");
        string tempExtractedPayload = string.Empty;
        string parent = Directory.GetParent(InstallDirectory)?.FullName ?? throw new InvalidOperationException();
        string stagingDirectory = Path.Combine(parent, ".idds-stage-" + Guid.NewGuid().ToString("N"));
        string backupDirectory = Path.Combine(parent, ".idds-backup-" + Guid.NewGuid().ToString("N"));
        bool previousInstallationMoved = false;
        bool installationDirectoryReplaced = false;
        bool systemStateChanged = false;
        bool newServiceCreated = false;
        ServiceStateSnapshot serviceState = default;
        bool restartRequired = false;
        bool cleanupIncomplete = false;

        try
        {
            Report(progress, "ProgressPreparing", 5);
            cancellationToken.ThrowIfCancellationRequested();
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
            Directory.CreateDirectory(parent);

            Report(progress, "ProgressValidating", 15);
            CopyDirectoryOverwrite(payloadDir, stagingDirectory, cancellationToken);
            string stagedService = Path.Combine(stagingDirectory, "IDDSCommunity.IntrusionDetection.Service.exe");
            if (!File.Exists(stagedService)) throw new FileNotFoundException(SetupText.Get("ServiceExecutableMissing"), stagedService);
            cancellationToken.ThrowIfCancellationRequested();

            Report(progress, "ProgressStoppingService", 30);
            serviceState = CaptureServiceState();
            StopService(serviceState);
            systemStateChanged = true;
            KillRunningProcesses();
            cancellationToken.ThrowIfCancellationRequested();

            Report(progress, "ProgressInstallingFiles", 50);
            if (Directory.Exists(InstallDirectory))
            {
                MoveDirectoryWithRetry(InstallDirectory, backupDirectory, cancellationToken);
                previousInstallationMoved = true;
            }
            MoveDirectoryWithRetry(stagingDirectory, InstallDirectory, cancellationToken);
            installationDirectoryReplaced = true;
            cancellationToken.ThrowIfCancellationRequested();

            string service = Path.Combine(InstallDirectory, "IDDSCommunity.IntrusionDetection.Service.exe");
            if (!File.Exists(service)) throw new FileNotFoundException(SetupText.Get("ServiceExecutableMissing"), service);

            Report(progress, "ProgressRegisteringService", 70);
            if (!serviceState.Exists)
            {
                ConfigureNewService(service);
                newServiceCreated = true;
            }
            ConfigureEventLog();
            EnsureOperatorsGroup();
            ConfigureDataDirectoryPermissions();
            cancellationToken.ThrowIfCancellationRequested();
            if (!serviceState.Exists)
            {
                Report(progress, "ProgressStartingService", 85);
                StartServiceAndVerify();
            }
            else if (serviceState.Status != ServiceControllerStatus.Stopped)
            {
                Report(progress, "ProgressStartingService", 85);
                RestoreServiceState(serviceState);
            }
            CreateShortcuts(desktopShortcut, startMenuShortcut);
            if (previousInstallationMoved)
            {
                try
                {
                    restartRequired |= SafeDeleteDirectory(backupDirectory);
                }
                catch (Exception exception)
                {
                    LogNonFatal($"Clean previous installation backup {backupDirectory}", exception);
                    cleanupIncomplete = true;
                }
            }
            Report(progress, "ProgressCompleted", 100);
            return new SetupOperationResult(restartRequired, cleanupIncomplete);
        }
        catch (Exception installationFailure)
        {
            try
            {
                if (systemStateChanged)
                    RollBackInstallation(previousInstallationMoved, installationDirectoryReplaced, backupDirectory, serviceState, newServiceCreated);
            }
            catch (Exception rollbackFailure)
            {
                throw new AggregateException(SetupText.Get("RollbackFailed"), installationFailure, rollbackFailure);
            }
            throw;
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
                TryCleanTemporaryDirectory(stagingDirectory);
            if (!string.IsNullOrEmpty(tempExtractedPayload) && Directory.Exists(tempExtractedPayload))
            {
                TryCleanTemporaryDirectory(tempExtractedPayload);
            }
        }
    }

    private static void RollBackInstallation(
        bool previousInstallationMoved,
        bool installationDirectoryReplaced,
        string backupDirectory,
        ServiceStateSnapshot serviceState,
        bool newServiceCreated)
    {
        RunSc("stop", ServiceName, acceptMissing: true);
        if (newServiceCreated)
            RunSc("delete", ServiceName, acceptMissing: true);
        KillRunningProcesses();
        if (installationDirectoryReplaced && Directory.Exists(InstallDirectory))
            _ = SafeDeleteDirectory(InstallDirectory);
        if (previousInstallationMoved)
        {
            if (!Directory.Exists(backupDirectory))
                throw new DirectoryNotFoundException(backupDirectory);
            MoveDirectoryWithRetry(backupDirectory, InstallDirectory, CancellationToken.None);
        }
        RestoreServiceState(serviceState);
    }

    private static void Report(IProgress<SetupProgress>? progress, string messageKey, int percentage) =>
        progress?.Report(new SetupProgress(messageKey, percentage));

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

    /// <summary>
    /// 設定全系統共用之資料庫目錄存取權限，使非提升權限管理介面能正常存取 SQLite WAL 與 SHM 檔案。
    /// </summary>
    private static void ConfigureDataDirectoryPermissions()
    {
        string dataDir = IddsConfig.GetDefaultDataDirectory();
        Directory.CreateDirectory(dataDir);
        using PrincipalContext ctx = new(ContextType.Machine);
        using GroupPrincipal operatorsGroup = GroupPrincipal.FindByIdentity(ctx, Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME)
            ?? throw new InvalidOperationException(SetupText.Format("OperatorsGroupNotFound", Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME));
        SecurityIdentifier operatorsSid = operatorsGroup.Sid
            ?? throw new InvalidOperationException(SetupText.Format("OperatorsGroupSidUnavailable", Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME));

        DirectoryInfo dirInfo = new(dataDir);
        System.Security.AccessControl.DirectorySecurity security = CreateDataDirectorySecurity(operatorsSid);
        dirInfo.SetAccessControl(security);
    }

    /// <summary>
    /// 建立資料目錄的存取控制描述元。
    /// </summary>
    /// <remarks>
    /// 僅授予 <c>SYSTEM</c>（完全控制）、<c>BUILTIN\Administrators</c>（完全控制）
    /// 與選用的 <paramref name="operatorsSid"/>（修改），符合 AGENTS.md 規範第 8 條。
    /// 禁止對 <c>BUILTIN\Users</c>、<c>Everyone</c>、<c>Authenticated Users</c> 等廣泛群組授予任何存取權限。
    /// </remarks>
    /// <param name="operatorsSid">
    /// <c>IDDSCommunityOperators</c> 群組的 SID；若群組尚未建立則傳入 <see langword="null"/>，此時略過操作人員授權規則。
    /// </param>
    /// <returns>已設定繼承保護與明確 ACL 的 <see cref="System.Security.AccessControl.DirectorySecurity"/> 執行個體。</returns>
    internal static System.Security.AccessControl.DirectorySecurity CreateDataDirectorySecurity(SecurityIdentifier? operatorsSid)
    {
        System.Security.AccessControl.DirectorySecurity security = new();

        // 停用繼承並保留現有繼承條目為明確規則，確保子物件（-wal、-shm）遵守相同邊界
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        System.Security.AccessControl.InheritanceFlags inherit =
            System.Security.AccessControl.InheritanceFlags.ContainerInherit |
            System.Security.AccessControl.InheritanceFlags.ObjectInherit;

        // SYSTEM：完全控制（服務寫入資料庫與 WAL 所需）
        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            System.Security.AccessControl.FileSystemRights.FullControl,
            inherit,
            System.Security.AccessControl.PropagationFlags.None,
            System.Security.AccessControl.AccessControlType.Allow));

        // Administrators：完全控制（本機管理員維護所需）
        security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            System.Security.AccessControl.FileSystemRights.FullControl,
            inherit,
            System.Security.AccessControl.PropagationFlags.None,
            System.Security.AccessControl.AccessControlType.Allow));

        // IDDSCommunityOperators：Modify 繼承（Admin 主控台取得 SQLite WAL 鎖所需）
        if (operatorsSid is not null)
        {
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                operatorsSid,
                System.Security.AccessControl.FileSystemRights.Modify,
                inherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Allow));
        }

        return security;
    }


    /// <summary>
    /// 建立（若不存在）供非提升權限管理主控台使用的本機群組，並將目前執行安裝程式的使用者加入其中，
    /// 使其後續可讀取受 DPAPI 保護的資料庫金鑰檔案，而不需授予本機所有標準使用者存取權限。
    /// </summary>
    private static void EnsureOperatorsGroup()
    {
        string? currentUserSid = WindowsIdentity.GetCurrent().User?.Value;
        if (currentUserSid is null)
            throw new InvalidOperationException(SetupText.Get("CurrentUserSidUnavailable"));

        using PrincipalContext context = new(ContextType.Machine);
        using GroupPrincipal group = GroupPrincipal.FindByIdentity(context, Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME)
            ?? CreateOperatorsGroup(context);
        using UserPrincipal? user = UserPrincipal.FindByIdentity(context, currentUserSid);
        if (user is null)
            throw new InvalidOperationException(SetupText.Get("CurrentUserAccountUnavailable"));
        if (!group.Members.Contains(user))
        {
            group.Members.Add(user);
            group.Save();
        }
    }

    private static GroupPrincipal CreateOperatorsGroup(PrincipalContext context)
    {
        GroupPrincipal group = new(context, Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME)
        {
            Description = SetupText.Get("OperatorsGroupDescription")
        };
        group.Save();
        return group;
    }

    /// <summary>
    /// 移除安裝程式建立之非提升權限操作人員本機群組。
    /// </summary>
    private static void RemoveOperatorsGroup()
    {
        using PrincipalContext context = new(ContextType.Machine);
        using GroupPrincipal? group = GroupPrincipal.FindByIdentity(context, Globals.IDDSCOMMUNITY_OPERATORS_GROUP_NAME);
        group?.Delete();
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);
    private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;
    /// <summary>
    /// 停止並註銷 Windows 服務、終止相關程序，並移除安裝檔案、防火牆規則與捷徑。
    /// </summary>
    internal static SetupOperationResult Uninstall(IProgress<SetupProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Report(progress, "ProgressStoppingService", 15);
        cancellationToken.ThrowIfCancellationRequested();
        ServiceStateSnapshot serviceState = CaptureServiceState();
        bool desktopShortcut = HasDesktopShortcut;
        bool startMenuShortcut = HasStartMenuShortcut;
        string parent = Directory.GetParent(InstallDirectory)?.FullName ?? throw new InvalidOperationException();
        string quarantineDirectory = Path.Combine(parent, ".idds-remove-" + Guid.NewGuid().ToString("N"));
        bool filesQuarantined = false;
        SetupRollbackJournal rollback = new();
        try
        {
            if (serviceState.Exists)
                rollback.Record(() => RestoreServiceState(serviceState));
            StopService(serviceState);
            KillRunningProcesses();
            cancellationToken.ThrowIfCancellationRequested();
            Report(progress, "ProgressRemovingFiles", 60);
            if (Directory.Exists(InstallDirectory))
            {
                rollback.Record(() =>
                {
                    if (Directory.Exists(quarantineDirectory) && !Directory.Exists(InstallDirectory))
                        MoveDirectoryWithRetry(quarantineDirectory, InstallDirectory, CancellationToken.None);
                });
                MoveDirectoryWithRetry(InstallDirectory, quarantineDirectory, cancellationToken);
                filesQuarantined = true;
            }
            rollback.Record(() => CreateShortcuts(desktopShortcut, startMenuShortcut));
            RemoveShortcuts();
            cancellationToken.ThrowIfCancellationRequested();
            if (serviceState.Exists)
            {
                RunSc("delete", ServiceName, acceptMissing: true);
            }
            rollback.Commit();
        }
        catch (Exception uninstallFailure)
        {
            try
            {
                rollback.RollBack();
            }
            catch (Exception rollbackFailure)
            {
                throw new AggregateException(SetupText.Get("RollbackFailed"), uninstallFailure, rollbackFailure);
            }
            throw;
        }

        bool cleanupIncomplete = false;
        bool restartRequired = false;
        try
        {
            CleanUpFirewallRules();
        }
        catch (Exception exception)
        {
            LogNonFatal("Clean Windows Firewall rules after uninstall", exception);
            cleanupIncomplete = true;
        }
        try
        {
            RemoveOperatorsGroup();
        }
        catch (Exception exception)
        {
            LogNonFatal("Remove operators group", exception);
            cleanupIncomplete = true;
        }
        if (filesQuarantined && Directory.Exists(quarantineDirectory))
        {
            try
            {
                restartRequired = SafeDeleteDirectory(quarantineDirectory);
            }
            catch (Exception exception)
            {
                LogNonFatal($"Clean uninstalled files {quarantineDirectory}", exception);
                cleanupIncomplete = true;
            }
        }
        Report(progress, "ProgressCompleted", 100);
        return new SetupOperationResult(restartRequired, cleanupIncomplete);
    }

    private static ServiceStateSnapshot CaptureServiceState()
    {
        ServiceController[] services = ServiceController.GetServices();
        try
        {
            ServiceController? service = services.FirstOrDefault(candidate =>
                string.Equals(candidate.ServiceName, ServiceName, StringComparison.OrdinalIgnoreCase));
            if (service is null) return new ServiceStateSnapshot(Exists: false, ServiceControllerStatus.Stopped);
            ServiceControllerStatus stableStatus = WaitForStableServiceStatus(service);
            return new ServiceStateSnapshot(Exists: true, stableStatus);
        }
        finally
        {
            foreach (ServiceController service in services) service.Dispose();
        }
    }

    private static void StopService(ServiceStateSnapshot state)
    {
        if (!state.Exists || state.Status == ServiceControllerStatus.Stopped) return;
        RunSc("stop", ServiceName, acceptMissing: true);
        using ServiceController controller = new(ServiceName);
        controller.WaitForStatus(ServiceControllerStatus.Stopped, ServiceStartTimeout);
        controller.Refresh();
        if (controller.Status != ServiceControllerStatus.Stopped)
            throw new InvalidOperationException(SetupText.Get("ServiceStopVerificationFailed"));
    }

    private static ServiceControllerStatus WaitForStableServiceStatus(ServiceController controller)
    {
        controller.Refresh();
        ServiceControllerStatus target = GetStableServiceStatusTarget(controller.Status);
        if (controller.Status == target) return target;
        controller.WaitForStatus(target, ServiceStartTimeout);
        controller.Refresh();
        if (controller.Status != target)
            throw new InvalidOperationException(SetupText.Get("ServiceStateStabilizationFailed"));
        return target;
    }

    internal static ServiceControllerStatus GetStableServiceStatusTarget(ServiceControllerStatus status) => status switch
    {
        ServiceControllerStatus.StartPending or ServiceControllerStatus.ContinuePending => ServiceControllerStatus.Running,
        ServiceControllerStatus.StopPending => ServiceControllerStatus.Stopped,
        ServiceControllerStatus.PausePending => ServiceControllerStatus.Paused,
        ServiceControllerStatus.Running or ServiceControllerStatus.Stopped or ServiceControllerStatus.Paused => status,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static void RestoreServiceState(ServiceStateSnapshot state)
    {
        if (!state.Exists || state.Status == ServiceControllerStatus.Stopped) return;
        StartServiceAndVerify();
        if (state.Status != ServiceControllerStatus.Paused) return;
        using ServiceController controller = new(ServiceName);
        controller.Pause();
        controller.WaitForStatus(ServiceControllerStatus.Paused, ServiceStartTimeout);
        controller.Refresh();
        if (controller.Status != ServiceControllerStatus.Paused)
            throw new InvalidOperationException(SetupText.Get("ServicePauseVerificationFailed"));
    }

    private static void ConfigureNewService(string executablePath)
    {
        RunSc("create", ServiceName, "binPath=", $"\"{executablePath}\"", "start=", "auto", "DisplayName=", ServiceDisplayName);
        RunSc("description", ServiceName, SetupText.Get("ServiceDescription"));
        RunSc("failure", ServiceName, "reset=", "86400", "actions=", "restart/5000/restart/15000/none/0");
    }

    private static void StartServiceAndVerify()
    {
        using (ServiceController current = new(ServiceName))
        {
            current.Refresh();
            if (current.Status == ServiceControllerStatus.Running) return;
        }
        RunSc("start", ServiceName);
        using ServiceController controller = new(ServiceName);
        controller.WaitForStatus(ServiceControllerStatus.Running, ServiceStartTimeout);
        controller.Refresh();
        if (controller.Status != ServiceControllerStatus.Running)
            throw new InvalidOperationException(SetupText.Get("ServiceStartVerificationFailed"));
    }

    private static void CleanUpFirewallRules()
    {
        ProcessStartInfo psi = new(Path.Combine(Environment.SystemDirectory, "netsh.exe"), "advfirewall firewall delete rule name=all group=\"IDDS Community\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using Process process = Process.Start(psi) ?? throw new InvalidOperationException(SetupText.Get("FirewallCleanupStartFailed"));
        if (!process.WaitForExit((int)ProcessTimeout.TotalMilliseconds))
        {
            process.Kill(true);
            throw new System.TimeoutException(SetupText.Get("FirewallCleanupTimedOut"));
        }
        if (process.ExitCode != 0)
            throw new InvalidOperationException(SetupText.Format("FirewallCleanupFailed", process.ExitCode));
    }

    private static void KillRunningProcesses()
    {
        try
        {
            using ServiceController sc = new(ServiceName);
            if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception exception)
        {
            LogNonFatal("Stop service before process cleanup", exception);
        }

        string[] targetProcessNames = ["IDDSCommunity.IntrusionDetection.Service", "IDDSCommunity.IntrusionDetection.Admin"];
        foreach (string name in targetProcessNames)
        {
            try
            {
                foreach (Process discoveredProcess in Process.GetProcessesByName(name))
                {
                    using Process p = discoveredProcess;
                    try
                    {
                        string? mainModulePath = null;
                        try
                        {
                            mainModulePath = p.MainModule?.FileName;
                        }
                        catch (System.ComponentModel.Win32Exception winEx)
                        {
                            LogNonFatal($"Query module path for {name} (PID {p.Id})", winEx);
                        }

                        if (mainModulePath == null || mainModulePath.StartsWith(InstallDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill(true);
                            if (!p.WaitForExit(3000))
                                throw new System.TimeoutException(SetupText.Format("ProcessStopTimedOut", name));
                        }
                    }
                    catch (Exception exception)
                    {
                        LogNonFatal($"Inspect or terminate process {name}", exception);
                    }
                }
            }
            catch (Exception exception)
            {
                LogNonFatal($"Enumerate process {name}", exception);
            }
        }
    }

    private static void TryCleanTemporaryDirectory(string directoryPath)
    {
        try
        {
            _ = SafeDeleteDirectory(directoryPath);
        }
        catch (Exception exception)
        {
            LogNonFatal($"Clean temporary directory {directoryPath}", exception);
        }
    }

    private static void LogNonFatal(string operation, Exception exception) =>
        _ = RollingDiagnosticLog.Write("Setup", operation, exception);

    internal static bool SafeDeleteDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return false;
        bool restartRequired = false;

        foreach (string file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            catch (Exception exception)
            {
                // Handle file locks (e.g. runtime DLLs like clrjit.dll) via Win32 delay until reboot
                try
                {
                    ScheduleDeleteAfterRestart(file, exception);
                    restartRequired = true;
                }
                catch (IOException) { throw; }
                catch (Exception schedulingFailure)
                {
                    throw new AggregateException(SetupText.Format("DeleteFailed", file), exception, schedulingFailure);
                }
            }
        }

        foreach (string subDir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
        {
            try
            {
                Directory.Delete(subDir, false);
            }
            catch (Exception exception) when (restartRequired)
            {
                ScheduleDeleteAfterRestart(subDir, exception);
            }
        }

        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch (Exception exception) when (restartRequired)
        {
            ScheduleDeleteAfterRestart(directoryPath, exception);
        }
        if (Directory.Exists(directoryPath) && !restartRequired)
            throw new IOException(SetupText.Format("DeleteFailed", directoryPath));
        return restartRequired;
    }

    private static void ScheduleDeleteAfterRestart(string path, Exception originalException)
    {
        if (!MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT))
            throw new IOException(SetupText.Format("DeleteFailed", path), originalException);
    }

    internal static void CopyDirectoryOverwrite(string source, string destination, CancellationToken cancellationToken)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException(SetupText.Get("ReparsePointRejected"));

        Directory.CreateDirectory(destination);

        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException(SetupText.Get("ReparsePointRejected"));
            string destFile = Path.Combine(destination, Path.GetRelativePath(source, file));
            if (File.Exists(destFile))
            {
                File.SetAttributes(destFile, FileAttributes.Normal);
            }
            File.Copy(file, destFile, true);
        }
    }

    internal static void MoveDirectoryWithRetry(string source, string destination, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        Exception? lastFailure = null;
        for (int attempt = 1; attempt <= DirectoryMoveAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastFailure = exception;
                if (!Directory.Exists(source) && Directory.Exists(destination))
                    return;
                if (attempt == DirectoryMoveAttempts)
                    break;
                cancellationToken.WaitHandle.WaitOne(DirectoryMoveRetryDelay);
            }
        }

        throw new IOException(SetupText.Format("DirectoryMoveFailed", source, destination, DirectoryMoveAttempts), lastFailure);
    }

    private static void RunSc(string command, string serviceName, params string[] arguments) => RunSc(command, serviceName, false, arguments);

    private static void RunSc(string command, string serviceName, bool acceptMissing, params string[] arguments)
    {
        ProcessStartInfo startInfo = new(Path.Combine(Environment.SystemDirectory, "sc.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add(command);
        startInfo.ArgumentList.Add(serviceName);
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException(SetupText.Get("ServiceControlStartFailed"));
        System.Threading.Tasks.Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        System.Threading.Tasks.Task<string> standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)ProcessTimeout.TotalMilliseconds))
        {
            process.Kill(true);
            throw new System.TimeoutException(SetupText.Get("ServiceControlTimedOut"));
        }
        if (process.ExitCode != 0 && !(acceptMissing && (process.ExitCode == 1060 || process.ExitCode == 1062)))
        {
            string details = standardError.GetAwaiter().GetResult().Trim();
            if (string.IsNullOrEmpty(details)) details = standardOutput.GetAwaiter().GetResult().Trim();
            throw new InvalidOperationException(SetupText.Format("ServiceControlFailedWithDetails", process.ExitCode, details));
        }
    }

    internal readonly record struct SetupProgress(string MessageKey, int Percentage);
    internal readonly record struct SetupOperationResult(bool RestartRequired, bool CleanupIncomplete);
    private readonly record struct ServiceStateSnapshot(bool Exists, ServiceControllerStatus Status);
}
