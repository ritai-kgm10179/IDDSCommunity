using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public sealed class PanelDatabaseMaintenance : UserControl
{
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);
    private readonly SqliteMaintenanceService maintenance = new(Database.Instance);
    private readonly Label statusLabel;
    private readonly Button checkButton;
    private readonly Button backupButton;
    private readonly Button optimizeButton;
    private readonly Button purgeButton;
    private readonly Button restoreButton;

    public PanelDatabaseMaintenance()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        Controls.Add(CreateLabel(Strings.Get("Database maintenance"), 11F, Color.FromArgb(19, 184, 166), new Point(11, 8)));
        Controls.Add(CreateLabel(Strings.Get("SQLite database health, verified backups, retention cleanup, and safe optimization."), 9F, BodyTextColor, new Point(15, 43)));

        statusLabel = CreateLabel(Strings.Get("Reading database status..."), 9F, BodyTextColor, new Point(15, 73));
        statusLabel.AutoSize = false;
        statusLabel.Size = new Size(430, 94);
        Controls.Add(statusLabel);

        checkButton = CreateButton(Strings.Get("Run integrity check"), new Point(15, 178));
        backupButton = CreateButton(Strings.Get("Create verified backup"), new Point(145, 178));
        optimizeButton = CreateButton(Strings.Get("Optimize database"), new Point(275, 178));
        purgeButton = CreateButton(Strings.Get("Clean expired data"), new Point(15, 216));
        restoreButton = CreateButton(Strings.Get("Restore backup"), new Point(145, 216));
        checkButton.Click += async (_, _) => await RunAsync(() => maintenance.GetStatus(true), ShowStatus);
        backupButton.Click += async (_, _) => await RunAsync(CreateBackup, result =>
            statusLabel.Text = Strings.Format("Verified backup created: {0}", result.FilePath));
        optimizeButton.Click += async (_, _) => await RunAsync(() => { maintenance.Optimize(); return maintenance.GetStatus(); }, ShowStatus);
        purgeButton.Click += async (_, _) => await RunAsync(() => maintenance.PurgeExpired(new DatabaseRetentionPolicy()), result =>
            statusLabel.Text = Strings.Format("Expired rows removed: {0}", string.Join(", ", result)));
        restoreButton.Click += RestoreBackup;
        Controls.Add(checkButton);
        Controls.Add(backupButton);
        Controls.Add(optimizeButton);
        Controls.Add(purgeButton);
        Controls.Add(restoreButton);
    }

    public void RefreshStatus() => _ = RunAsync(() => maintenance.GetStatus(), ShowStatus);

    private DatabaseBackupResult CreateBackup()
    {
        string databaseDirectory = Path.GetDirectoryName(Database.Instance.DataSource) ?? AppContext.BaseDirectory;
        return maintenance.CreateVerifiedBackup(Path.Combine(databaseDirectory, "Backups"));
    }

    private void ShowStatus(DatabaseMaintenanceStatus status)
    {
        statusLabel.Text = Strings.Format(
            "Database status summary",
            status.IntegrityResult,
            status.DatabaseBytes,
            status.WalBytes,
            status.PageCount,
            status.FreePageCount,
            status.JournalMode);
    }

    private async Task RunAsync<T>(Func<T> operation, Action<T> success)
    {
        SetButtonsEnabled(false);
        statusLabel.Text = Strings.Get("Database maintenance is running...");
        try
        {
            T result = await Task.Run(operation);
            success(result);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Database maintenance failed: {0}", exception);
            statusLabel.Text = Strings.Get("Database maintenance failed. Review the application log for details.");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void RestoreBackup(object? sender, EventArgs e)
    {
        if (!IsServiceStopped())
        {
            MessageBox.Show(Strings.Get("Stop the IDDS Community service before restoring a database backup."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string databaseDirectory = Path.GetDirectoryName(Database.Instance.DataSource) ?? AppContext.BaseDirectory;
        using OpenFileDialog dialog = new()
        {
            Filter = Strings.Get("SQLite backup files (*.db)|*.db|All files (*.*)|*.*"),
            InitialDirectory = Path.Combine(databaseDirectory, "Backups"),
            RestoreDirectory = true,
            Title = Strings.Get("Select a verified database backup")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(Strings.Get("Restore the selected database backup? The current database will be preserved as a rollback copy."), Strings.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        string backupPath = dialog.FileName;
        await RunAsync(
            () => maintenance.RestoreVerifiedBackup(backupPath, Path.Combine(databaseDirectory, "Backups", "Rollback")),
            result => statusLabel.Text = Strings.Format("Database restored. Rollback copy: {0}", result.FilePath));
    }

    private static bool IsServiceStopped()
    {
        try
        {
            using ServiceController controller = new(Globals.WINDOWS_SERVICE_NAME);
            controller.Refresh();
            return controller.Status == ServiceControllerStatus.Stopped;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        checkButton.Enabled = enabled;
        backupButton.Enabled = enabled;
        optimizeButton.Enabled = enabled;
        purgeButton.Enabled = enabled;
        restoreButton.Enabled = enabled;
    }

    private static SmartLabel CreateLabel(string text, float size, Color color, Point location) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", size),
        ForeColor = color,
        Location = location,
        Text = text
    };

    private static Button CreateButton(string text, Point location) => new()
    {
        BackColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F),
        ForeColor = BodyTextColor,
        Location = location,
        Size = new Size(120, 28),
        Text = text,
        UseVisualStyleBackColor = false
    };
}
