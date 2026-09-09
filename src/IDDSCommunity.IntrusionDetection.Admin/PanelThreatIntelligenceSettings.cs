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
public sealed partial class PanelThreatIntelligenceSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(15, 118, 110);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    /// <summary>
    /// 當威脅情報與叢集聯防設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? ThreatIntelligenceSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelThreatIntelligenceSettings"/> 類別之新執行個體。
    /// </summary>
    public PanelThreatIntelligenceSettings()
    {
        InitializeComponent();

        comboClusterRole.Items.AddRange([
            Strings.Get("Standalone"),
            Strings.Get("Edge Node"),
            Strings.Get("Threat Hub")
        ]);
        comboClusterRole.SelectedIndexChanged += (_, _) => UpdateClusterControlsState();

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

        btnSave.Click += SaveSettings;

        SettingsResetButtonFactory.AddTo(this, (_, _) => ResetToDefaults(), container: headerPanel);

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

    private const string DefaultBogonV4 = "https://www.team-cymru.org/Services/Bogons/fullbogons-ipv4.txt";
    private const string DefaultBogonV6 = "https://www.team-cymru.org/Services/Bogons/fullbogons-ipv6.txt";
    private const string DefaultGeoIpV4 = "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv4.csv";
    private const string DefaultGeoIpV6 = "https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country/dbip-country-ipv6.csv";

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
}
