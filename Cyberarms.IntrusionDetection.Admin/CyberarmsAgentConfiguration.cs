using System;
using System.Drawing;
using Cyberarms.IntrusionDetection.Shared;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class CyberarmsAgentConfiguration : UserControl
{
    public event EventHandler? PluginsChanged;
    public event EventHandler? AgentSettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="CyberarmsAgentConfiguration"/> class.
    /// </summary>

    public CyberarmsAgentConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        cyberarmsSettingsNavigation.PluginsChanged += new EventHandler(cyberarmsSettingsNavigation_PluginsChanged);
    }

    /// <summary>
    /// Handles the plugins changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void cyberarmsSettingsNavigation_PluginsChanged(object? sender, EventArgs e) => PluginsChanged?.Invoke(sender, e);

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

    public void ClearSecurityAgents() => cyberarmsSettingsNavigation.Clear();

    /// <summary>
    /// Loads security agent.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void LoadSecurityAgent(SecurityAgent agent) => cyberarmsSettingsNavigation.AddNavigationItem(agent.DisplayName, agent.SelectedIcon, agent.UnselectedIcon);

    /// <summary>
    /// Executes the show agent config operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void ShowAgentConfig(SecurityAgent agent)
    {
        if (agent != null)
        {
            if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
            cyberarmsSettingsNavigation.SetSelectedItem(agent.DisplayName);
        }
        if (agent is not null)
            PluginConfigPanel.Agent = agent;
    }

    /// <summary>
    /// Handles the navigation changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void cyberarmsSettingsNavigation_NavigationChanged(object sender, EventArgs e)
    {
        if (cyberarmsSettingsNavigation.SelectedItem != null && !string.IsNullOrEmpty(cyberarmsSettingsNavigation.SelectedItem.DisplayName))
        {
            SecurityAgent? agent = SecurityAgents.Instance.FindByDisplayName(cyberarmsSettingsNavigation.SelectedItem.DisplayName);
            if (agent != null)
            {
                if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
            }
            if (agent is not null)
                PluginConfigPanel.Agent = agent;
        }
    }
}
