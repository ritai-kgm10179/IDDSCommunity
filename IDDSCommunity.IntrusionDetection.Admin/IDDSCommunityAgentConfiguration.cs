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
    /// 初始化 <see cref="IDDSCommunityAgentConfiguration"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunityAgentConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        iddscommunitySettingsNavigation.PluginsChanged += new EventHandler(iddscommunitySettingsNavigation_PluginsChanged);
    }
    /// <summary>
    /// 處理 plugins changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
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
    /// 處理 agent configuration changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void _pluginConfigPanel_AgentConfigurationChanged(object? sender, EventArgs e) => OnAgentSettingsChanged();
    /// <summary>
    /// Processes the agent settings changed notification.
    /// </summary>
    void OnAgentSettingsChanged() => AgentSettingsChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 處理 agent changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void _pluginConfigPanel_AgentChanged(object? sender, EventArgs e)
    {
        //OnAgentSettingsChanged();
    }
    /// <summary>
    /// Processes the plugins changed notification.
    /// </summary>
    private void OnPluginsChanged() => PluginsChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 自動刷寫並持久化當前控制項中尚未儲存的 Agent 設定變更。
    /// </summary>
    public void FlushUnsavedChanges() => PluginConfigPanel.FlushUnsavedChanges();
    /// <summary>
    /// Clears security agents.
    /// </summary>
    public void ClearSecurityAgents() => iddscommunitySettingsNavigation.Clear();
    /// <summary>
    /// Loads security agent.
    /// </summary>
    /// <param name="agent">agent 的值。</param>
    public void LoadSecurityAgent(SecurityAgent agent) => iddscommunitySettingsNavigation.AddNavigationItem(agent.DisplayName, agent.SelectedIcon, agent.UnselectedIcon);
    /// <summary>
    /// 執行 show agent config 作業。
    /// </summary>
    /// <param name="agent">agent 的值。</param>
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
    /// 處理 navigation changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
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
