using System;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class PluginItem : UserControl
{
    public event EventHandler? SecurityAgentConfigurationRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginItem"/> class.
    /// </summary>

    public PluginItem() => InitializeComponent();

    /// <summary>
    /// Sets soft locks.
    /// </summary>
    /// <param name="softLocks">The soft locks value.</param>

    public void SetSoftLocks(int softLocks) => labelSoftLocksValue.Text = softLocks.ToString();


    /// <summary>
    /// Sets hard locks.
    /// </summary>
    /// <param name="hardLocks">The hard locks value.</param>

    public void SetHardLocks(int hardLocks) => labelHardLocksValue.Text = hardLocks.ToString();

    /// <summary>
    /// Sets name.
    /// </summary>
    /// <param name="name">The name value.</param>

    public void SetName(string name) => labelAgentName.Text = name;

    /// <summary>
    /// Sets icon.
    /// </summary>
    /// <param name="icon">The icon value.</param>

    public void SetIcon(Image? icon) => pictureBoxAgentIcon.Image = icon;


    /// <summary>
    /// Sets failed logins.
    /// </summary>
    /// <param name="failedLogins">The failed logins value.</param>

    public void SetFailedLogins(int failedLogins) => labelFailedLoginsValue.Text = failedLogins.ToString();

    private SecurityAgent? _securityAgent;
    public SecurityAgent SecurityAgent
    {
        get => _securityAgent ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Security agent has not been assigned."));
        set
        {
            _securityAgent = value;
            UpdateValues(value);
            _securityAgent.StatisticsUpdated += new EventHandler(_securityAgent_StatisticsUpdated);
        }
    }

    /// <summary>
    /// Updates values.
    /// </summary>
    /// <param name="displayName">The display name value.</param>
    /// <param name="failedLogins">The failed logins value.</param>
    /// <param name="hardLocks">The hard locks value.</param>
    /// <param name="softLocks">The soft locks value.</param>
    /// <param name="icon">The icon value.</param>

    public void UpdateValues(string displayName, int failedLogins, int hardLocks, int softLocks, Image? icon)
    {
        SetName(displayName);
        SetFailedLogins(failedLogins);
        SetHardLocks(hardLocks);
        SetSoftLocks(softLocks);
        SetIcon(icon);
        Image? previousStatusImage = pictureBoxEnabledState.Image;
        pictureBoxEnabledState.Image = InterfaceIcons.CreateAgentStatus(16, SecurityAgent.Enabled);
        previousStatusImage?.Dispose();
        string localizedStatus = Strings.Get(SecurityAgent.Enabled ? "enabled" : "disabled");
        pictureBoxEnabledState.AccessibleName = $"{Strings.Get("Agent status")}: {localizedStatus}";
        pictureBoxEnabledState.AccessibleDescription = Strings.Format(
            "The security agent {0} is {1}. Double-click to configure this agent.",
            SecurityAgent.DisplayName,
            localizedStatus);
        toolTip1.ToolTipTitle = Strings.Get("Agent status");
        toolTip1.SetToolTip(pictureBoxEnabledState, pictureBoxEnabledState.AccessibleDescription);
    }

    /// <summary>
    /// Updates values.
    /// </summary>
    /// <param name="agent">The agent value.</param>

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

    /// <summary>
    /// Handles the statistics updated event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _securityAgent_StatisticsUpdated(object? sender, EventArgs e) => UpdateValues(SecurityAgent);

    /// <summary>
    /// Handles the popup event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void toolTip1_Popup(object sender, PopupEventArgs e)
    {

    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEnabledState_Click(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Handles the double click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEnabledState_DoubleClick(object sender, EventArgs e) => SecurityAgentConfigurationRequest?.Invoke(SecurityAgent, EventArgs.Empty);



}
