using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class PanelPluginConfiguration : UserControl
{
    public event EventHandler? AgentChanged;
    public event EventHandler? AgentConfigurationChanged;
    /// <summary>
    /// Initializes a new instance of the <see cref="PanelPluginConfiguration"/> class.
    /// </summary>

    public PanelPluginConfiguration()
    {
        InitializeComponent();
        flowLayoutPanelCustomPluginSettings.ClientSizeChanged += (_, _) => UpdateCustomSettingsLayout();
        AgentChanged += new EventHandler(PanelPluginConfiguration_AgentChanged);
    }

    /// <summary>
    /// Handles the agent changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void PanelPluginConfiguration_AgentChanged(object? sender, EventArgs e)
    {
        LoadData();
        smartLabelAgentName.Text = Agent.DisplayName;
        ClearErrors();
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X + 1, pictureBox.Location.Y + 1);
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X - 1, pictureBox.Location.Y - 1);
    }

    public bool IsInEditMode { get; set; }

    /// <summary>
    /// Loads data.
    /// </summary>

    private void LoadData()
    {
        if (IsInEditMode) ToggleEditMode();
        checkBoxLockForever.Checked = Agent.LockForever;
        textBoxHardLocks.Text = Agent.HardLockAttempts.ToString();
        textBoxHardLockDuration.Text = Agent.HardLockTimeHours.ToString();
        textBoxSoftLocks.Text = Agent.SoftLockAttempts.ToString();
        textBoxSoftLockDuration.Text = Agent.SoftLockTimeMinutes.ToString();
        checkBoxEnableSecurityAgent.Checked = Agent.Enabled;
        checkBoxOverrideConfiguration.Checked = Agent.OverrideConfig;
        SetEnabledMode(checkBoxOverrideConfiguration.Checked);
        LoadCustomSettings();
        smartLabelCustomConfig.Visible = flowLayoutPanelCustomPluginSettings.Controls.Count > 0;
        SetEditMode(false);
    }

    /// <summary>
    /// Loads custom settings.
    /// </summary>

    private void LoadCustomSettings()
    {
        flowLayoutPanelCustomPluginSettings.Controls.Clear();
        string? protectionDetails = GetProtectionDetails(Agent.Name);
        if (protectionDetails is not null)
        {
            Label details = new()
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(102, 102, 102),
                Margin = new Padding(0, 2, 0, 8),
                Size = new Size(390, 38),
                Text = protectionDetails
            };
            flowLayoutPanelCustomPluginSettings.Controls.Add(details);
        }
        foreach (string propName in Agent.CustomConfiguration.Keys)
        {
            string propertyType = Agent.CustomConfigurationTypes.TryGetValue(propName, out string? declaredType)
                ? declaredType
                : typeof(string).FullName!;
            PluginSettingEditor editor = new(propName, propertyType, Agent.CustomConfiguration[propName], Agent.Name);
            editor.ValueChanged += (_, _) => SetEditMode(true);
            flowLayoutPanelCustomPluginSettings.Controls.Add(editor);
        }
        flowLayoutPanelCustomPluginSettings.Controls.Add(new Panel
        {
            Height = 16,
            Margin = Padding.Empty,
            TabStop = false
        });
        UpdateCustomSettingsLayout();
    }

    /// <summary>
    /// Keeps custom setting rows inside the visible client area at all DPI scaling levels.
    /// </summary>
    private void UpdateCustomSettingsLayout()
    {
        int availableWidth = Math.Max(240, flowLayoutPanelCustomPluginSettings.ClientSize.Width
            - flowLayoutPanelCustomPluginSettings.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2);
        foreach (Control control in flowLayoutPanelCustomPluginSettings.Controls)
            control.Width = availableWidth;
    }

    /// <summary>
    /// Gets a localized description of the authoritative detection source and encrypted-traffic limitations.
    /// </summary>
    /// <param name="agentName">The fully qualified Agent type name.</param>
    /// <returns>The localized details, or <see langword="null"/> when no specialized disclosure is required.</returns>
    private static string? GetProtectionDetails(string agentName) => agentName switch
    {
        "IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher" => Strings.Get("Detection source: Windows Application Event Log, Event ID 18456. The database port is not scanned."),
        "IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher" => Strings.Get("Detection source: Windows Application Event Log, Event ID 100. The database port is not scanned."),
        "IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent" => Strings.Get("Detection source: Windows Application Event Log, Event ID 661. The database port is not scanned."),
        "IDDSCommunity.Agents.MailServer.ImapAgent" => Strings.Get("Inspects cleartext IMAP on the configured port. Parsing stops after STARTTLS; implicit TLS on port 993 requires server-side logs."),
        _ => null
    };

    /// <summary>
    /// Saves custom configuration.
    /// </summary>

    private void SaveCustomConfiguration()
    {
        foreach (Control o in flowLayoutPanelCustomPluginSettings.Controls)
        {
            if (o is PluginSettingEditor setting)
            {
                Agent.CustomConfiguration[setting.PropertyName] = setting.Value;
            }
        }
    }


    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData(); else ToggleEditMode();
        ClearErrors();
    }

    /// <summary>
    /// Executes the toggle edit mode operation.
    /// </summary>

    private void ToggleEditMode()
    {
        IsInEditMode = true;
        return;
    }

    /// <summary>
    /// Sets enabled mode.
    /// </summary>
    /// <param name="enabled">The enabled value.</param>

    public void SetEnabledMode(bool enabled)
    {
        textBoxHardLockDuration.Enabled = enabled;
        textBoxHardLocks.Enabled = enabled;
        textBoxSoftLockDuration.Enabled = enabled;
        textBoxSoftLocks.Enabled = enabled;
        checkBoxLockForever.Enabled = enabled;
    }

    /// <summary>
    /// Clears errors.
    /// </summary>

    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxSave_Click(object sender, EventArgs e)
    {
        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxHardLocks.Text, out int hardLocks))
        {
            errHardLocks.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxHardLockDuration.Text, out int hardLockDuration))
        {
            errHardLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLockDuration.Text, out int softLockDuration))
        {
            errSoftLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLocks.Text, out int softLocks))
        {
            errSoftLocks.Visible = true;
            hasError = true;
        }
        if (!hasError)
        {
            Agent.LockForever = checkBoxLockForever.Checked;
            Agent.HardLockAttempts = hardLocks;
            Agent.HardLockTimeHours = hardLockDuration;
            Agent.SoftLockAttempts = softLocks;
            Agent.SoftLockTimeMinutes = softLockDuration;
            Agent.Enabled = checkBoxEnableSecurityAgent.Checked;
            Agent.OverrideConfig = checkBoxOverrideConfiguration.Checked;
            SaveCustomConfiguration();
            if (!ValidateCustomConfiguration())
            {
                SetEditMode(true);
                return;
            }
            Agent.Save();
            OnAgentConfigurationChanged();

        }
        SetEditMode(false);
    }

    private bool ValidateCustomConfiguration()
    {
        Dictionary<string, string> values = Agent.CustomConfiguration;
        if (TryInteger(values, "WindowSeconds", out int windowSeconds)
            && TryInteger(values, "SourceStateRetentionSeconds", out int retentionSeconds)
            && retentionSeconds < windowSeconds)
            return ShowCustomValidationError("Source state retention must not be shorter than the detection window.");

        if (values.TryGetValue("LogDirectory", out string? directory)
            && (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory)))
            return ShowCustomValidationError("Select an absolute log directory.");

        if (values.TryGetValue("LogFilePath", out string? filePath)
            && !string.IsNullOrWhiteSpace(filePath)
            && !Path.IsPathFullyQualified(filePath))
            return ShowCustomValidationError("Select an absolute log file path.");

        if (values.TryGetValue("SearchPattern", out string? pattern)
            && (string.IsNullOrWhiteSpace(pattern) || pattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            return ShowCustomValidationError("Enter a valid log file search pattern.");

        if (values.TryGetValue("ReadEventLog", out string? readEventLog)
            && bool.TryParse(readEventLog, out bool shouldReadEventLog)
            && !shouldReadEventLog
            && values.TryGetValue("LogFilePath", out filePath)
            && string.IsNullOrWhiteSpace(filePath))
            return ShowCustomValidationError("Enable Windows event log reading or select a log file.");

        return true;
    }

    private static bool TryInteger(IReadOnlyDictionary<string, string> values, string key, out int result)
    {
        result = default;
        return values.TryGetValue(key, out string? value)
            && int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result);
    }

    private bool ShowCustomValidationError(string key)
    {
        MessageBox.Show(this, Strings.Get(key), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    /// <summary>
    /// Processes the agent configuration changed notification.
    /// </summary>

    private void OnAgentConfigurationChanged() => AgentConfigurationChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Processes the agent changed notification.
    /// </summary>

    private void OnAgentChanged() => AgentChanged?.Invoke(this, EventArgs.Empty);

    private SecurityAgent? _agent;
    public SecurityAgent Agent
    {
        get => _agent ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Security agent has not been assigned."));
        set
        {
            _agent = value;
            AgentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();

    /// <summary>
    /// Handles the checked changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void checkBoxOverrideConfiguration_CheckedChanged(object sender, EventArgs e)
    {
        SetEnabledMode(checkBoxOverrideConfiguration.Checked);
        SetEditMode(true);
    }


    /// <summary>
    /// Handles the key press event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void textBox_KeyPress(object? sender, KeyPressEventArgs e) => SetEditMode(true);

    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>

    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }

    /// <summary>
    /// Handles the checked changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void checkBox_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);

}
