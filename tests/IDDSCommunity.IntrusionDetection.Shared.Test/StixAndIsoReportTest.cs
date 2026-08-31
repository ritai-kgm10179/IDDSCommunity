using System;
using System.Collections.Generic;
using System.Text.Json;
using IDDSCommunity.IntrusionDetection.Shared.Reports;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 STIX 2.1 匯出器與 ISO/IEC 27001:2022 稽核報表引擎。
/// </summary>
[TestClass]
public sealed class StixAndIsoReportTest
{
    /// <summary>
    /// 驗證 StixBundleExporter 正確產製符標準之 STIX 2.1 JSON 物件。
    /// </summary>
    [TestMethod]
    public void StixBundleExporter_ExportBundle_GeneratesValidStix21Json()
    {
        var items = new List<StixExportItem>
        {
            new()
            {
                IpAddress = "198.51.100.77",
                EventTimeUtc = new DateTime(2026, 8, 31, 4, 0, 0, DateTimeKind.Utc),
                Description = "RDP brute force attack detected",
                ConfidenceScore = 90,
                AgentName = "TerminalServer"
            },
            new()
            {
                IpAddress = "2001:db8::dead:beef",
                EventTimeUtc = new DateTime(2026, 8, 31, 4, 15, 0, DateTimeKind.Utc),
                Description = "OpenSSH auth failure",
                ConfidenceScore = 85,
                AgentName = "OpenSsh"
            }
        };

        Guid bundleGuid = Guid.NewGuid();
        string json = StixBundleExporter.ExportBundle(items, bundleGuid);

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("bundle", root.GetProperty("type").GetString());
        Assert.AreEqual($"bundle--{bundleGuid}", root.GetProperty("id").GetString());

        var objects = root.GetProperty("objects");
        Assert.IsTrue(objects.GetArrayLength() >= 3); // identity, 2 indicators, report
    }

    /// <summary>
    /// 驗證 Iso27001ComplianceReportGenerator 正確產製 Annex A 控制措施合規報告。
    /// </summary>
    [TestMethod]
    public void Iso27001ComplianceReportGenerator_GenerateHtmlReport_ContainsAnnexAControls()
    {
        var stats = new Iso27001ReportStats
        {
            TotalBlockedIps = 1250,
            ActiveFirewallRules = 48,
            ThreatFeedIndicatorsCount = 50000,
            HoneypotProbeCount = 120
        };

        string html = Iso27001ComplianceReportGenerator.GenerateHtmlReport(stats);

        Assert.IsNotNull(html);
        Assert.IsTrue(html.Contains("ISO/IEC 27001:2022", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.5.7", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.8.7", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.8.15", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.8.16", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.8.20", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("A.8.24", StringComparison.Ordinal));
        Assert.IsTrue(html.Contains("1,250", StringComparison.Ordinal));
    }
}
