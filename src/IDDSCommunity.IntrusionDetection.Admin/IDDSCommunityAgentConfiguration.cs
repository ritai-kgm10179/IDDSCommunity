using System;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供安全性代理程式組態設定與啟用狀態管理之使用者控制項。
/// </summary>
public partial class IDDSCommunityAgentConfiguration : UserControl
{
        /// <summary>
    /// 當 AgentSettingsChanged 時引發之事件。
    /// </summary>
public event EventHandler? AgentSettingsChanged;
    /// <summary>
    /// 初始化 <see cref="IDDSCommunityAgentConfiguration"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunityAgentConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
    }
    /// <summary>
    /// 手動排列導覽清單與設定內容面板。AutoScaleMode.Font 會依實際字型度量放大
    /// iddscommunitySettingsNavigation 這種顯式設定 Size 的 Dock=Left 子控制項，但 Dock=Fill 的
    /// configurationPanel 並未跟著扣除放大後的寬度，因此改為手動依 iddscommunitySettingsNavigation
    /// 目前的實際寬度計算 configurationPanel 的 Bounds，不再依賴 Dock=Fill 的自動計算。
    /// </summary>
    /// <param name="levent">版面配置事件資料。</param>
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        // 「設定」分頁（IDDSCommunityApplicationSettings）用 Anchor + 固定座標排列導覽清單與內容面板，
        // 兩者之間設計時就留了 8px 的呼吸空間（Location 280 - (Location 12 + Size 260) = 8）。
        // 這裡改用 LogicalToDeviceUnits 讓同樣的 8px 間距隨 DPI/AutoScale 縮放，維持與「設定」分頁一致的視覺風格。
        int gap = LogicalToDeviceUnits(8);
        int navRight = iddscommunitySettingsNavigation.Right + gap;
        configurationPanel.Bounds = new Rectangle(
            navRight,
            Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Right - navRight),
            Math.Max(0, ClientSize.Height - Padding.Vertical));
    }

    private PanelPluginConfiguration? _pluginConfigPanel;

        /// <summary>
    /// 取得或設定 PluginConfigPanel。
    /// </summary>
public PanelPluginConfiguration PluginConfigPanel
    {
        get
        {
            if (_pluginConfigPanel == null)
            {
                _pluginConfigPanel = new PanelPluginConfiguration
                {
                    Dock = DockStyle.Fill
                };
                _pluginConfigPanel.AgentChanged += new EventHandler(_pluginConfigPanel_AgentChanged);
                _pluginConfigPanel.AgentConfigurationChanged += new EventHandler(_pluginConfigPanel_AgentConfigurationChanged);
                configurationPanel.Controls.Add(_pluginConfigPanel);
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
    /// 自動刷寫並持久化當前控制項中尚未儲存的 Agent 設定變更。
    /// </summary>
    public Task<bool> FlushUnsavedChangesAsync() => PluginConfigPanel.FlushUnsavedChangesAsync();
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
    public async Task<bool> ShowAgentConfigAsync(SecurityAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        if (!await PluginConfigPanel.FlushUnsavedChangesAsync()) return false;
        await Task.Run(() =>
        {
            if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
        });
        if (!await PluginConfigPanel.SwitchAgentAsync(agent)) return false;
        iddscommunitySettingsNavigation.SetSelectedItem(agent.DisplayName);
        return true;
    }

    /// <summary>
    /// 同步顯示指定 Agent 設定，供初始畫面建立與不具非同步事件來源的相容呼叫使用。
    /// </summary>
    /// <param name="agent">要顯示的 Agent。</param>
    public void ShowAgentConfig(SecurityAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        if (!agent.CheckConfigVersionById()) agent.CheckConfigVersionByName();
        PluginConfigPanel.Agent = agent;
        iddscommunitySettingsNavigation.SetSelectedItem(agent.DisplayName);
    }
    /// <summary>
    /// 處理 navigation changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void iddscommunitySettingsNavigation_NavigationChanged(object sender, EventArgs e)
    {
        if (iddscommunitySettingsNavigation.SelectedItem != null && !string.IsNullOrEmpty(iddscommunitySettingsNavigation.SelectedItem.DisplayName))
        {
            SecurityAgent? agent = SecurityAgents.Instance.FindByDisplayName(iddscommunitySettingsNavigation.SelectedItem.DisplayName);
            if (agent is not null && !await ShowAgentConfigAsync(agent))
                iddscommunitySettingsNavigation.SetSelectedItem(PluginConfigPanel.Agent.DisplayName);
        }
    }
}
