using System;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public sealed class PanelConfigurationTransfer : UserControl
{
    private readonly ConfigurationTransferService transfer = new(Database.Instance);
    private readonly Label status;
    private readonly CheckBox includeSecrets;
    private readonly TextBox passphrase;
    private readonly Button exportButton;
    private readonly Button importButton;

    public PanelConfigurationTransfer()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        Controls.Add(Label(Strings.Get("Configuration import and export"), 11F, Color.FromArgb(19, 184, 166), 11, 8));
        Controls.Add(Label(Strings.Get("Transfer policies, safe networks, application settings, and Agent settings using a versioned JSON package."), 9F, Color.FromArgb(102, 102, 102), 15, 43));
        includeSecrets = new CheckBox { AutoSize = true, Font = new Font("Segoe UI", 9F), Location = new Point(15, 82), Text = Strings.Get("Include encrypted SMTP password") };
        Controls.Add(includeSecrets);
        Controls.Add(Label(Strings.Get("Package passphrase"), 9F, Color.FromArgb(102, 102, 102), 15, 118));
        passphrase = new TextBox { Font = new Font("Segoe UI", 9F), Location = new Point(145, 114), PasswordChar = '●', Size = new Size(260, 24) };
        Controls.Add(passphrase);
        exportButton = Button(Strings.Get("Export settings"), 15, 158);
        importButton = Button(Strings.Get("Import settings"), 145, 158);
        exportButton.Click += Export;
        importButton.Click += Import;
        Controls.Add(exportButton);
        Controls.Add(importButton);
        status = Label(Strings.Get("Secrets are excluded by default. Selected secrets are protected with Argon2id and AES-256-GCM."), 9F, Color.FromArgb(102, 102, 102), 15, 205);
        status.AutoSize = false;
        status.Size = new Size(620, 100);
        Controls.Add(status);
    }

    private async void Export(object? sender, EventArgs e)
    {
        if (includeSecrets.Checked && passphrase.Text.Length < 12) { ShowError(Strings.Get("Enter a passphrase containing at least 12 characters before exporting secrets.")); return; }
        using SaveFileDialog dialog = new() { AddExtension = true, DefaultExt = "json", Filter = Strings.Get("IDDS Community settings (*.json)|*.json"), FileName = "idds-community-settings.json", RestoreDirectory = true, Title = Strings.Get("Export IDDS Community settings") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunAsync(() => transfer.ExportToFile(dialog.FileName, includeSecrets.Checked, passphrase.Text), Strings.Format("Settings exported: {0}", dialog.FileName));
    }

    private async void Import(object? sender, EventArgs e)
    {
        if (!IsServiceStopped()) { ShowError(Strings.Get("Stop the IDDS Community service before importing settings.")); return; }
        using OpenFileDialog dialog = new() { CheckFileExists = true, Filter = Strings.Get("IDDS Community settings (*.json)|*.json"), RestoreDirectory = true, Title = Strings.Get("Import IDDS Community settings") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ConfigurationTransferPackage package = transfer.ReadPackage(dialog.FileName);
            ConfigurationImportPreview preview = transfer.Preview(package);
            if (package.Secrets is not null && string.IsNullOrWhiteSpace(passphrase.Text)) { ShowError(Strings.Get("This package contains encrypted secrets. Enter its passphrase.")); return; }
            string summary = Strings.Format("Import preview: {0} Agents, {1} safe networks, {2} application settings, {3} unavailable Agents.", preview.AgentCount, preview.SafeNetworkCount, preview.ApplicationSettingCount, preview.UnknownAgentIds.Count);
            if (MessageBox.Show(summary + Environment.NewLine + Strings.Get("Import these settings? A verified safety backup will be created first."), Strings.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            string backupDirectory = Path.Combine(Path.GetDirectoryName(Database.Instance.DataSource) ?? AppContext.BaseDirectory, "Backups", "ConfigurationImport");
            await RunAsync(() => transfer.ImportFromFile(dialog.FileName, backupDirectory, passphrase.Text), Strings.Get("Settings imported successfully. Restart the service to apply all Agent settings."));
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceError("Configuration import failed: {0}", exception); ShowError(Strings.Get("Configuration import failed. No settings were applied.")); }
    }

    private async Task RunAsync(Action operation, string success)
    {
        SetEnabled(false);
        status.Text = Strings.Get("Configuration transfer is running...");
        try { await Task.Run(operation); status.Text = success; }
        catch (Exception exception) { System.Diagnostics.Trace.TraceError("Configuration transfer failed: {0}", exception); ShowError(Strings.Get("Configuration transfer failed. Review the application log for details.")); }
        finally { SetEnabled(true); }
    }

    private async Task RunAsync<T>(Func<T> operation, string success) => await RunAsync(() => { _ = operation(); }, success);
    private void SetEnabled(bool enabled) { exportButton.Enabled = enabled; importButton.Enabled = enabled; includeSecrets.Enabled = enabled; passphrase.Enabled = enabled; }
    private void ShowError(string message) { status.Text = message; MessageBox.Show(message, Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    private static bool IsServiceStopped() { try { using ServiceController controller = new(Globals.WINDOWS_SERVICE_NAME); controller.Refresh(); return controller.Status == ServiceControllerStatus.Stopped; } catch (InvalidOperationException) { return true; } }
    private static Label Label(string text, float size, Color color, int x, int y) => new() { AutoSize = true, Font = new Font("Segoe UI", size), ForeColor = color, Location = new Point(x, y), Text = text };
    private static Button Button(string text, int x, int y) => new() { BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(102, 102, 102), Location = new Point(x, y), Size = new Size(120, 28), Text = text, UseVisualStyleBackColor = false };
}
