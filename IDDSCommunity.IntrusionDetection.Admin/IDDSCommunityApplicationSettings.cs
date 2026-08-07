using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunityApplicationSettings : UserControl
{

    public const string MENU_LOCK_OUT_CONFIGURATION = "Lock out configuration";
    public const string MENU_SAFE_NETWORKS = "Safe networks";
    public const string MENU_NOTIFICATION_SETTINGS = "Notification settings";
    public const string MENU_SMTP_SETTINGS = "SMTP configuration";
    public const string MENU_LANGUAGE_SETTINGS = "Language settings";
    public const string MENU_DATABASE_MAINTENANCE = "Database maintenance";
    public const string MENU_CONFIGURATION_TRANSFER = "Configuration import and export";
    public const string MENU_REPORT_EXPORT = "Report export";
    public event EventHandler? ConfigurationChanged;


    /// <summary>
    /// 初始化 <see cref="IDDSCommunityApplicationSettings"/> 類別的新執行個體。
    /// </summary>

    public IDDSCommunityApplicationSettings()
    {
        InitializeComponent();
        BackColor = Color.White;
        Load += new EventHandler(CyberamsApplicationSettings_Load);
    }

    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void CyberamsApplicationSettings_Load(object? sender, EventArgs? e)
    {
        iddscommunitySettingsNavigation.NavigationChanged += new EventHandler(iddscommunitySettingsNavigation_NavigationChanged);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_LOCK_OUT_CONFIGURATION), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_SAFE_NETWORKS), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_NOTIFICATION_SETTINGS), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_SMTP_SETTINGS), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_LANGUAGE_SETTINGS), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_DATABASE_MAINTENANCE), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_CONFIGURATION_TRANSFER), null, null);
        iddscommunitySettingsNavigation.AddNavigationItem(Strings.Get(MENU_REPORT_EXPORT), null, null);
    }

    private PanelLockoutConfiguration? _lockoutConfiguration;

    public PanelLockoutConfiguration LockoutConfiguration
    {
        get
        {
            if (_lockoutConfiguration == null)
            {
                _lockoutConfiguration = new PanelLockoutConfiguration
                {
                    Dock = DockStyle.Fill
                };
                configurationPanel.Controls.Add(_lockoutConfiguration);
                _lockoutConfiguration.LockoutConfigurationChanged += new EventHandler(_lockoutConfiguration_LockoutConfigurationChanged);
            }
            return _lockoutConfiguration;
        }
    }

    /// <summary>
    /// 處理 lockout configuration changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void _lockoutConfiguration_LockoutConfigurationChanged(object? sender, EventArgs? e) => OnConfigurationChanged();







    private PanelSafeNetworks? _panelSafeNetworks;
    public PanelSafeNetworks PanelSafeNetworks
    {
        get
        {
            if (_panelSafeNetworks == null)
            {
                _panelSafeNetworks = new PanelSafeNetworks
                {
                    Dock = DockStyle.Fill
                };
                configurationPanel.Controls.Add(_panelSafeNetworks);
                _panelSafeNetworks.SafeNetworksChanged += new EventHandler(_panelSafeNetworks_SafeNetworksChanged);
            }
            return _panelSafeNetworks;
        }
    }

    /// <summary>
    /// 處理 safe networks changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void _panelSafeNetworks_SafeNetworksChanged(object? sender, EventArgs? e) => OnConfigurationChanged();

    private PanelSmtpSettings? _panelSmtpSettings;
    public PanelSmtpSettings PanelSmtpSettings
    {
        get
        {
            if (_panelSmtpSettings == null)
            {
                _panelSmtpSettings = new PanelSmtpSettings
                {
                    Dock = DockStyle.Fill
                };
                configurationPanel.Controls.Add(_panelSmtpSettings);
                _panelSmtpSettings.SmtpSettingsChanged += new EventHandler(_panelSmtpSettings_SmtpSettingsChanged);
            }
            return _panelSmtpSettings;
        }

    }

    /// <summary>
    /// 處理 smtp settings changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void _panelSmtpSettings_SmtpSettingsChanged(object? sender, EventArgs? e) => OnConfigurationChanged();

    private PanelNotificationSettings? _panelNotificationSettings;
    private PanelLanguageSettings? _panelLanguageSettings;
    private PanelDatabaseMaintenance? _panelDatabaseMaintenance;
    private PanelConfigurationTransfer? _panelConfigurationTransfer;
    private PanelReportExport? _panelReportExport;

    public PanelLanguageSettings PanelLanguageSettings => _panelLanguageSettings ??= CreateLanguageSettingsPanel();
    public PanelDatabaseMaintenance PanelDatabaseMaintenance => _panelDatabaseMaintenance ??= CreateDatabaseMaintenancePanel();
    public PanelConfigurationTransfer PanelConfigurationTransfer => _panelConfigurationTransfer ??= CreateConfigurationTransferPanel();
    public PanelReportExport PanelReportExport => _panelReportExport ??= CreateReportExportPanel();

    private PanelReportExport CreateReportExportPanel()
    {
        PanelReportExport panel = new();
        configurationPanel.Controls.Add(panel);
        return panel;
    }

    private PanelConfigurationTransfer CreateConfigurationTransferPanel()
    {
        PanelConfigurationTransfer panel = new();
        configurationPanel.Controls.Add(panel);
        return panel;
    }

    private PanelDatabaseMaintenance CreateDatabaseMaintenancePanel()
    {
        PanelDatabaseMaintenance panel = new();
        configurationPanel.Controls.Add(panel);
        return panel;
    }

    /// <summary>
    /// Creates and attaches the language settings panel.
    /// </summary>
    /// <returns>附加的語言設定面板。</returns>
    private PanelLanguageSettings CreateLanguageSettingsPanel()
    {
        PanelLanguageSettings panel = new();
        configurationPanel.Controls.Add(panel);
        return panel;
    }
    public PanelNotificationSettings PanelNotificationSettings
    {
        get
        {
            if (_panelNotificationSettings == null)
            {
                _panelNotificationSettings = new PanelNotificationSettings
                {
                    Dock = DockStyle.Fill
                };
                _panelNotificationSettings.NotificationSettingsChanged += new EventHandler(_panelNotificationSettings_NotificationSettingsChanged);
                configurationPanel.Controls.Add(_panelNotificationSettings);
            }
            return _panelNotificationSettings;
        }
    }

    /// <summary>
    /// 處理 notification settings changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void _panelNotificationSettings_NotificationSettingsChanged(object? sender, EventArgs? e) => OnConfigurationChanged();

    /// <summary>
    /// 處理 navigation changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void iddscommunitySettingsNavigation_NavigationChanged(object? sender, EventArgs? e)
    {
        switch ((sender as IDDSCommunitySettingsNavigationItem)?.DisplayName)
        {

            case var displayName when displayName == Strings.Get(MENU_LOCK_OUT_CONFIGURATION):
                LockoutConfiguration.BringToFront();
                break;
            case var displayName when displayName == Strings.Get(MENU_NOTIFICATION_SETTINGS):
                PanelNotificationSettings.BringToFront();
                PanelNotificationSettings.LoadData();
                break;
            case var displayName when displayName == Strings.Get(MENU_SAFE_NETWORKS):
                PanelSafeNetworks.BringToFront();
                break;
            case var displayName when displayName == Strings.Get(MENU_SMTP_SETTINGS):
                PanelSmtpSettings.BringToFront();
                break;
            case var displayName when displayName == Strings.Get(MENU_LANGUAGE_SETTINGS):
                PanelLanguageSettings.BringToFront();
                break;
            case var displayName when displayName == Strings.Get(MENU_DATABASE_MAINTENANCE):
                PanelDatabaseMaintenance.BringToFront();
                PanelDatabaseMaintenance.RefreshStatus();
                break;
            case var displayName when displayName == Strings.Get(MENU_CONFIGURATION_TRANSFER):
                PanelConfigurationTransfer.BringToFront();
                break;
            case var displayName when displayName == Strings.Get(MENU_REPORT_EXPORT):
                PanelReportExport.BringToFront();
                break;
        }
    }

    /// <summary>
    /// Processes the configuration changed notification.
    /// </summary>

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke(this, EventArgs.Empty);


}
