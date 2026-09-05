using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service.Notifications;

/// <summary>
/// 提供 SOAR 自動化自訂處置腳本 (PowerShell / CMD) 背景非同步執行器。
/// </summary>
public sealed class SoarRemediationExecutor
{
    private readonly IddsConfig configuration;

    /// <summary>
    /// 初始化 <see cref="SoarRemediationExecutor"/> 類別的新執行個體。
    /// </summary>
    /// <param name="configuration">全域組態執行個體。</param>
    public SoarRemediationExecutor(IddsConfig configuration)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 依據資安事件非同步執行配置之處置腳本。
    /// </summary>
    /// <param name="lockType">鎖定類型。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="agentName">觸發之代理程式名稱。</param>
    /// <param name="details">詳細事件描述。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    public async Task<bool> ExecuteScriptAsync(
        LockType lockType,
        string ipAddress,
        string agentName,
        string details,
        CancellationToken cancellationToken = default)
    {
        string scriptPath = configuration.SoarRemediationScriptPath;
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            return false;
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            ProcessStartInfo startInfo = CreateStartInfo(scriptPath, lockType, ipAddress, agentName, details);
            using Process? process = Process.Start(startInfo);
            if (process is null) return false;
            Task output = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null, deadline.Token);
            Task error = process.StandardError.BaseStream.CopyToAsync(Stream.Null, deadline.Token);
            try
            {
                await Task.WhenAll(process.WaitForExitAsync(deadline.Token), output, error).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            WindowsLogManager.Instance.WriteEntry($"[SOAR] Script execution failed: {ex.GetType().Name}",
                EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            return false;
        }
    }

    internal static ProcessStartInfo CreateStartInfo(string scriptPath, LockType lockType, string ipAddress, string agentName, string details)
    {
        string extension = Path.GetExtension(scriptPath).ToLowerInvariant();
        ProcessStartInfo info = new()
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (extension == ".ps1")
        {
            string pwsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe");
            info.FileName = File.Exists(pwsh) ? pwsh : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            foreach (string value in new[] { "-NoProfile", "-NonInteractive", "-File", Path.GetFullPath(scriptPath), "-IpAddress", ipAddress, "-LockType", lockType.ToString(), "-Agent", agentName, "-Details", details })
                info.ArgumentList.Add(value);
        }
        else if (extension is ".cmd" or ".bat")
        {
            // 批次檔由固定命令啟動；事件資料只透過環境變數傳遞，避免 shell 展開。
            if (scriptPath.Contains('"') || scriptPath.Contains('%')) throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unsupported batch script path."), nameof(scriptPath));
            info.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            info.Arguments = $"/d /s /c \"\"{Path.GetFullPath(scriptPath)}\"\"";
        }
        else
        {
            info.FileName = Path.GetFullPath(scriptPath);
            foreach (string value in new[] { ipAddress, lockType.ToString(), agentName, details }) info.ArgumentList.Add(value);
        }
        info.Environment["IDDS_IP_ADDRESS"] = ipAddress;
        info.Environment["IDDS_LOCK_TYPE"] = lockType.ToString();
        info.Environment["IDDS_AGENT"] = agentName;
        info.Environment["IDDS_DETAILS"] = details;
        return info;
    }
}