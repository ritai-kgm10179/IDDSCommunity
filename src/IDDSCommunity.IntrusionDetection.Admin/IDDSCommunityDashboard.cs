using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunityDashboard : UserControl
{
    public event EventHandler? SecurityAgentConfigurationRequest;
    /// <summary>
    /// 初始化 <see cref="IDDSCommunityDashboard"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunityDashboard() => InitializeComponent();
    /// <summary>
    /// Sets soft locks.
    /// </summary>
    /// <param name="locks">locks 的值。</param>
    public void SetSoftLocks(int locks) => labelSoftLocks.Text = locks.ToString();
    /// <summary>
    /// Sets hard locks.
    /// </summary>
    /// <param name="locks">locks 的值。</param>
    public void SetHardLocks(int locks) => labelHardLocks.Text = locks.ToString();
    /// <summary>
    /// Sets unsuccessful logins.
    /// </summary>
    /// <param name="logins">logins 的值。</param>
    public void SetUnsuccessfulLogins(int logins) => labelUnsuccessfulLogins.Text = logins.ToString();

    /// <summary>
    /// 將同一時間區間的登入失敗計數套用至所有 Agent 項目。
    /// </summary>
    /// <param name="attemptsByAgent">以 Agent 識別碼索引的登入失敗數量。</param>
    /// <param name="lockStatisticsByAgent">以 Agent 識別碼索引的累計封鎖統計資料。</param>
    public void SetAgentStatistics(
        IReadOnlyDictionary<Guid, int> attemptsByAgent,
        IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatisticsByAgent)
    {
        foreach (Control control in flowLayoutPanelPlugins.Controls)
        {
            if (control is PluginItem item)
            {
                Guid agentId = item.SecurityAgent.Id;
                AgentLockStatistics locks = lockStatisticsByAgent.GetValueOrDefault(agentId) ?? new AgentLockStatistics(0, 0);
                item.RefreshPresentation();
                item.SetStatistics(attemptsByAgent.GetValueOrDefault(agentId), locks.HardLocks, locks.SoftLocks);
            }
        }
    }

    /// <summary>
    /// Adds agent.
    /// </summary>
    /// <param name="agent">agent 的值。</param>
    public void AddAgent(SecurityAgent agent)
    {
        PluginItem agentX = new()
        {
            SecurityAgent = agent
        };
        flowLayoutPanelPlugins.Controls.Add(agentX);
        agentX.SecurityAgentConfigurationRequest += new EventHandler(agentX_SecurityAgentConfigurationRequest);
    }
    /// <summary>
    /// 處理 security agent configuration request 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void agentX_SecurityAgentConfigurationRequest(object? sender, EventArgs e) => SecurityAgentConfigurationRequest?.Invoke(sender, e);
    /// <summary>
    /// Clears agents.
    /// </summary>
    public void ClearAgents() => flowLayoutPanelPlugins.Controls.Clear();

}
