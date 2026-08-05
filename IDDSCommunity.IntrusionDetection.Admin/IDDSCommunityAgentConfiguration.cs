using System;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunityAgentConfiguration : UserControl
{
    public event EventHandler? PluginsChanged;
    public event EventHandler? AgentSettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="IDDSCommunityAgentConfiguration"/> class.
    /// </summary>

    public IDDSCommunityAgentConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        iddscommunitySettingsNavigation.PluginsChanged += new EventHandler(iddscommunitySettingsNavigation_PluginsChanged);
    }

    /// <summary>
    /// Handles the plugins changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void iddscommunitySettingsNavigation_PluginsChanged(object? sender, EventArgs e) => PluginsChanged?.Invoke(sender, e);

    private PanelPluginConfiguration? _pluginConfigPanel;

    public PanelPluginConfiguration PluginConfigPanel
    {
        get
        {
            if (_pluginConfigPanel == null)
            {
                _pluginConfigPanel = new PanelPluginConfiguration();
                configurationPanel.Controls.Add(_pluginConfigPanel);
                _pluginConfigPanel.Dock = DockStyle.Fill;
                _pluginConfigPanel.AgentChanged += new EventHandler(_pluginConfigPanel_AgentChanged);
                _pluginConfigPanel.AgentConfigurationChanged += new EventHandler(_pluginConfigPanel_AgentConfigurationChanged);
            }
            return _pluginConfigPanel;
        }
    }

    /// <summary>
    /// Handles the agent configuration changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _pluginConfigPanel_AgentConfigurationChanged(object? sender, EventArgs e) => OnAgentSettingsChanged();

    /// <summary>
    /// Processes the agent settings changed notification.
    /// </summary>

    void OnAgentSettingsChanged() => AgentSettingsChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Handles the agent changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void _pluginConfigPanel_AgentChanged(object? sender, EventArgs e)
    {
        //OnAgentSettingsChanged();
    }

    /// <summary>
    /// Processes the plugins changed notification.
    /// </summary>

    private void OnPluginsChanged() => PluginsChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Clears security agents.
    /// </summary>

    public void ClearSecurityAgents() => iddscommunitySettingsNavigation.Clear();

    /// <summary>
    /// Loads security agent.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void LoadSecurityAgent(SecurityAgent agent) => iddscommunitySettingsNavigation.AddNavigationItem(agent.DisplayName, agent.SelectedIcon, agent.UnselectedIcon);

    /// <summary>
    /// Executes the show agent config operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void ShowAgentConfig(SecurityAgent agent)
    {
        if (agent != null)
        {
            if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
            iddscommunitySettingsNavigation.SetSelectedItem(agent.DisplayName);
        }
        if (agent is not null)
            PluginConfigPanel.Agent = agent;
    }

    /// <summary>
    /// Handles the navigation changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void iddscommunitySettingsNavigation_NavigationChanged(object sender, EventArgs e)
    {
        if (iddscommunitySettingsNavigation.SelectedItem != null && !string.IsNullOrEmpty(iddscommunitySettingsNavigation.SelectedItem.DisplayName))
        {
            SecurityAgent? agent = SecurityAgents.Instance.FindByDisplayName(iddscommunitySettingsNavigation.SelectedItem.DisplayName);
            if (agent != null)
            {
                if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
            }
            if (agent is not null)
                PluginConfigPanel.Agent = agent;
        }
    }
}
