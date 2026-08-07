using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Navy = Color.FromArgb(17, 37, 61);
    private static readonly Color Teal = Color.FromArgb(19, 184, 166);
    private readonly Button launchAppButton = CreateActionButton(SetupText.Get("LaunchApp"), primary: true);
    private readonly Button installButton = CreateActionButton(SetupText.Get("Install"), primary: true);
    private readonly Button uninstallButton = CreateActionButton(SetupText.Get("Uninstall"), primary: false);
    private readonly Button userGuideButton = CreateActionButton(SetupText.Get("OpenUserGuide"), primary: false);
    private readonly Button closeButton = CreateActionButton(SetupText.Get("Close"), primary: false);
    private readonly Button languageButton = new()
    {
        AutoSize = true,
        Location = new Point(510, 16),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = Navy,
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
        Cursor = Cursors.Hand
    };
    private readonly Label titleLabel = new() { AutoSize = true, Location = new Point(32, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Teal };
    private readonly Label descriptionLabel = new() { AutoSize = true, MaximumSize = new Size(576, 0), Location = new Point(32, 50) };
    private readonly Label locationLabel = new() { AutoSize = true, MaximumSize = new Size(576, 0), Location = new Point(32, 112), ForeColor = Color.FromArgb(100, 116, 139) };
    private readonly CheckBox checkBoxDesktopShortcut = new() { AutoSize = true, Location = new Point(32, 142), Checked = true };
    private readonly CheckBox checkBoxStartMenuShortcut = new() { AutoSize = true, Location = new Point(220, 142), Checked = true };
    private readonly Label statusLabel = new() { AutoSize = true, Location = new Point(32, 172), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    /// <summary>
    /// 初始化安裝程式視窗。
    /// </summary>
    internal SetupForm()
    {
        ClientSize = new Size(640, 275);
        BackColor = Color.FromArgb(243, 246, 248);
        ForeColor = Navy;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        FlowLayoutPanel actions = new() { AutoSize = true, Location = new Point(28, 210), Padding = new Padding(0), WrapContents = false };
        actions.Controls.AddRange([launchAppButton, installButton, uninstallButton, userGuideButton, closeButton]);
        Controls.AddRange([languageButton, titleLabel, descriptionLabel, locationLabel, checkBoxDesktopShortcut, checkBoxStartMenuShortcut, statusLabel, actions]);

        launchAppButton.Click += (_, _) => SetupOperations.LaunchApp();
        userGuideButton.Click += (_, _) => SetupOperations.OpenUserGuide();
        closeButton.Click += (_, _) => Close();
        installButton.Click += async (_, _) => await ExecuteAsync(true);
        uninstallButton.Click += async (_, _) => await ExecuteAsync(false);
        languageButton.Click += (_, _) => ToggleLanguage();

        CancelButton = closeButton;
        RefreshLocalizedText();
    }

    private void ToggleLanguage()
    {
        bool isZh = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(isZh ? "en-US" : "zh-TW");
        RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        Text = SetupText.Get("Title");
        languageButton.Text = SetupText.Get("LanguageButtonText");
        titleLabel.Text = SetupText.Get("Title");
        descriptionLabel.Text = SetupText.Get("Description");
        locationLabel.Text = SetupText.Format("InstallLocation", SetupOperations.InstallDirectory);
        checkBoxDesktopShortcut.Text = SetupText.Get("CreateDesktopShortcut");
        checkBoxStartMenuShortcut.Text = SetupText.Get("CreateStartMenuShortcut");
        launchAppButton.Text = SetupText.Get("LaunchApp");
        uninstallButton.Text = SetupText.Get("Uninstall");
        userGuideButton.Text = SetupText.Get("OpenUserGuide");
        closeButton.Text = SetupText.Get("Close");
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool installed = SetupOperations.IsInstalled;
        SetupOperations.InstallAction action = SetupOperations.CurrentInstallAction;
        Version? installedVer = SetupOperations.InstalledVersion;
        Version setupVer = SetupOperations.CurrentSetupVersion;

        if (action == SetupOperations.InstallAction.FreshInstall)
        {
            statusLabel.Text = SetupText.Get("StatusNotInstalled");
            statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
            installButton.Text = SetupText.Get("Install");
            installButton.BackColor = Teal;
            installButton.ForeColor = Color.White;
        }
        else if (action == SetupOperations.InstallAction.Upgrade)
        {
            statusLabel.Text = SetupText.Format("StatusUpgradeAvailable", installedVer?.ToString(3) ?? "3.0.0", setupVer.ToString(3));
            statusLabel.ForeColor = Teal;
            installButton.Text = SetupText.Get("Upgrade");
            installButton.BackColor = Teal;
            installButton.ForeColor = Color.White;
        }
        else if (action == SetupOperations.InstallAction.Downgrade)
        {
            statusLabel.Text = SetupText.Format("StatusInstalledWithVersion", installedVer?.ToString(3) ?? setupVer.ToString(3));
            statusLabel.ForeColor = Color.FromArgb(225, 29, 72);
            installButton.Text = SetupText.Get("Downgrade");
            installButton.BackColor = Color.FromArgb(225, 29, 72);
            installButton.ForeColor = Color.White;
        }
        else
        {
            statusLabel.Text = SetupText.Format("StatusInstalledWithVersion", installedVer?.ToString(3) ?? "3.0.0");
            statusLabel.ForeColor = Teal;
            installButton.Text = SetupText.Get("Reinstall");
            installButton.BackColor = Color.White;
            installButton.ForeColor = Navy;
        }

        launchAppButton.Visible = installed && SetupOperations.CanLaunchApp;
        uninstallButton.Visible = installed;
        checkBoxDesktopShortcut.Checked = installed ? SetupOperations.HasDesktopShortcut : true;
        checkBoxStartMenuShortcut.Checked = installed ? SetupOperations.HasStartMenuShortcut : true;
    }

    private static Button CreateActionButton(string text, bool primary) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(104, 38),
        Margin = new Padding(3),
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Teal : Color.White,
        ForeColor = primary ? Color.White : Navy,
        UseVisualStyleBackColor = false
    };

    private async Task ExecuteAsync(bool install)
    {
        SetupOperations.InstallAction currentAction = SetupOperations.CurrentInstallAction;
        string actionName = install ? (currentAction switch
        {
            SetupOperations.InstallAction.Upgrade => SetupText.Get("Upgrade"),
            SetupOperations.InstallAction.Downgrade => SetupText.Get("Downgrade"),
            SetupOperations.InstallAction.Reinstall => SetupText.Get("Reinstall"),
            _ => SetupText.Get("Install")
        }) : SetupText.Get("Uninstall");

        if (install && currentAction == SetupOperations.InstallAction.Downgrade)
        {
            Version installedVer = SetupOperations.InstalledVersion ?? new Version(3, 0, 0);
            Version setupVer = SetupOperations.CurrentSetupVersion;
            if (MessageBox.Show(SetupText.Format("DowngradeConfirm", installedVer.ToString(3), setupVer.ToString(3)), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }
        else
        {
            if (MessageBox.Show(SetupText.Format("Confirm", actionName), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
        }

        installButton.Enabled = uninstallButton.Enabled = false;
        bool desktop = checkBoxDesktopShortcut.Checked;
        bool startMenu = checkBoxStartMenuShortcut.Checked;
        try
        {
            await Task.Run(() =>
            {
                if (install) SetupOperations.Install(desktop, startMenu);
                else SetupOperations.Uninstall();
            });
            MessageBox.Show(SetupText.Format("Completed", actionName, SetupOperations.InstallDirectory), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateStatus();
            installButton.Enabled = true;
        }
    }
}
