using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal sealed class SetupForm : Form
{
    private static readonly Color Navy = Color.FromArgb(17, 37, 61);
    private static readonly Color Teal = Color.FromArgb(19, 184, 166);
    private readonly Button launchAppButton = CreateActionButton(string.Empty, primary: true);
    private readonly Button installButton = CreateActionButton(string.Empty, primary: true);
    private readonly Button uninstallButton = CreateActionButton(string.Empty, primary: false);
    private readonly Button userGuideButton = CreateActionButton(string.Empty, primary: false);
    private readonly Button closeButton = CreateActionButton(string.Empty, primary: false);
    private readonly Button languageButton = CreateActionButton(string.Empty, primary: false);
    private readonly Label titleLabel = CreateLabel(10F, FontStyle.Bold, Teal);
    private readonly Label descriptionLabel = CreateLabel(9F, FontStyle.Regular, Navy);
    private readonly Label locationLabel = CreateLabel(9F, FontStyle.Regular, Color.FromArgb(100, 116, 139));
    private readonly CheckBox checkBoxDesktopShortcut = new() { AutoSize = true, Checked = true };
    private readonly CheckBox checkBoxStartMenuShortcut = new() { AutoSize = true, Checked = true };
    private readonly Label statusLabel = CreateLabel(9.5F, FontStyle.Bold, Navy);
    private readonly Label progressLabel = CreateLabel(9F, FontStyle.Regular, Color.FromArgb(71, 85, 105));
    private readonly ProgressBar progressBar = new() { Dock = DockStyle.Fill, Height = 18, Style = ProgressBarStyle.Continuous };
    private CancellationTokenSource? operationCancellation;
    private bool operationActive;

    /// <summary>
    /// 初始化安裝程式視窗。
    /// </summary>
    internal SetupForm()
    {
        ClientSize = new Size(760, 390);
        MinimumSize = new Size(680, 410);
        BackColor = Color.FromArgb(243, 246, 248);
        ForeColor = Navy;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        AutoScaleMode = AutoScaleMode.Font;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 18, 28, 20),
            ColumnCount = 1,
            RowCount = 8
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        TableLayoutPanel header = new() { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(languageButton, 1, 0);

        FlowLayoutPanel shortcutOptions = new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        shortcutOptions.Controls.AddRange([checkBoxDesktopShortcut, checkBoxStartMenuShortcut]);
        FlowLayoutPanel actions = new() { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        actions.Controls.AddRange([launchAppButton, installButton, uninstallButton, userGuideButton, closeButton]);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(descriptionLabel, 0, 1);
        root.Controls.Add(locationLabel, 0, 2);
        root.Controls.Add(shortcutOptions, 0, 3);
        root.Controls.Add(statusLabel, 0, 4);
        root.Controls.Add(progressLabel, 0, 5);
        root.Controls.Add(progressBar, 0, 6);
        root.Controls.Add(actions, 0, 7);
        Controls.Add(root);

        launchAppButton.Click += (_, _) => SetupOperations.LaunchApp();
        userGuideButton.Click += (_, _) => SetupOperations.OpenUserGuide();
        closeButton.Click += CloseOrCancel;
        installButton.Click += async (_, _) => await ExecuteAsync(true);
        uninstallButton.Click += async (_, _) => await ExecuteAsync(false);
        languageButton.Click += (_, _) => ToggleLanguage();

        CancelButton = closeButton;
        progressBar.Visible = false;
        progressLabel.Visible = false;
        RefreshLocalizedText();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (operationActive)
        {
            e.Cancel = true;
            RequestCancellation();
            return;
        }
        base.OnFormClosing(e);
    }

    private void CloseOrCancel(object? sender, EventArgs e)
    {
        if (operationActive) RequestCancellation();
        else Close();
    }

    private void RequestCancellation()
    {
        if (operationCancellation is null || operationCancellation.IsCancellationRequested) return;
        operationCancellation.Cancel();
        closeButton.Enabled = false;
        progressLabel.Text = SetupText.Get("ProgressCancelling");
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
        closeButton.Text = operationActive ? SetupText.Get("Cancel") : SetupText.Get("Close");
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
            SetInstallButton(SetupText.Get("Install"), Teal, Color.White);
        }
        else if (action == SetupOperations.InstallAction.Upgrade)
        {
            statusLabel.Text = SetupText.Format("StatusUpgradeAvailable", installedVer?.ToString(3) ?? "3.0.0", setupVer.ToString(3));
            statusLabel.ForeColor = Teal;
            SetInstallButton(SetupText.Get("Upgrade"), Teal, Color.White);
        }
        else if (action == SetupOperations.InstallAction.Downgrade)
        {
            statusLabel.Text = SetupText.Format("StatusInstalledWithVersion", installedVer?.ToString(3) ?? setupVer.ToString(3));
            statusLabel.ForeColor = Color.FromArgb(225, 29, 72);
            SetInstallButton(SetupText.Get("Downgrade"), Color.FromArgb(225, 29, 72), Color.White);
        }
        else
        {
            statusLabel.Text = SetupText.Format("StatusInstalledWithVersion", installedVer?.ToString(3) ?? "3.0.0");
            statusLabel.ForeColor = Teal;
            SetInstallButton(SetupText.Get("Reinstall"), Color.White, Navy);
        }

        launchAppButton.Visible = installed && SetupOperations.CanLaunchApp;
        uninstallButton.Visible = installed;
        if (!operationActive)
        {
            checkBoxDesktopShortcut.Checked = installed ? SetupOperations.HasDesktopShortcut : true;
            checkBoxStartMenuShortcut.Checked = installed ? SetupOperations.HasStartMenuShortcut : true;
        }
    }

    private void SetInstallButton(string text, Color background, Color foreground)
    {
        installButton.Text = text;
        installButton.BackColor = background;
        installButton.ForeColor = foreground;
    }

    private static Label CreateLabel(float size, FontStyle style, Color color) => new()
    {
        AutoSize = true,
        MaximumSize = new Size(680, 0),
        Margin = new Padding(4, 6, 4, 8),
        Font = new Font("Segoe UI", size, style, GraphicsUnit.Point),
        ForeColor = color
    };

    private static Button CreateActionButton(string text, bool primary) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(108, 38),
        Margin = new Padding(3),
        FlatStyle = FlatStyle.Flat,
        BackColor = primary ? Teal : Color.White,
        ForeColor = primary ? Color.White : Navy,
        UseVisualStyleBackColor = false
    };

    private void SetBusy(bool busy)
    {
        operationActive = busy;
        launchAppButton.Enabled = !busy;
        installButton.Enabled = !busy;
        uninstallButton.Enabled = !busy;
        userGuideButton.Enabled = !busy;
        languageButton.Enabled = !busy;
        checkBoxDesktopShortcut.Enabled = !busy;
        checkBoxStartMenuShortcut.Enabled = !busy;
        closeButton.Enabled = true;
        closeButton.Text = SetupText.Get(busy ? "Cancel" : "Close");
        progressBar.Visible = busy;
        progressLabel.Visible = busy;
        if (!busy)
        {
            progressBar.Value = 0;
            progressLabel.Text = string.Empty;
        }
    }

    private async Task ExecuteAsync(bool install)
    {
        SetupOperations.InstallAction currentAction = SetupOperations.CurrentInstallAction;
        string actionName = install ? currentAction switch
        {
            SetupOperations.InstallAction.Upgrade => SetupText.Get("Upgrade"),
            SetupOperations.InstallAction.Downgrade => SetupText.Get("Downgrade"),
            SetupOperations.InstallAction.Reinstall => SetupText.Get("Reinstall"),
            _ => SetupText.Get("Install")
        } : SetupText.Get("Uninstall");

        string confirmation = install && currentAction == SetupOperations.InstallAction.Downgrade
            ? SetupText.Format("DowngradeConfirm", (SetupOperations.InstalledVersion ?? new Version(3, 0, 0)).ToString(3), SetupOperations.CurrentSetupVersion.ToString(3))
            : SetupText.Format("Confirm", actionName);
        if (MessageBox.Show(this, confirmation, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        bool desktop = checkBoxDesktopShortcut.Checked;
        bool startMenu = checkBoxStartMenuShortcut.Checked;
        operationCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = operationCancellation.Token;
        Progress<SetupOperations.SetupProgress> progress = new(value =>
        {
            progressLabel.Text = SetupText.Get(value.MessageKey);
            progressBar.Value = Math.Clamp(value.Percentage, progressBar.Minimum, progressBar.Maximum);
        });
        SetBusy(true);
        try
        {
            SetupOperations.SetupOperationResult result = await Task.Run(() => install
                ? SetupOperations.Install(desktop, startMenu, progress, cancellationToken)
                : SetupOperations.Uninstall(progress, cancellationToken), cancellationToken);
            string message = SetupText.Format("Completed", actionName, SetupOperations.InstallDirectory);
            if (result.RestartRequired) message += Environment.NewLine + Environment.NewLine + SetupText.Get("RestartRequired");
            if (result.CleanupIncomplete) message += Environment.NewLine + Environment.NewLine + SetupText.Get("CleanupIncomplete");
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, SetupText.Get("CancelledAndRolledBack"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            string? logPath = RollingDiagnosticLog.Write("Setup", "Setup operation failed", exception);
            string message = SetupText.Format("OperationFailed", exception.Message);
            if (!string.IsNullOrWhiteSpace(logPath)) message += Environment.NewLine + SetupText.Format("DiagnosticLogPath", logPath);
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            operationCancellation.Dispose();
            operationCancellation = null;
            SetBusy(false);
            UpdateStatus();
        }
    }
}
