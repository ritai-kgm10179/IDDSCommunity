using System;
using IDDSCommunity.IntrusionDetection.Shared.Compliance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 CisBenchmarkScanner 合規評估掃描引擎功能。
/// </summary>
[TestClass]
public sealed class CisBenchmarkScannerTest
{
    /// <summary>
    /// 驗證 RunScan 能夠順利產出評估報告並計算合規分數。
    /// </summary>
    [TestMethod]
    public void RunScan_GeneratesValidReportAndScore()
    {
        CisBenchmarkResult report = CisBenchmarkScanner.RunScan();

        Assert.IsNotNull(report);
        Assert.IsTrue(report.TotalChecks >= 5, "Should evaluate multiple CIS benchmark items");
        Assert.IsTrue(report.PassedChecks >= 0);
        Assert.IsTrue(report.ComplianceScore >= 0.0 && report.ComplianceScore <= 100.0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(report.HostName));
    }
}
