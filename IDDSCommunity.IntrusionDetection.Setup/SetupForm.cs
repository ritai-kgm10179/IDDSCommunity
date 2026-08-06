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
    private readonly Label statusLabel = new() { AutoSize = true, Location = new Point(32, 146), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    /// <summary>Initializes the setup window.</summary>
    internal SetupForm()
    {
        Text = SetupText.Get("Title");
        ClientSize = new Size(560, 250);
        BackColor = Color.FromArgb(243, 246, 248);
        ForeColor = Navy;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Label title = new() { Text = SetupText.Get("Title"), AutoSize = true, Location = new Point(32, 24), Font = new Font(Font, FontStyle.Bold), ForeColor = Teal };
        Label description = new() { Text = SetupText.Get("Description"), AutoSize = true, MaximumSize = new Size(496, 0), Location = new Point(32, 56) };
        Label location = new() { Text = SetupText.Format("InstallLocation", SetupOperations.InstallDirectory), AutoSize = true, MaximumSize = new Size(496, 0), Location = new Point(32, 118), ForeColor = Color.FromArgb(100, 116, 139) };
        FlowLayoutPanel actions = new() { AutoSize = true, Location = new Point(28, 185), Padding = new Padding(0), WrapContents = false };
        actions.Controls.AddRange([launchAppButton, installButton, uninstallButton]);
        Controls.AddRange([title, description, location, statusLabel, actions]);
        launchAppButton.Click += (_, _) => SetupOperations.LaunchApp();
        installButton.Click += async (_, _) => await ExecuteAsync(true);
        uninstallButton.Click += async (_, _) => await ExecuteAsync(false);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool installed = SetupOperations.IsInstalled;
        statusLabel.Text = SetupText.Get(installed ? "StatusInstalled" : "StatusNotInstalled");
        statusLabel.ForeColor = installed ? Teal : Color.FromArgb(100, 116, 139);
        launchAppButton.Visible = installed && SetupOperations.CanLaunchApp;
        installButton.Text = SetupText.Get(installed ? "Reinstall" : "Install");
        installButton.BackColor = installed ? Color.White : Teal;
        installButton.ForeColor = installed ? Navy : Color.White;
        uninstallButton.Enabled = installed;
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
        string action = SetupText.Get(install ? "Install" : "Uninstall");
        if (MessageBox.Show(SetupText.Format("Confirm", action), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        installButton.Enabled = uninstallButton.Enabled = false;
        try
        {
            await Task.Run(() =>
            {
                if (install) SetupOperations.Install();
                else SetupOperations.Uninstall();
            });
            MessageBox.Show(SetupText.Format("Completed", action, SetupOperations.InstallDirectory), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
