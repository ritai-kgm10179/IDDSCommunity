using System;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class CyberarmsApplicationSettings : UserControl
{

    public const string MENU_LOCK_OUT_CONFIGURATION = "Lock out configuration";
    public const string MENU_SAFE_NETWORKS = "Safe networks";
    public const string MENU_LICENSING = "Licensing";
    public const string MENU_NOTIFICATION_SETTINGS = "Notification settings";
    public const string MENU_SMTP_SETTINGS = "SMTP configuration";
    public const string MENU_LANGUAGE_SETTINGS = "Language settings";
    public event EventHandler? ConfigurationChanged;


    /// <summary>
    /// Initializes a new instance of the <see cref="CyberarmsApplicationSettings"/> class.
    /// </summary>

    public CyberarmsApplicationSettings()
    {
        InitializeComponent();
        BackColor = Color.White;
        Load += new EventHandler(CyberamsApplicationSettings_Load);
    }

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void CyberamsApplicationSettings_Load(object? sender, EventArgs? e)
    {
        cyberarmsSettingsNavigation.NavigationChanged += new EventHandler(cyberarmsSettingsNavigation_NavigationChanged);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_LOCK_OUT_CONFIGURATION), null!, null!);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_SAFE_NETWORKS), null!, null!);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_LICENSING), null!, null!);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_NOTIFICATION_SETTINGS), null!, null!);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_SMTP_SETTINGS), null!, null!);
        cyberarmsSettingsNavigation.AddNavigationItem(Strings.Get(MENU_LANGUAGE_SETTINGS), null!, null!);
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
    /// Handles the lockout configuration changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
    /// Handles the safe networks changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
    /// Handles the smtp settings changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _panelSmtpSettings_SmtpSettingsChanged(object? sender, EventArgs? e) => OnConfigurationChanged();

    private PanelNotificationSettings? _panelNotificationSettings;
    private PanelLanguageSettings? _panelLanguageSettings;

    public PanelLanguageSettings PanelLanguageSettings => _panelLanguageSettings ??= CreateLanguageSettingsPanel();

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
    /// Handles the notification settings changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _panelNotificationSettings_NotificationSettingsChanged(object? sender, EventArgs? e) => OnConfigurationChanged();

    /// <summary>
    /// Handles the navigation changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void cyberarmsSettingsNavigation_NavigationChanged(object? sender, EventArgs? e)
    {
        switch ((sender as CyberarmsSettingsNavigationItem)?.DisplayName)
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
        }
    }

    /// <summary>
    /// Processes the configuration changed notification.
    /// </summary>

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke(this, EventArgs.Empty);


}
