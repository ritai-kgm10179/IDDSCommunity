using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Service.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 SOAR 大量輸出排空及取消後程序終止。
/// </summary>
[TestClass]
public sealed class SoarRemediationExecutorTest
{
    /// <summary>
    /// 同時大量寫入 stdout 及 stderr 不造成管線互鎖。
    /// </summary>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    public async Task LargeOutputCompletesWithoutPipeDeadlock()
    {
        string path = Path.Combine(Path.GetTempPath(), "idds-soar-" + Guid.NewGuid().ToString("N") + ".ps1");
        try
        {
            File.WriteAllText(path, "param($IpAddress,$LockType,$Agent,$Details)\r\n$s='x'*1024\r\nfor($i=0;$i -lt 2048;$i++){[Console]::Out.WriteLine($s);[Console]::Error.WriteLine($s)}\r\nexit 0\r\n", new System.Text.UTF8Encoding(true));
            var config = IddsConfig.GetDefaultConfiguration();
            config.SoarRemediationScriptPath = path;
            Assert.IsTrue(await new SoarRemediationExecutor(config).ExecuteScriptAsync(LockType.HardLock, "8.8.8.8", "test", "test"));
        }
        finally { File.Delete(path); }
    }

    /// <summary>
    /// 取消等待會終止執行中的處置程序。
    /// </summary>
    /// <returns>非同步測試作業。</returns>
    [TestMethod]
    public async Task CancellationTerminatesProcess()
    {
        string path = Path.Combine(Path.GetTempPath(), "idds-soar-" + Guid.NewGuid().ToString("N") + ".ps1");
        string pidPath = path + ".pid";
        try
        {
            File.WriteAllText(path, "param($IpAddress,$LockType,$Agent,$Details)\r\n[IO.File]::WriteAllText($PSCommandPath+'.pid',[string]$PID)\r\nStart-Sleep -Seconds 60\r\n", new System.Text.UTF8Encoding(true));
            var config = IddsConfig.GetDefaultConfiguration();
            config.SoarRemediationScriptPath = path;
            using var cancel = new CancellationTokenSource();
            Task<bool> execution = new SoarRemediationExecutor(config).ExecuteScriptAsync(LockType.HardLock, "8.8.8.8", "test", "test", cancel.Token);
            try
            {
                for (int i = 0; i < 100 && !File.Exists(pidPath); i++) await Task.Delay(50);
                Assert.IsTrue(File.Exists(pidPath), "處置程序未在期限內啟動。");
            }
            finally { cancel.Cancel(); }
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await execution);
            int pid = int.Parse(File.ReadAllText(pidPath));
            try { using Process process = Process.GetProcessById(pid); Assert.IsTrue(process.HasExited); }
            catch (ArgumentException) { }
        }
        finally { File.Delete(path); File.Delete(pidPath); }
    }

    /// <summary>
    /// 批次檔參數使用環境變數，特殊字元不進入命令列。
    /// </summary>
    [TestMethod]
    public void BatchInputDoesNotEnterShellCommand()
    {
        var start = SoarRemediationExecutor.CreateStartInfo(Path.Combine(Path.GetTempPath(), "safe.cmd"), LockType.HardLock, "8.8.8.8", "agent & calc", "$(whoami) & echo bad");
        Assert.IsFalse(start.Arguments.Contains("whoami", StringComparison.Ordinal));
        Assert.AreEqual("$(whoami) & echo bad", start.Environment["IDDS_DETAILS"]);
    }
}