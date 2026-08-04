using System;
using System.Drawing;
using Cyberarms.IntrusionDetection.Shared;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class PluginItem : UserControl
{
    public event EventHandler? SecurityAgentConfigurationRequest;

    public PluginItem() => InitializeComponent();

    public void SetSoftLocks(int softLocks) => labelSoftLocksValue.Text = softLocks.ToString();


    public void SetHardLocks(int hardLocks) => labelHardLocksValue.Text = hardLocks.ToString();

    public void SetName(string name) => labelAgentName.Text = name;

    public void SetIcon(Image? icon) => pictureBoxAgentIcon.Image = icon;


    public void SetFailedLogins(int failedLogins) => labelFailedLoginsValue.Text = failedLogins.ToString();

    private SecurityAgent? _securityAgent;
    public SecurityAgent SecurityAgent
    {
        get => _securityAgent ?? throw new InvalidOperationException("Security agent has not been assigned.");
        set
        {
            _securityAgent = value;
            UpdateValues(value);
            _securityAgent.StatisticsUpdated += new EventHandler(_securityAgent_StatisticsUpdated);
        }
    }

    public void UpdateValues(string displayName, int failedLogins, int hardLocks, int softLocks, Image? icon)
    {
        SetName(displayName);
        SetFailedLogins(failedLogins);
        SetHardLocks(hardLocks);
        SetSoftLocks(softLocks);
        SetIcon(icon);
        pictureBoxEnabledState.Image = SecurityAgent.Enabled ?
                        Properties.Resources.status_agent_enabled_dark :
                        Properties.Resources.status_agent_disabled_dark;
        toolTip1.SetToolTip(pictureBoxEnabledState, string.Format("The security agent {0} is {1}. Double-click to configure this agent.",
            SecurityAgent.DisplayName, SecurityAgent.Enabled ? "enabled" : "disabled"));
    }

    public void UpdateValues(SecurityAgent agent)
    {
        if (agent != null)
        {
            UpdateValues(agent.DisplayName, agent.FailedLogins, agent.HardLocks, agent.SoftLocks, agent.Icon);
        }
        else
        {
            UpdateValues("", 0, 0, 0, null);
        }
    }

    void _securityAgent_StatisticsUpdated(object? sender, EventArgs e) => UpdateValues(SecurityAgent);

    private void toolTip1_Popup(object sender, PopupEventArgs e)
    {

    }

    private void pictureBoxEnabledState_Click(object sender, EventArgs e)
    {

    }

    private void pictureBoxEnabledState_DoubleClick(object sender, EventArgs e) => SecurityAgentConfigurationRequest?.Invoke(SecurityAgent, EventArgs.Empty);



}
