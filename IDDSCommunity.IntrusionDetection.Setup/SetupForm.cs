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
    private readonly CheckBox checkBoxDesktopShortcut = new() { Text = SetupText.Get("CreateDesktopShortcut"), AutoSize = true, Location = new Point(32, 142), Checked = true };
    private readonly CheckBox checkBoxStartMenuShortcut = new() { Text = SetupText.Get("CreateStartMenuShortcut"), AutoSize = true, Location = new Point(220, 142), Checked = true };
    private readonly Label statusLabel = new() { AutoSize = true, Location = new Point(32, 172), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    /// <summary>Initializes the setup window.</summary>
    internal SetupForm()
    {
        Text = SetupText.Get("Title");
        ClientSize = new Size(580, 275);
        BackColor = Color.FromArgb(243, 246, 248);
        ForeColor = Navy;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Label title = new() { Text = SetupText.Get("Title"), AutoSize = true, Location = new Point(32, 20), Font = new Font(Font, FontStyle.Bold), ForeColor = Teal };
        Label description = new() { Text = SetupText.Get("Description"), AutoSize = true, MaximumSize = new Size(516, 0), Location = new Point(32, 50) };
        Label location = new() { Text = SetupText.Format("InstallLocation", SetupOperations.InstallDirectory), AutoSize = true, MaximumSize = new Size(516, 0), Location = new Point(32, 112), ForeColor = Color.FromArgb(100, 116, 139) };
        FlowLayoutPanel actions = new() { AutoSize = true, Location = new Point(28, 210), Padding = new Padding(0), WrapContents = false };
        actions.Controls.AddRange([launchAppButton, installButton, uninstallButton, userGuideButton]);
        Controls.AddRange([title, description, location, checkBoxDesktopShortcut, checkBoxStartMenuShortcut, statusLabel, actions]);
        launchAppButton.Click += (_, _) => SetupOperations.LaunchApp();
        userGuideButton.Click += (_, _) => SetupOperations.OpenUserGuide();
        installButton.Click += async (_, _) => await ExecuteAsync(true);
        uninstallButton.Click += async (_, _) => await ExecuteAsync(false);
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
            statusLabel.Text = SetupText.Format("StatusInstalledWithVersion", installedVer?.ToString(3) ?? setupVer.ToString(3));
            statusLabel.ForeColor = Teal;
            installButton.Text = SetupText.Get("Reinstall");
            installButton.BackColor = Color.White;
            installButton.ForeColor = Navy;
        }

        launchAppButton.Visible = installed && SetupOperations.CanLaunchApp;
        uninstallButton.Enabled = installed;
        checkBoxDesktopShortcut.Checked = installed ? SetupOperations.HasDesktopShortcut : true;
        checkBoxStartMenuShortcut.Checked = installed ? SetupOperations.HasStartMenuShortcut : true;
    }

    private static Button CreateActionButton(string text, bool primary) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(112, 38),
        Margin = new Padding(4),
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
