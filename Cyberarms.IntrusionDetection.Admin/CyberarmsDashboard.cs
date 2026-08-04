using System;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class CyberarmsDashboard : UserControl
{
    public event EventHandler? SecurityAgentConfigurationRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="CyberarmsDashboard"/> class.
    /// </summary>

    public CyberarmsDashboard() => InitializeComponent();

    /// <summary>
    /// Sets soft locks.
    /// </summary>
    /// <param name="locks">The locks value.</param>

    public void SetSoftLocks(int locks) => labelSoftLocks.Text = locks.ToString();

    /// <summary>
    /// Sets hard locks.
    /// </summary>
    /// <param name="locks">The locks value.</param>

    public void SetHardLocks(int locks) => labelHardLocks.Text = locks.ToString();

    /// <summary>
    /// Sets unsuccessful logins.
    /// </summary>
    /// <param name="logins">The logins value.</param>

    public void SetUnsuccessfulLogins(int logins) => labelUnsuccessfulLogins.Text = logins.ToString();


    /// <summary>
    /// Adds agent.
    /// </summary>
    /// <param name="agent">The agent value.</param>

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
    /// Handles the security agent configuration request event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void agentX_SecurityAgentConfigurationRequest(object? sender, EventArgs e) => SecurityAgentConfigurationRequest?.Invoke(sender, e);

    /// <summary>
    /// Clears agents.
    /// </summary>

    public void ClearAgents() => flowLayoutPanelPlugins.Controls.Clear();

}
