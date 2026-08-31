using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供跨主機威脅聯防、外部情資訂閱、雙層 Bogon 防護與智慧假釋參數設定之管理主控台面板。
/// </summary>
public sealed class PanelThreatIntelligenceSettings : UserControl
{
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);

    // Section 1: Cluster
    private readonly ComboBox comboClusterRole;
    private readonly TextBox txtHubEndpoint;
    private readonly TextBox txtHubApiKey;
    private readonly NumericUpDown numHubPort;
    private readonly NumericUpDown numSyncInterval;

    // Section 2: External Feeds
    private readonly CheckBox chkEnableFeeds;
    private readonly NumericUpDown numFeedInterval;
    private readonly NumericUpDown numIpsumLevel;
    private readonly NumericUpDown numFeedTtlDays;
    private readonly TextBox txtAbuseApiKey;
    private readonly NumericUpDown numAbuseMinConfidence;
    private readonly TextBox txtCustomUrls;

    // Section 3: Bogon & Probation
    private readonly CheckBox chkEnableDynamicBogon;
    private readonly TextBox txtBogonIpv4Url;
    private readonly TextBox txtBogonIpv6Url;
    private readonly NumericUpDown numProbationDays;

    /// <summary>
    /// 當威脅情報與叢集聯防設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? ThreatIntelligenceSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelThreatIntelligenceSettings"/> 類別之新執行個體。
    /// </summary>
    public PanelThreatIntelligenceSettings()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        AutoScroll = true;

        Font defaultFont = new("Segoe UI", 9F);
        Font sectionHeaderFont = new("Segoe UI", 10F, FontStyle.Bold);

        int leftMargin = 15;
        int currentY = 10;

        // Page Header
        SmartLabel pageTitle = CreateHeaderLabel(Strings.Get("Threat intelligence and cluster"), 11F, AccentColor, new Point(11, currentY));
        Controls.Add(pageTitle);
        currentY += 32;

        // === Section 1: Cluster Topology ===
        Label lblSectionCluster = CreateHeaderLabel(Strings.Get("Cluster topology & Threat Hub"), 10F, AccentColor, new Point(leftMargin, currentY));
        Controls.Add(lblSectionCluster);
        currentY += 24;

        Label lblRole = CreateFieldLabel(Strings.Get("Cluster node role"), new Point(leftMargin, currentY));
        comboClusterRole = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(220, 23)
        };
        comboClusterRole.Items.AddRange(["Standalone (0)", "EdgeNode (1)", "ThreatHub (2)"]);
        Controls.Add(lblRole);
        Controls.Add(comboClusterRole);

        Label lblEndpoint = CreateFieldLabel(Strings.Get("Threat Hub endpoint URL"), new Point(250, currentY));
        txtHubEndpoint = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(250, currentY + 18),
            Size = new Size(240, 23)
        };
        Controls.Add(lblEndpoint);
        Controls.Add(txtHubEndpoint);
        currentY += 48;

        Label lblApiKey = CreateFieldLabel(Strings.Get("Cluster API key"), new Point(leftMargin, currentY));
        txtHubApiKey = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(220, 23)
        };
        Controls.Add(lblApiKey);
        Controls.Add(txtHubApiKey);

        Label lblPort = CreateFieldLabel(Strings.Get("Threat Hub port"), new Point(250, currentY));
        numHubPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(250, currentY + 18),
            Size = new Size(100, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 8443
        };
        Controls.Add(lblPort);
        Controls.Add(numHubPort);

        Label lblSync = CreateFieldLabel(Strings.Get("Cluster sync interval (seconds)"), new Point(370, currentY));
        numSyncInterval = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(370, currentY + 18),
            Size = new Size(120, 23),
            Minimum = 5,
            Maximum = 3600,
            Value = 60
        };
        Controls.Add(lblSync);
        Controls.Add(numSyncInterval);
        currentY += 54;

        // === Section 2: External Threat Feeds ===
        Label lblSectionFeeds = CreateHeaderLabel(Strings.Get("External threat feeds subscription"), 10F, AccentColor, new Point(leftMargin, currentY));
        Controls.Add(lblSectionFeeds);
        currentY += 24;

        chkEnableFeeds = new CheckBox
        {
            Text = Strings.Get("Enable automated threat feed subscription"),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY),
            AutoSize = true
        };
        Controls.Add(chkEnableFeeds);
        currentY += 26;

        Label lblFeedInterval = CreateFieldLabel(Strings.Get("Feed update interval (hours)"), new Point(leftMargin, currentY));
        numFeedInterval = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(100, 23),
            Minimum = 1,
            Maximum = 168,
            Value = 24
        };
        Controls.Add(lblFeedInterval);
        Controls.Add(numFeedInterval);

        Label lblIpsumLevel = CreateFieldLabel(Strings.Get("IPsum minimum severity level (1-8)"), new Point(135, currentY));
        numIpsumLevel = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(135, currentY + 18),
            Size = new Size(100, 23),
            Minimum = 1,
            Maximum = 8,
            Value = 3
        };
        Controls.Add(lblIpsumLevel);
        Controls.Add(numIpsumLevel);

        Label lblFeedTtl = CreateFieldLabel(Strings.Get("Threat intelligence TTL (days)"), new Point(255, currentY));
        numFeedTtlDays = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(255, currentY + 18),
            Size = new Size(100, 23),
            Minimum = 1,
            Maximum = 365,
            Value = 7
        };
        Controls.Add(lblFeedTtl);
        Controls.Add(numFeedTtlDays);
        currentY += 48;

        Label lblAbuseKey = CreateFieldLabel(Strings.Get("AbuseIPDB API key"), new Point(leftMargin, currentY));
        txtAbuseApiKey = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(280, 23)
        };
        Controls.Add(lblAbuseKey);
        Controls.Add(txtAbuseApiKey);

        Label lblAbuseMin = CreateFieldLabel(Strings.Get("AbuseIPDB minimum confidence (%)"), new Point(310, currentY));
        numAbuseMinConfidence = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(310, currentY + 18),
            Size = new Size(100, 23),
            Minimum = 25,
            Maximum = 100,
            Value = 90
        };
        Controls.Add(lblAbuseMin);
        Controls.Add(numAbuseMinConfidence);
        currentY += 48;

        Label lblCustomUrls = CreateFieldLabel(Strings.Get("Custom threat feed URLs (one per line)"), new Point(leftMargin, currentY));
        txtCustomUrls = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(475, 48),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        Controls.Add(lblCustomUrls);
        Controls.Add(txtCustomUrls);
        currentY += 74;

        // === Section 3: Bogon & Probation ===
        Label lblSectionBogon = CreateHeaderLabel(Strings.Get("Bogon & probation guardrails"), 10F, AccentColor, new Point(leftMargin, currentY));
        Controls.Add(lblSectionBogon);
        currentY += 24;

        chkEnableDynamicBogon = new CheckBox
        {
            Text = Strings.Get("Enable Team Cymru Fullbogons dynamic updates"),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY),
            AutoSize = true
        };
        Controls.Add(chkEnableDynamicBogon);

        Label lblProbationDays = CreateFieldLabel(Strings.Get("Probation decay period (days)"), new Point(340, currentY - 4));
        numProbationDays = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(340, currentY + 16),
            Size = new Size(100, 23),
            Minimum = 1,
            Maximum = 365,
            Value = 90
        };
        Controls.Add(lblProbationDays);
        Controls.Add(numProbationDays);
        currentY += 46;

        Label lblBogonV4 = CreateFieldLabel(Strings.Get("Dynamic Bogon IPv4 list URL"), new Point(leftMargin, currentY));
        txtBogonIpv4Url = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(475, 23)
        };
        Controls.Add(lblBogonV4);
        Controls.Add(txtBogonIpv4Url);
        currentY += 46;

        Label lblBogonV6 = CreateFieldLabel(Strings.Get("Dynamic Bogon IPv6 list URL"), new Point(leftMargin, currentY));
        txtBogonIpv6Url = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(475, 23)
        };
        Controls.Add(lblBogonV6);
        Controls.Add(txtBogonIpv6Url);
        currentY += 52;

        // Action Buttons
        Button btnSave = new()
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY),
            Size = new Size(102, 26),
            Text = Strings.Get("&Save"),
            UseVisualStyleBackColor = false
        };
        btnSave.Click += SaveSettings;
        Controls.Add(btnSave);

        SettingsResetButtonFactory.AddTo(
            this,
            (_, _) => ResetToDefaults(),
            new Point(leftMargin + 108, currentY));

        LoadData();
    }

    /// <summary>
    /// 自全域組態讀取設定並載入至使用者介面控制項中。
    /// </summary>
    public void LoadData()
    {
        IddsConfig config = IddsConfig.Instance;

        comboClusterRole.SelectedIndex = (int)config.ThreatHubRole;
        txtHubEndpoint.Text = config.ThreatHubEndpoint;
        txtHubApiKey.Text = config.ThreatHubApiKey;
        numHubPort.Value = Math.Clamp(config.ThreatHubPort, 1, 65535);
        numSyncInterval.Value = Math.Clamp(config.ThreatHubSyncIntervalSeconds, 5, 3600);

        chkEnableFeeds.Checked = config.EnableExternalThreatFeeds;
        numFeedInterval.Value = Math.Clamp(config.ThreatFeedUpdateIntervalHours, 1, 168);
        numIpsumLevel.Value = Math.Clamp(config.ThreatFeedMinLevel, 1, 8);
        numFeedTtlDays.Value = Math.Clamp(config.ThreatFeedTtlDays, 1, 365);
        txtAbuseApiKey.Text = config.AbuseIpDbApiKey;
        numAbuseMinConfidence.Value = Math.Clamp(config.AbuseIpDbMinConfidence, 25, 100);
        txtCustomUrls.Text = config.ThreatFeedCustomUrls;

        chkEnableDynamicBogon.Checked = config.EnableDynamicBogonUpdate;
        txtBogonIpv4Url.Text = config.DynamicBogonIpv4Url;
        txtBogonIpv6Url.Text = config.DynamicBogonIpv6Url;
        numProbationDays.Value = Math.Clamp(config.ProbationDecayDays, 1, 365);
    }

    private const string DefaultBogonV4 = "https://www.team-cymru.com/Services/Bogons/fullbogons-ipv4.txt";
    private const string DefaultBogonV6 = "https://www.team-cymru.com/Services/Bogons/fullbogons-ipv6.txt";

    private void SaveSettings(object? sender, EventArgs e)
    {
        IddsConfig config = IddsConfig.Instance;

        config.ThreatHubRole = (ThreatHubRole)Math.Clamp(comboClusterRole.SelectedIndex, 0, 2);
        config.ThreatHubEndpoint = txtHubEndpoint.Text.Trim();
        config.ThreatHubApiKey = txtHubApiKey.Text.Trim();
        config.ThreatHubPort = (int)numHubPort.Value;
        config.ThreatHubSyncIntervalSeconds = (int)numSyncInterval.Value;

        config.EnableExternalThreatFeeds = chkEnableFeeds.Checked;
        config.ThreatFeedUpdateIntervalHours = (int)numFeedInterval.Value;
        config.ThreatFeedMinLevel = (int)numIpsumLevel.Value;
        config.ThreatFeedTtlDays = (int)numFeedTtlDays.Value;
        config.AbuseIpDbApiKey = txtAbuseApiKey.Text.Trim();
        config.AbuseIpDbMinConfidence = (int)numAbuseMinConfidence.Value;
        config.ThreatFeedCustomUrls = txtCustomUrls.Text.Trim();

        config.EnableDynamicBogonUpdate = chkEnableDynamicBogon.Checked;
        config.DynamicBogonIpv4Url = txtBogonIpv4Url.Text.Trim();
        config.DynamicBogonIpv6Url = txtBogonIpv6Url.Text.Trim();
        config.ProbationDecayDays = (int)numProbationDays.Value;

        config.Save();
        ThreatIntelligenceSettingsChanged?.Invoke(this, EventArgs.Empty);

        MessageBox.Show(
            Strings.Get("Configuration was saved successfully."),
            Strings.AppTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ResetToDefaults()
    {
        comboClusterRole.SelectedIndex = (int)ThreatHubRole.Standalone;
        txtHubEndpoint.Text = string.Empty;
        txtHubApiKey.Text = Guid.NewGuid().ToString("N");
        numHubPort.Value = 8443;
        numSyncInterval.Value = 60;

        chkEnableFeeds.Checked = false;
        numFeedInterval.Value = 24;
        numIpsumLevel.Value = 3;
        numFeedTtlDays.Value = 7;
        txtAbuseApiKey.Text = string.Empty;
        numAbuseMinConfidence.Value = 90;
        txtCustomUrls.Text = string.Empty;

        chkEnableDynamicBogon.Checked = true;
        txtBogonIpv4Url.Text = DefaultBogonV4;
        txtBogonIpv6Url.Text = DefaultBogonV6;
        numProbationDays.Value = 90;
    }

    private static SmartLabel CreateHeaderLabel(string text, float fontSize, Color color, Point location) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", fontSize),
        ForeColor = color,
        Location = location,
        Text = text
    };

    private static Label CreateFieldLabel(string text, Point location) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 9F),
        ForeColor = BodyTextColor,
        Location = location,
        Text = text
    };
}
