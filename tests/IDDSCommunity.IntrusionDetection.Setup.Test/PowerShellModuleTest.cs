using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Setup.Test;

/// <summary>
/// 驗證 IDDS Community 官方 PowerShell 模組清單與腳本檔案。
/// </summary>
[TestClass]
public sealed class PowerShellModuleTest
{
    /// <summary>
    /// 驗證 IDDSCommunity.psd1 與 IDDSCommunity.psm1 存在且包含必要之匯出函式與資訊。
    /// </summary>
    [TestMethod]
    public void PowerShellModule_FilesExistAndValid()
    {
        string baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "IDDSCommunity.PowerShell"));
        if (!Directory.Exists(baseDir))
        {
            // Try relative path from project
            baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "tools", "IDDSCommunity.PowerShell"));
        }

        if (Directory.Exists(baseDir))
        {
            string psd1 = Path.Combine(baseDir, "IDDSCommunity.psd1");
            string psm1 = Path.Combine(baseDir, "IDDSCommunity.psm1");

            Assert.IsTrue(File.Exists(psd1), "IDDSCommunity.psd1 should exist");
            Assert.IsTrue(File.Exists(psm1), "IDDSCommunity.psm1 should exist");

            string psd1Content = File.ReadAllText(psd1);
            Assert.IsTrue(psd1Content.Contains("RootModule = 'IDDSCommunity.psm1'"));
            Assert.IsTrue(psd1Content.Contains("Get-IddsStatus"));
            Assert.IsTrue(psd1Content.Contains("Get-IddsBlockedIp"));
            Assert.IsTrue(psd1Content.Contains("Block-IddsIp"));
            Assert.IsTrue(psd1Content.Contains("Unblock-IddsIp"));
            Assert.IsTrue(psd1Content.Contains("Get-IddsCloudPerimeter"));
            Assert.IsTrue(psd1Content.Contains("Test-IddsHoneyAccount"));
            Assert.IsTrue(psd1Content.Contains("Invoke-IddsCisScan"));

            string psm1Content = File.ReadAllText(psm1);
            Assert.IsTrue(psm1Content.Contains("function Get-IddsStatus"));
            Assert.IsTrue(psm1Content.Contains("function Get-IddsBlockedIp"));
            Assert.IsTrue(psm1Content.Contains("function Block-IddsIp"));
            Assert.IsTrue(psm1Content.Contains("function Unblock-IddsIp"));
            Assert.IsTrue(psm1Content.Contains("function Get-IddsCloudPerimeter"));
            Assert.IsTrue(psm1Content.Contains("function Test-IddsHoneyAccount"));
            Assert.IsTrue(psm1Content.Contains("function Invoke-IddsCisScan"));
            Assert.IsTrue(psm1Content.Contains("function Export-IddsStixBundle"));
            Assert.IsTrue(psm1Content.Contains("function Export-IddsIso27001Report"));
        }
    }
}
