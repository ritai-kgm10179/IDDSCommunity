namespace IDDSCommunity.IntrusionDetection.Setup.Test;

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// 驗證正式安裝包的擴充元件發布契約。
/// </summary>
[TestClass]
public sealed class PluginPackagingContractTest
{
    /// <summary>
    /// 驗證 WinRM 與遠端桌面閘道 Agent 均納入正式封裝流程。
    /// </summary>
    [TestMethod]
    public void BuildSetup_IncludesPhase1AAgents()
    {
        string repositoryRoot = FindRepositoryRoot();
        string script = File.ReadAllText(Path.Combine(repositoryRoot, "build-setup.ps1"));

        StringAssert.Contains(script, "'IDDSCommunity.Agents.WinRm'");
        StringAssert.Contains(script, "'IDDSCommunity.Agents.RemoteDesktopGateway'");
    }

    /// <summary>
    /// 驗證正式安裝包包含部署機器使用的唯讀資料庫診斷工具。
    /// </summary>
    [TestMethod]
    public void BuildSetup_IncludesDatabaseDiagnosticsTool()
    {
        string repositoryRoot = FindRepositoryRoot();
        string script = File.ReadAllText(Path.Combine(repositoryRoot, "build-setup.ps1"));

        StringAssert.Contains(script, "tools\\IDDSCommunity.DatabaseDiagnostics\\IDDSCommunity.DatabaseDiagnostics.csproj");
        StringAssert.Contains(script, "Tools\\DatabaseDiagnostics");
    }

    /// <summary>
    /// 驗證封鎖設定的儲存與放棄按鈕使用版面容器維持明確間距。
    /// </summary>
    [TestMethod]
    public void LockoutConfiguration_UsesLayoutManagedActionButtonSpacing()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "IDDSCommunity.IntrusionDetection.Admin",
            "PanelLockoutConfiguration.cs"));

        StringAssert.Contains(source, "actionButtonsLayout.Controls.Add(buttonSave)");
        StringAssert.Contains(source, "actionButtonsLayout.Controls.Add(buttonDiscard)");
        StringAssert.Contains(source, "buttonDiscard.Margin = new Padding(12, 0, 0, 0)");
    }

    /// <summary>
    /// 驗證安全性紀錄與首頁採用一致的最近三十天顯示範圍。
    /// </summary>
    [TestMethod]
    public void AdminSecurityLog_UsesThirtyDayWindow()
    {
        string repositoryRoot = FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "IDDSCommunity.IntrusionDetection.Admin",
            "IddsAdmin.cs"));

        StringAssert.Contains(source, "SecurityLogWindow = TimeSpan.FromDays(30)");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IDDSCommunity.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("找不到包含 IDDSCommunity.slnx 的儲存庫根目錄。");
    }
}
