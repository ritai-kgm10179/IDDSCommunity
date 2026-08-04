using System;
using System.Drawing;
using Cyberarms.IntrusionDetection.Shared;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class PanelPluginConfiguration : UserControl
{
    public event EventHandler? AgentChanged;
    public event EventHandler? AgentConfigurationChanged;
    public PanelPluginConfiguration()
    {
        InitializeComponent();
        AgentChanged += new EventHandler(PanelPluginConfiguration_AgentChanged);
    }

    void PanelPluginConfiguration_AgentChanged(object? sender, EventArgs e)
    {
        LoadData();
        smartLabelAgentName.Text = Agent.DisplayName;
        ClearErrors();
    }

    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X + 1, pictureBox.Location.Y + 1);
    }

    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is PictureBox pictureBox)
            pictureBox.Location = new Point(pictureBox.Location.X - 1, pictureBox.Location.Y - 1);
    }

    public bool IsInEditMode { get; set; }

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
        smartLabelCustomConfig.Visible = Agent.CustomConfiguration.Count > 0;
        SetEditMode(false);
    }

    private void LoadCustomSettings()
    {
        flowLayoutPanelCustomPluginSettings.Controls.Clear();
        foreach (string propName in Agent.CustomConfiguration.Keys)
        {
            SmartLabelTextbox ltx = new()
            {
                LabelText = propName,
                TextBoxText = Agent.CustomConfiguration[propName]
            };
            flowLayoutPanelCustomPluginSettings.Controls.Add(ltx);
            ltx.TextBoxKeyPress += new KeyPressEventHandler(textBox_KeyPress);
        }
    }

    private void SaveCustomConfiguration()
    {
        foreach (Control o in flowLayoutPanelCustomPluginSettings.Controls)
        {
            if (o is SmartLabelTextbox)
            {
                SmartLabelTextbox setting = (SmartLabelTextbox)o;
                string name = setting.LabelText;
                string value = setting.TextBoxText;
                Agent.CustomConfiguration[name] = value;
            }
        }
    }


    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData(); else ToggleEditMode();
        ClearErrors();
    }

    private void ToggleEditMode()
    {
        IsInEditMode = true;
        return;
    }

    public void SetEnabledMode(bool enabled)
    {
        textBoxHardLockDuration.Enabled = enabled;
        textBoxHardLocks.Enabled = enabled;
        textBoxSoftLockDuration.Enabled = enabled;
        textBoxSoftLocks.Enabled = enabled;
        checkBoxLockForever.Enabled = enabled;
    }

    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }

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

    private void OnAgentConfigurationChanged() => AgentConfigurationChanged?.Invoke(this, EventArgs.Empty);

    private void OnAgentChanged() => AgentChanged?.Invoke(this, EventArgs.Empty);

    private SecurityAgent? _agent;
    public SecurityAgent Agent
    {
        get => _agent ?? throw new InvalidOperationException("Security agent has not been assigned.");
        set
        {
            _agent = value;
            AgentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();

    private void checkBoxOverrideConfiguration_CheckedChanged(object sender, EventArgs e)
    {
        SetEnabledMode(checkBoxOverrideConfiguration.Checked);
        SetEditMode(true);
    }


    private void textBox_KeyPress(object? sender, KeyPressEventArgs e) => SetEditMode(true);

    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }

    private void checkBox_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);

}
