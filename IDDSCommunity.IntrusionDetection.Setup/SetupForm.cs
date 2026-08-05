using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Navy = Color.FromArgb(17, 37, 61);
    private static readonly Color Teal = Color.FromArgb(19, 184, 166);
    private readonly Button installButton = CreateActionButton(SetupText.Get("Install"), primary: true);
    private readonly Button uninstallButton = CreateActionButton(SetupText.Get("Uninstall"), primary: false);

    /// <summary>Initializes the setup window.</summary>
    internal SetupForm()
    {
        Text = SetupText.Get("Title");
        ClientSize = new Size(560, 220);
        BackColor = Color.FromArgb(243, 246, 248);
        ForeColor = Navy;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Label title = new() { Text = SetupText.Get("Title"), AutoSize = true, Location = new Point(32, 28), Font = new Font(Font, FontStyle.Bold), ForeColor = Teal };
        Label description = new() { Text = SetupText.Get("Description"), AutoSize = true, MaximumSize = new Size(496, 0), Location = new Point(32, 66) };
        FlowLayoutPanel actions = new() { AutoSize = true, Location = new Point(28, 158), Padding = new Padding(0), WrapContents = false };
        actions.Controls.AddRange([installButton, uninstallButton]);
        Controls.AddRange([title, description, actions]);
        installButton.Click += async (_, _) => await ExecuteAsync(true);
        uninstallButton.Click += async (_, _) => await ExecuteAsync(false);
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
            MessageBox.Show(SetupText.Format("Completed", action), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { installButton.Enabled = uninstallButton.Enabled = true; }
    }
}
