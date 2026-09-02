using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供分散式威脅情資中繼 (Threat Hub)、外部威脅名單訂閱 (IPsum, AbuseIPDB) 與 Bogon 動態過濾配置面板。
/// </summary>
public sealed class PanelThreatIntelligenceSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    // Section 1: Topology
    private readonly ComboBox comboClusterRole;
    private readonly TextBox txtHubEndpoint;
    private readonly TextBox txtHubApiKey;
    private readonly NumericUpDown numHubPort;
    private readonly NumericUpDown numSyncInterval;

    // Section 2: Threat Feeds
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

    // Section 4: GeoIP
    private readonly CheckBox chkEnableGeoBlocking;
    private readonly TextBox txtBlockedCountries;
    private readonly CheckBox chkEnableGeoIpAutoUpdate;
    private readonly TextBox txtGeoIpDatabaseIpv4Url;
    private readonly TextBox txtGeoIpDatabaseIpv6Url;
    private readonly TextBox txtGeoIpLocalFilePath;
    private readonly Button btnBrowseGeoIpFile;
    private readonly NumericUpDown numGeoIpUpdateDays;
    private readonly Button btnUpdateGeoIpNow;
    private readonly Label lblGeoIpStatus;

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
        int controlWidth = 380;
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
            Size = new Size(controlWidth, 23)
        };
        comboClusterRole.Items.AddRange(["Standalone (0)", "EdgeNode (1)", "ThreatHub (2)"]);
        comboClusterRole.SelectedIndexChanged += (_, _) => UpdateClusterControlsState();
        Controls.Add(lblRole);
        Controls.Add(comboClusterRole);
        currentY += 46;

        Label lblEndpoint = CreateFieldLabel(Strings.Get("Threat Hub endpoint URL"), new Point(leftMargin, currentY));
        txtHubEndpoint = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblEndpoint);
        Controls.Add(txtHubEndpoint);
        currentY += 46;

        Label lblApiKey = CreateFieldLabel(Strings.Get("Cluster API key"), new Point(leftMargin, currentY));
        txtHubApiKey = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblApiKey);
        Controls.Add(txtHubApiKey);
        currentY += 46;

        Label lblPort = CreateFieldLabel(Strings.Get("Threat Hub port"), new Point(leftMargin, currentY));
        numHubPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(180, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 8443
        };
        Controls.Add(lblPort);
        Controls.Add(numHubPort);

        Label lblSync = CreateFieldLabel(Strings.Get("Cluster sync interval (seconds)"), new Point(leftMargin + 195, currentY));
        numSyncInterval = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin + 195, currentY + 18),
            Size = new Size(185, 23),
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
            Size = new Size(180, 23),
            Minimum = 1,
            Maximum = 168,
            Value = 24
        };
        Controls.Add(lblFeedInterval);
        Controls.Add(numFeedInterval);

        Label lblIpsumLevel = CreateFieldLabel(Strings.Get("IPsum minimum severity level (1-8)"), new Point(leftMargin + 195, currentY));
        numIpsumLevel = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin + 195, currentY + 18),
            Size = new Size(185, 23),
            Minimum = 1,
            Maximum = 8,
            Value = 3
        };
        Controls.Add(lblIpsumLevel);
        Controls.Add(numIpsumLevel);
        currentY += 46;

        Label lblFeedTtl = CreateFieldLabel(Strings.Get("Threat intelligence TTL (days)"), new Point(leftMargin, currentY));
        numFeedTtlDays = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(180, 23),
            Minimum = 1,
            Maximum = 365,
            Value = 7
        };
        Controls.Add(lblFeedTtl);
        Controls.Add(numFeedTtlDays);

        Label lblAbuseMin = CreateFieldLabel(Strings.Get("AbuseIPDB minimum confidence (%)"), new Point(leftMargin + 195, currentY));
        numAbuseMinConfidence = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin + 195, currentY + 18),
            Size = new Size(185, 23),
            Minimum = 25,
            Maximum = 100,
            Value = 90
        };
        Controls.Add(lblAbuseMin);
        Controls.Add(numAbuseMinConfidence);
        currentY += 46;

        Label lblAbuseKey = CreateFieldLabel(Strings.Get("AbuseIPDB API key"), new Point(leftMargin, currentY));
        txtAbuseApiKey = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblAbuseKey);
        Controls.Add(txtAbuseApiKey);
        currentY += 46;

        Label lblCustomUrls = CreateFieldLabel(Strings.Get("Custom threat feed URLs (one per line)"), new Point(leftMargin, currentY));
        txtCustomUrls = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 48),
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
        currentY += 26;

        Label lblProbationDays = CreateFieldLabel(Strings.Get("Probation decay period (days)"), new Point(leftMargin, currentY));
        numProbationDays = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(180, 23),
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
            Size = new Size(controlWidth, 23)
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
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblBogonV6);
        Controls.Add(txtBogonIpv6Url);
        currentY += 52;

        // === Section 4: GeoIP & Geo-fencing ===
        Label lblSectionGeo = CreateHeaderLabel(Strings.Get("GeoIP & Country-level Blocking (Geo-fencing)"), 10F, AccentColor, new Point(leftMargin, currentY));
        Controls.Add(lblSectionGeo);
        currentY += 24;

        chkEnableGeoBlocking = new CheckBox
        {
            Text = Strings.Get("Enable country-level Geo-blocking"),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY),
            AutoSize = true
        };
        Controls.Add(chkEnableGeoBlocking);
        currentY += 26;

        Label lblBlockedCountries = CreateFieldLabel(Strings.Get("Blocked country codes (ISO 3166-1 alpha-2, e.g. CN, RU)"), new Point(leftMargin, currentY));
        txtBlockedCountries = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblBlockedCountries);
        Controls.Add(txtBlockedCountries);
        currentY += 46;

        chkEnableGeoIpAutoUpdate = new CheckBox
        {
            Text = Strings.Get("Enable GeoIP automatic database update"),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY),
            AutoSize = true
        };
        Controls.Add(chkEnableGeoIpAutoUpdate);
        currentY += 26;

        Label lblGeoV4 = CreateFieldLabel(Strings.Get("GeoIP IPv4 Database URL:"), new Point(leftMargin, currentY));
        txtGeoIpDatabaseIpv4Url = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblGeoV4);
        Controls.Add(txtGeoIpDatabaseIpv4Url);
        currentY += 46;

        Label lblGeoV6 = CreateFieldLabel(Strings.Get("GeoIP IPv6 Database URL:"), new Point(leftMargin, currentY));
        txtGeoIpDatabaseIpv6Url = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        Controls.Add(lblGeoV6);
        Controls.Add(txtGeoIpDatabaseIpv6Url);
        currentY += 46;

        Label lblGeoLocal = CreateFieldLabel(Strings.Get("Local GeoIP CSV file path (optional)"), new Point(leftMargin, currentY));
        txtGeoIpLocalFilePath = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(controlWidth - 85, 23)
        };
        btnBrowseGeoIpFile = new Button
        {
            Font = defaultFont,
            Location = new Point(leftMargin + controlWidth - 80, currentY + 17),
            Size = new Size(80, 25),
            Text = Strings.Get("Browse...")
        };
        btnBrowseGeoIpFile.Click += (_, _) =>
        {
            using OpenFileDialog dialog = new()
            {
                Filter = Strings.Get("CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"),
                Title = Strings.Get("Local GeoIP CSV file path (optional)")
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtGeoIpLocalFilePath.Text = dialog.FileName;
            }
        };
        Controls.Add(lblGeoLocal);
        Controls.Add(txtGeoIpLocalFilePath);
        Controls.Add(btnBrowseGeoIpFile);
        currentY += 46;

        Label lblGeoDays = CreateFieldLabel(Strings.Get("GeoIP update interval (days)"), new Point(leftMargin, currentY));
        numGeoIpUpdateDays = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin, currentY + 18),
            Size = new Size(120, 23),
            Minimum = 1,
            Maximum = 365,
            Value = 7
        };
        Controls.Add(lblGeoDays);
        Controls.Add(numGeoIpUpdateDays);
        currentY += 48;

        btnUpdateGeoIpNow = new Button
        {
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(leftMargin, currentY),
            Size = new Size(200, 30),
            Text = Strings.Get("Update GeoIP Database Now")
        };
        lblGeoIpStatus = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(leftMargin + 210, currentY + 6)
        };
        btnUpdateGeoIpNow.Click += async (_, _) =>
        {
            btnUpdateGeoIpNow.Enabled = false;
            lblGeoIpStatus.Text = Strings.Get("Updating GeoIP database...");
            try
            {
                IddsConfig cfg = IddsConfig.Instance;
                cfg.GeoIpDatabaseIpv4Url = txtGeoIpDatabaseIpv4Url.Text.Trim();
                cfg.GeoIpDatabaseIpv6Url = txtGeoIpDatabaseIpv6Url.Text.Trim();
                cfg.GeoIpLocalFilePath = txtGeoIpLocalFilePath.Text.Trim();
                cfg.EnableGeoIpAutoUpdate = chkEnableGeoIpAutoUpdate.Checked;
                cfg.GeoIpUpdateIntervalDays = (int)numGeoIpUpdateDays.Value;

                using GeoIpUpdateService updater = new(cfg);
                var result = await updater.RefreshDatabaseAsync(isManual: true).ConfigureAwait(true);
                if (result.Success)
                {
                    lblGeoIpStatus.Text = string.Format(
                        Strings.Get("GeoIP database updated successfully: {0} prefixes across {1} countries loaded."),
                        result.TotalRecords, result.TotalCountries);
                }
                else
                {
                    lblGeoIpStatus.Text = string.Format(
                        Strings.Get("Failed to update GeoIP database: {0}"),
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                lblGeoIpStatus.Text = string.Format(
                    Strings.Get("Failed to update GeoIP database: {0}"),
                    ex.Message);
            }
            finally
            {
                btnUpdateGeoIpNow.Enabled = true;
            }
        };
        Controls.Add(btnUpdateGeoIpNow);
        Controls.Add(lblGeoIpStatus);
        currentY += 52;

        // Action Buttons
        Button btnSave = new()
        {
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(leftMargin, currentY),
            Size = new Size(120, 32),
            Text = Strings.Get("&Save")
        };
        btnSave.Click += SaveSettings;
        Controls.Add(btnSave);

        SettingsResetButtonFactory.AddTo(
            this,
            (_, _) => ResetToDefaults(),
            new Point(leftMargin + 130, currentY));

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

        chkEnableGeoIpAutoUpdate.Checked = config.EnableGeoIpAutoUpdate;
        txtGeoIpDatabaseIpv4Url.Text = config.GeoIpDatabaseIpv4Url;
        txtGeoIpDatabaseIpv6Url.Text = config.GeoIpDatabaseIpv6Url;
        txtGeoIpLocalFilePath.Text = config.GeoIpLocalFilePath;
        numGeoIpUpdateDays.Value = Math.Clamp(config.GeoIpUpdateIntervalDays, 1, 365);

        chkEnableGeoBlocking.Checked = config.EnableGeoBlocking;
        txtBlockedCountries.Text = config.BlockedCountryCodes;

        int loadedRecords = GeoIpLookupService.TotalLoadedRecords;
        int loadedCountries = GeoIpLookupService.TotalLoadedCountries;
        if (loadedRecords > 0)
        {
            lblGeoIpStatus.Text = string.Format(
                Strings.Get("GeoIP database updated successfully: {0} prefixes across {1} countries loaded."),
                loadedRecords, loadedCountries);
        }

        UpdateClusterControlsState();
    }

    private void UpdateClusterControlsState()
    {
        ThreatHubRole role = (ThreatHubRole)Math.Clamp(comboClusterRole.SelectedIndex, 0, 2);
        switch (role)
        {
            case ThreatHubRole.Standalone:
                txtHubEndpoint.Enabled = false;
                txtHubApiKey.Enabled = false;
                numHubPort.Enabled = false;
                numSyncInterval.Enabled = false;
                break;
            case ThreatHubRole.EdgeNode:
                txtHubEndpoint.Enabled = true;
                txtHubApiKey.Enabled = true;
                numHubPort.Enabled = false;
                numSyncInterval.Enabled = true;
                break;
            case ThreatHubRole.ThreatHub:
                txtHubEndpoint.Enabled = false;
                txtHubApiKey.Enabled = true;
                numHubPort.Enabled = true;
                numSyncInterval.Enabled = false;
                break;
        }
    }

    private const string DefaultBogonV4 = "https://www.team-cymru.com/Services/Bogons/fullbogons-ipv4.txt";
    private const string DefaultBogonV6 = "https://www.team-cymru.com/Services/Bogons/fullbogons-ipv6.txt";
    private const string DefaultGeoIpV4 = "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv4.net.csv";
    private const string DefaultGeoIpV6 = "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv6.net.csv";

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

        config.EnableGeoIpAutoUpdate = chkEnableGeoIpAutoUpdate.Checked;
        config.GeoIpDatabaseIpv4Url = txtGeoIpDatabaseIpv4Url.Text.Trim();
        config.GeoIpDatabaseIpv6Url = txtGeoIpDatabaseIpv6Url.Text.Trim();
        config.GeoIpLocalFilePath = txtGeoIpLocalFilePath.Text.Trim();
        config.GeoIpUpdateIntervalDays = (int)numGeoIpUpdateDays.Value;

        config.EnableGeoBlocking = chkEnableGeoBlocking.Checked;
        config.BlockedCountryCodes = txtBlockedCountries.Text.Trim();

        config.SaveAppConfig();
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

        chkEnableDynamicBogon.Checked = false;
        txtBogonIpv4Url.Text = DefaultBogonV4;
        txtBogonIpv6Url.Text = DefaultBogonV6;
        numProbationDays.Value = 90;

        chkEnableGeoIpAutoUpdate.Checked = true;
        txtGeoIpDatabaseIpv4Url.Text = DefaultGeoIpV4;
        txtGeoIpDatabaseIpv6Url.Text = DefaultGeoIpV6;
        txtGeoIpLocalFilePath.Text = string.Empty;
        numGeoIpUpdateDays.Value = 7;

        chkEnableGeoBlocking.Checked = false;
        txtBlockedCountries.Text = string.Empty;
    }

    private static SmartLabel CreateHeaderLabel(string text, float fontSize, Color color, Point location) =>
        new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
            ForeColor = color,
            Location = location,
            Text = text
        };

    private static Label CreateFieldLabel(string text, Point location) =>
        new()
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = BodyTextColor,
            Location = location,
            Text = text
        };
}
