using System;
using System.Drawing;
using Cyberarms.IntrusionDetection.Shared;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class CyberarmsAgentConfiguration : UserControl
{
    public event EventHandler? PluginsChanged;
    public event EventHandler? AgentSettingsChanged;

    public CyberarmsAgentConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        cyberarmsSettingsNavigation.PluginsChanged += new EventHandler(cyberarmsSettingsNavigation_PluginsChanged);
    }

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

    void _pluginConfigPanel_AgentConfigurationChanged(object? sender, EventArgs e) => OnAgentSettingsChanged();

    void OnAgentSettingsChanged() => AgentSettingsChanged?.Invoke(this, EventArgs.Empty);

    void _pluginConfigPanel_AgentChanged(object? sender, EventArgs e)
    {
        //OnAgentSettingsChanged();
    }

    private void OnPluginsChanged() => PluginsChanged?.Invoke(this, EventArgs.Empty);

    public void ClearSecurityAgents() => cyberarmsSettingsNavigation.Clear();

    public void LoadSecurityAgent(SecurityAgent agent) => cyberarmsSettingsNavigation.AddNavigationItem(agent.DisplayName, agent.SelectedIcon, agent.UnselectedIcon);

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
