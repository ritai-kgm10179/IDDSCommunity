using System;
using System.IO;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證管理主控台（Admin Console）各設定面板中主開關與子控制項之連動停用（Enabled/Disabled）行為。
/// </summary>
[TestClass]
public sealed class AdminSettingsControlLinkageTest
{
    private string _tempDir = null!;

    /// <summary>
    /// 初始化測試資料庫環境。
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        Environment.SetEnvironmentVariable("IDDS_TEST_MODE", "1");
        _tempDir = Path.Combine(Path.GetTempPath(), "idds-linkage-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Database.Instance.Configure(_tempDir);
        LanguageManager.Instance.Initialize("zh-TW");
    }

    /// <summary>
    /// 清理測試資料庫連線與暫存目錄。
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        Database.Instance.Close();
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // 忽略清理失敗
        }
    }

    /// <summary>
    /// 驗證威脅情報與叢集聯防面板之角色切換、反向代理、地區封鎖與自動更新週期連動狀態。
    /// </summary>
    [STATestMethod]
    public void PanelThreatIntelligenceSettings_ControlsLinkage_WorksProperly()
    {
        using PanelThreatIntelligenceSettings panel = new();
        panel.LoadData();

        ComboBox role = Assert.IsInstanceOfType<ComboBox>(panel.Controls.Find("comboClusterRole", true)[0]);
        NumericUpDown grpc = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numGrpcPort", true)[0]);
        NumericUpDown hubPort = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numHubPort", true)[0]);
        TextBox endpoint = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtHubEndpoint", true)[0]);
        CheckBox revProxy = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkThreatHubReverseProxy", true)[0]);
        CheckBox loopback = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkThreatHubLoopbackOnly", true)[0]);

        // EdgeNode: gRPC 與 REST 埠皆須停用，端點啟用
        role.SelectedIndex = (int)ThreatHubRole.EdgeNode;
        Assert.IsFalse(grpc.Enabled, "邊緣防禦節點模式下，gRPC 連接埠必須停用。");
        Assert.IsFalse(hubPort.Enabled, "邊緣防禦節點模式下，REST 連接埠必須停用。");
        Assert.IsTrue(endpoint.Enabled, "邊緣防禦節點模式下，中繼端點必須啟用。");

        // ThreatHub: 反向代理未勾選時，僅限本機必須連動停用
        role.SelectedIndex = (int)ThreatHubRole.ThreatHub;
        Assert.IsTrue(grpc.Enabled, "中繼中心模式下，gRPC 連接埠必須啟用。");
        Assert.IsTrue(hubPort.Enabled, "中繼中心模式下，REST 連接埠必須啟用。");
        revProxy.Checked = false;
        Assert.IsFalse(loopback.Enabled, "未啟用反向代理時，僅限本機勾選框必須連動停用。");
        revProxy.Checked = true;
        Assert.IsTrue(loopback.Enabled, "啟用反向代理時，僅限本機勾選框必須啟用。");

        // 地區封鎖連動
        CheckBox geoBlock = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableGeoBlocking", true)[0]);
        TextBox blockedCountries = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtBlockedCountries", true)[0]);
        geoBlock.Checked = false;
        Assert.IsFalse(blockedCountries.Enabled, "未啟用地區封鎖時，國家名單輸入框必須停用。");
        geoBlock.Checked = true;
        Assert.IsTrue(blockedCountries.Enabled, "啟用地區封鎖時，國家名單輸入框必須啟用。");

        // GeoIP 自動更新天數連動
        CheckBox geoAuto = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableGeoIpAutoUpdate", true)[0]);
        NumericUpDown geoDays = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numGeoIpUpdateDays", true)[0]);
        geoAuto.Checked = false;
        Assert.IsFalse(geoDays.Enabled, "未啟用 GeoIP 自動更新時，更新週期天數必須停用。");
        geoAuto.Checked = true;
        Assert.IsTrue(geoDays.Enabled, "啟用 GeoIP 自動更新時，更新週期天數必須啟用。");
    }

    /// <summary>
    /// 驗證自助解鎖入口網站面板主開關與各子控制項之連動停用狀態。
    /// </summary>
    [STATestMethod]
    public void PanelSelfServiceSettings_ControlsLinkage_WorksProperly()
    {
        using PanelSelfServiceSettings panel = new();
        panel.LoadSettings();

        CheckBox enablePortal = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnablePortal", true)[0]);
        NumericUpDown port = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numPort", true)[0]);
        TextBox listenIp = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtListenIp", true)[0]);
        TextBox secret = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtTotpSecret", true)[0]);
        Button generate = Assert.IsInstanceOfType<Button>(panel.Controls.Find("btnGenerate", true)[0]);
        TextBox testCode = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtTestCode", true)[0]);
        Button verify = Assert.IsInstanceOfType<Button>(panel.Controls.Find("btnVerify", true)[0]);

        enablePortal.Checked = false;
        Assert.IsFalse(port.Enabled);
        Assert.IsFalse(listenIp.Enabled);
        Assert.IsFalse(secret.Enabled);
        Assert.IsFalse(generate.Enabled);
        Assert.IsFalse(testCode.Enabled);
        Assert.IsFalse(verify.Enabled);

        enablePortal.Checked = true;
        Assert.IsTrue(port.Enabled);
        Assert.IsTrue(listenIp.Enabled);
        Assert.IsTrue(secret.Enabled);
        Assert.IsTrue(generate.Enabled);
        Assert.IsTrue(testCode.Enabled);
        Assert.IsTrue(verify.Enabled);
    }

    /// <summary>
    /// 驗證多雲邊界安全聯防面板主開關與服務商下拉選單之連動停用狀態。
    /// </summary>
    [STATestMethod]
    public void PanelCloudPerimeterSettings_ControlsLinkage_WorksProperly()
    {
        using PanelCloudPerimeterSettings panel = new();
        panel.LoadSettings();

        CheckBox enableCloud = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableCloudPerimeter", true)[0]);
        ComboBox provider = Assert.IsInstanceOfType<ComboBox>(panel.Controls.Find("comboProviderType", true)[0]);
        TextBox apiKey = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtApiKey", true)[0]);
        Button testBtn = Assert.IsInstanceOfType<Button>(panel.Controls.Find("btnTestConnection", true)[0]);

        enableCloud.Checked = false;
        Assert.IsFalse(provider.Enabled);
        Assert.IsFalse(apiKey.Enabled);
        Assert.IsFalse(testBtn.Enabled);

        enableCloud.Checked = true;
        Assert.IsTrue(provider.Enabled);

        provider.SelectedIndex = 0; // None
        Assert.IsFalse(apiKey.Enabled);
        Assert.IsFalse(testBtn.Enabled);

        provider.SelectedIndex = 1; // AWS WAFv2
        Assert.IsTrue(apiKey.Enabled);
        Assert.IsTrue(testBtn.Enabled);
    }

    /// <summary>
    /// 驗證動態欺敵與管理 API 面板主開關與子控制項之連動停用狀態。
    /// </summary>
    [STATestMethod]
    public void PanelDeceptionAndApiSettings_ControlsLinkage_WorksProperly()
    {
        using PanelDeceptionAndApiSettings panel = new();
        panel.LoadSettings();

        CheckBox honey = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableHoneyAccounts", true)[0]);
        TextBox honeyText = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtHoneyAccounts", true)[0]);

        honey.Checked = false;
        Assert.IsFalse(honeyText.Enabled);
        honey.Checked = true;
        Assert.IsTrue(honeyText.Enabled);

        CheckBox api = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableManagementApi", true)[0]);
        NumericUpDown port = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numApiPort", true)[0]);
        TextBox apiKey = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("txtApiKey", true)[0]);
        Button genBtn = Assert.IsInstanceOfType<Button>(panel.Controls.Find("btnGenApiKey", true)[0]);

        api.Checked = false;
        Assert.IsFalse(port.Enabled);
        Assert.IsFalse(apiKey.Enabled);
        Assert.IsFalse(genBtn.Enabled);

        api.Checked = true;
        Assert.IsTrue(port.Enabled);
        Assert.IsTrue(apiKey.Enabled);
        Assert.IsTrue(genBtn.Enabled);
    }

    /// <summary>
    /// 驗證封鎖設定面板中永久硬封鎖與跨代理關聯分析之連動停用狀態。
    /// </summary>
    [STATestMethod]
    public void PanelLockoutConfiguration_ControlsLinkage_WorksProperly()
    {
        using PanelLockoutConfiguration panel = new();

        CheckBox lockForever = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxLockForever", true)[0]);
        TextBox hardDuration = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLockDuration", true)[0]);

        lockForever.Checked = true;
        Assert.IsFalse(hardDuration.Enabled, "永久硬封鎖勾選時，硬封鎖時長必須停用。");
        lockForever.Checked = false;
        Assert.IsTrue(hardDuration.Enabled, "未勾選永久硬封鎖時，硬封鎖時長必須啟用。");

        CheckBox correlation = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableCrossAgentCorrelation", true)[0]);
        NumericUpDown sprayAccount = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numericSprayAccountThreshold", true)[0]);
        NumericUpDown sprayIp = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numericSprayIpThreshold", true)[0]);
        NumericUpDown slidingWindow = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numericSlidingWindowMinutes", true)[0]);

        correlation.Checked = false;
        Assert.IsFalse(sprayAccount.Enabled);
        Assert.IsFalse(sprayIp.Enabled);
        Assert.IsFalse(slidingWindow.Enabled);

        correlation.Checked = true;
        Assert.IsTrue(sprayAccount.Enabled);
        Assert.IsTrue(sprayIp.Enabled);
        Assert.IsTrue(slidingWindow.Enabled);
    }

    /// <summary>
    /// 驗證通知設定面板中 Webhook、Syslog 與 Metrics 主開關之連動停用狀態。
    /// </summary>
    [STATestMethod]
    public void PanelNotificationSettings_ControlsLinkage_WorksProperly()
    {
        using PanelNotificationSettings panel = new();
        panel.LoadData();

        CheckBox webhook = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableWebhook", true)[0]);
        ComboBox platform = Assert.IsInstanceOfType<ComboBox>(panel.Controls.Find("comboBoxWebhookPlatform", true)[0]);
        TextBox url = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxWebhookUrl", true)[0]);
        Button testWebhook = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonTestWebhook", true)[0]);

        webhook.Checked = false;
        Assert.IsFalse(platform.Enabled);
        Assert.IsFalse(url.Enabled);
        Assert.IsFalse(testWebhook.Enabled);

        webhook.Checked = true;
        Assert.IsTrue(platform.Enabled);
        platform.SelectedIndex = 1; // Teams
        Assert.IsTrue(url.Enabled);
        Assert.IsTrue(testWebhook.Enabled);

        CheckBox syslog = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableSyslog", true)[0]);
        TextBox syslogHost = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxSyslogHost", true)[0]);
        NumericUpDown syslogPort = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numSyslogPort", true)[0]);
        Button testSyslog = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonTestSyslog", true)[0]);

        syslog.Checked = false;
        Assert.IsFalse(syslogHost.Enabled);
        Assert.IsFalse(syslogPort.Enabled);
        Assert.IsFalse(testSyslog.Enabled);

        syslog.Checked = true;
        Assert.IsTrue(syslogHost.Enabled);
        Assert.IsTrue(syslogPort.Enabled);
        Assert.IsTrue(testSyslog.Enabled);

        CheckBox metrics = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableMetrics", true)[0]);
        TextBox metricsIp = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxMetricsListenIp", true)[0]);
        NumericUpDown metricsPort = Assert.IsInstanceOfType<NumericUpDown>(panel.Controls.Find("numMetricsPort", true)[0]);

        metrics.Checked = false;
        Assert.IsFalse(metricsIp.Enabled);
        Assert.IsFalse(metricsPort.Enabled);

        metrics.Checked = true;
        Assert.IsTrue(metricsIp.Enabled);
        Assert.IsTrue(metricsPort.Enabled);
    }
}
