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

        return await Task.Run(() =>
        {
            try
            {
                string ext = Path.GetExtension(scriptPath).ToLowerInvariant();
                ProcessStartInfo psi;

                if (ext == ".ps1")
                {
                    // 優先使用 pwsh，若無則降級 powershell
                    string exeName = "pwsh.exe";
                    psi = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -IpAddress \"{ipAddress}\" -LockType \"{lockType}\" -Agent \"{agentName}\" -Details \"{details}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }
                else
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        Arguments = $"\"{ipAddress}\" \"{lockType}\" \"{agentName}\" \"{details}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }

                using var process = Process.Start(psi);
                if (process == null) return false;

                if (!process.WaitForExit(15000))
                {
                    try { process.Kill(); } catch { }
                    return false;
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                WindowsLogManager.Instance.WriteEntry($"[SOAR] Script execution failed: {ex.Message}",
                    EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                return false;
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
