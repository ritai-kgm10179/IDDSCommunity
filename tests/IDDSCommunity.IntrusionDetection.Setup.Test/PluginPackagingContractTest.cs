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
