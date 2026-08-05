using System;
using System.Drawing;
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
                Margin = new Padding(3, 3, 3, 6),
                Size = new Size(330, 38),
                Text = protectionDetails
            };
            flowLayoutPanelCustomPluginSettings.Controls.Add(details);
        }
        foreach (string propName in Agent.CustomConfiguration.Keys)
        {
            SmartLabelTextbox ltx = new()
            {
                LabelText = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get(propName),
                TextBoxText = Agent.CustomConfiguration[propName],
                Tag = propName
            };
            flowLayoutPanelCustomPluginSettings.Controls.Add(ltx);
            ltx.TextBoxKeyPress += new KeyPressEventHandler(textBox_KeyPress);
        }
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
            if (o is SmartLabelTextbox)
            {
                SmartLabelTextbox setting = (SmartLabelTextbox)o;
                string name = setting.Tag as string ?? setting.LabelText;
                string value = setting.TextBoxText;
                Agent.CustomConfiguration[name] = value;
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
            Agent.Save();
            OnAgentConfigurationChanged();

        }
        SetEditMode(false);
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
