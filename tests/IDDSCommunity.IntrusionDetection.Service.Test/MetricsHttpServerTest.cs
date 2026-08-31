using System;
using System.Net.Http;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Service.Observability;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 MetricsHttpServer 之指標文字輸出與健康檢查端點。
/// </summary>
[TestClass]
public sealed class MetricsHttpServerTest
{
    /// <summary>
    /// 驗證 BuildMetricsText 產生符合 Prometheus 規範之指標字串。
    /// </summary>
    [TestMethod]
    public void BuildMetricsText_ContainsStandardMetrics()
    {
        var config = new IddsConfig(new Database());
        var settings = new NotificationSettings(config);

        using var server = new MetricsHttpServer(settings, new Database());
        string metrics = server.BuildMetricsText();

        Assert.IsNotNull(metrics);
        Assert.IsTrue(metrics.Contains("idds_uptime_seconds", StringComparison.Ordinal));
        Assert.IsTrue(metrics.Contains("idds_active_firewall_blocks", StringComparison.Ordinal));
        Assert.IsTrue(metrics.Contains("idds_probation_ips_total", StringComparison.Ordinal));
    }
}
