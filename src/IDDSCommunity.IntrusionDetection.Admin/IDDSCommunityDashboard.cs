using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供系統整體防護狀態、失敗統計與圖表儀表板之使用者控制項。
/// </summary>
public partial class IDDSCommunityDashboard : UserControl
{
    private readonly SmartLabel labelCrossAgentAlerts = new();
    /// <summary>
    /// 當 SecurityAgentConfigurationRequest 時引發之事件。
    /// </summary>
    public event EventHandler? SecurityAgentConfigurationRequest;
    /// <summary>
    /// 初始化 <see cref="IDDSCommunityDashboard"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunityDashboard()
    {
        InitializeComponent();
        labelCrossAgentAlerts.Font = new System.Drawing.Font("Segoe UI", 8F);
        labelCrossAgentAlerts.ForeColor = System.Drawing.Color.White;
        labelCrossAgentAlerts.Location = new System.Drawing.Point(150, 8);
        labelCrossAgentAlerts.Size = new System.Drawing.Size(92, 32);
        labelCrossAgentAlerts.TextAlign = System.Drawing.ContentAlignment.TopRight;
        panelUnsuccessfulLogins.Controls.Add(labelCrossAgentAlerts);
        SetCrossAgentAlerts(0);
    }
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
    /// 設定最近 30 天跨代理程式密碼噴灑告警數量。
    /// </summary>
    /// <param name="alerts">告警數量。</param>
    public void SetCrossAgentAlerts(int alerts) => labelCrossAgentAlerts.Text = Shared.Localization.Strings.Format("Spray alerts: {0}", alerts);

    /// <summary>
    /// 將同一時間區間的登入失敗計數套用至所有 Agent 項目。
    /// </summary>
    /// <param name="attemptsByAgent">以 Agent 識別碼索引的登入失敗數量。</param>
    /// <param name="lockStatisticsByAgent">以 Agent 識別碼索引的累計封鎖統計資料。</param>
    public void SetAgentStatistics(
        IReadOnlyDictionary<Guid, int> attemptsByAgent,
        IReadOnlyDictionary<Guid, AgentLockStatistics> lockStatisticsByAgent)
    {
        RefreshAgentPresentations();
        foreach (Control control in flowLayoutPanelPlugins.Controls)
        {
            if (control is PluginItem item)
            {
                Guid agentId = item.SecurityAgent.Id;
                AgentLockStatistics locks = lockStatisticsByAgent.GetValueOrDefault(agentId) ?? new AgentLockStatistics(0, 0);
                item.SetStatistics(attemptsByAgent.GetValueOrDefault(agentId), locks.HardLocks, locks.SoftLocks);
            }
        }
    }

    /// <summary>
    /// 立即依目前 Agent 設定重新整理所有狀態呈現。
    /// </summary>
    public void RefreshAgentPresentations()
    {
        foreach (Control control in flowLayoutPanelPlugins.Controls)
        {
            if (control is PluginItem item)
                item.RefreshPresentation();
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
    public void ClearAgents()
    {
        while (flowLayoutPanelPlugins.Controls.Count > 0)
        {
            Control child = flowLayoutPanelPlugins.Controls[0];
            flowLayoutPanelPlugins.Controls.RemoveAt(0);
            child.Dispose();
        }
    }

}
