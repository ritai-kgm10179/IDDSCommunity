using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 Admin 設定頁的恢復預設值互動不會略過儲存與取消流程。
/// </summary>
[TestClass]
public sealed class SettingsResetUiTest
{
    /// <summary>
    /// 驗證 Agent 的恢復預設值操作只更新待儲存畫面，不會立即改寫執行中設定。
    /// </summary>
    [STATestMethod]
    public void AgentResetButton_StagesDefaultsWithoutImmediatePersistence()
    {
        SecurityAgent agent = new()
        {
            Enabled = true,
            OverrideConfig = true,
            LockForever = true,
            HardLockAttempts = 99,
            HardLockTimeHours = 48,
            SoftLockAttempts = 88,
            SoftLockTimeMinutes = 44,
            DefaultCustomConfiguration = new Dictionary<string, string> { ["Port"] = "25" },
            CustomConfiguration = new Dictionary<string, string> { ["Port"] = "2525" },
            CustomConfigurationTypes = new Dictionary<string, string> { ["Port"] = typeof(int).FullName! }
        };
        using PanelPluginConfiguration panel = new(_ => DialogResult.Yes) { Agent = agent };

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        reset.PerformClick();

        Assert.AreEqual(99, agent.HardLockAttempts);
        Assert.IsTrue(agent.OverrideConfig);
        Assert.IsTrue(agent.Enabled);
        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLocks", true)[0]);
        CheckBox enabled = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxEnableSecurityAgent", true)[0]);
        CheckBox overwrite = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("checkBoxOverrideConfiguration", true)[0]);
        Button save = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonSave", true)[0]);
        Assert.AreEqual(IddsConfig.DefaultHardLockAttempts.ToString(), hardLocks.Text);
        Assert.IsTrue(enabled.Checked);
        Assert.IsFalse(overwrite.Checked);
        Assert.IsTrue(save.Visible);
        Assert.HasCount(0, panel.Controls.Find("buttonDiscard", true));
    }

    /// <summary>
    /// 驗證使用者取消確認提示時不會變更待編輯設定。
    /// </summary>
    [STATestMethod]
    public void AgentResetButton_CancelledConfirmationLeavesSettingsUnchanged()
    {
        SecurityAgent agent = new()
        {
            HardLockAttempts = 99,
            DefaultCustomConfiguration = new Dictionary<string, string> { ["Port"] = "25" },
            CustomConfiguration = new Dictionary<string, string> { ["Port"] = "2525" },
            CustomConfigurationTypes = new Dictionary<string, string> { ["Port"] = typeof(int).FullName! }
        };
        using PanelPluginConfiguration panel = new(_ => DialogResult.No) { Agent = agent };

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        reset.PerformClick();

        TextBox hardLocks = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxHardLocks", true)[0]);
        Button save = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonSave", true)[0]);
        Assert.AreEqual("99", hardLocks.Text);
        Assert.IsTrue(save.Visible);
    }

    /// <summary>
    /// 驗證 Agent 啟用狀態變更後，Dashboard 可立即替換狀態圖示而不必等待定時更新。
    /// </summary>
    [STATestMethod]
    public void DashboardRefreshAgentPresentations_ImmediatelyUpdatesEnabledStateIcon()
    {
        SecurityAgent agent = new()
        {
            DisplayName = "Test Agent",
            Enabled = false
        };
        using IDDSCommunityDashboard dashboard = new();
        dashboard.AddAgent(agent);
        PictureBox status = Assert.IsInstanceOfType<PictureBox>(dashboard.Controls.Find("pictureBoxEnabledState", true)[0]);
        Assert.IsNotNull(status.Image);
        Image disabledImage = status.Image;
        string disabledAccessibleName = status.AccessibleName ?? string.Empty;

        agent.Enabled = true;
        dashboard.RefreshAgentPresentations();

        Assert.IsNotNull(status.Image);
        Image enabledImage = status.Image;
        Assert.AreNotSame(disabledImage, enabledImage);
        Assert.AreNotEqual(disabledAccessibleName, status.AccessibleName);
    }

    /// <summary>
    /// 驗證共用按鈕在較窄的可視容器內仍保持完整可見。
    /// </summary>
    [STATestMethod]
    public void ResetButton_RemainsInsideVisibleParentBounds()
    {
        using Panel viewport = new() { ClientSize = new System.Drawing.Size(500, 500) };
        using PanelNotificationSettings settings = new() { Size = new System.Drawing.Size(620, 400) };
        viewport.Controls.Add(settings);
        viewport.PerformLayout();
        settings.PerformLayout();

        Button reset = Assert.IsInstanceOfType<Button>(settings.Controls.Find("buttonResetDefaults", true)[0]);
        Assert.IsTrue(reset.Left >= 0);
        Assert.IsTrue(reset.Right <= viewport.ClientSize.Width);

        viewport.ClientSize = new System.Drawing.Size(420, 500);
        viewport.PerformLayout();
        settings.PerformLayout();
        Assert.IsTrue(reset.Right <= viewport.ClientSize.Width);
    }

    /// <summary>
    /// 驗證長代理程式名稱於中英文介面下自動折行，且標題區域絕不與恢復預設值按鈕重疊。
    /// </summary>
    /// <param name="agentDisplayName">待測試之代理程式顯示名稱。</param>
    [STATestMethod]
    [DataRow("Windows 遠端管理 ( WinRM / WAC ) 安全性代理程式")]
    [DataRow("Windows Remote Management (WinRM / WAC) Security Agent")]
    public void PanelPluginConfiguration_LongAgentTitle_WrapsAndDoesNotOverlapWithResetButton(string agentDisplayName)
    {
        SecurityAgent agent = new()
        {
            DisplayName = agentDisplayName
        };
        using Panel viewport = new() { ClientSize = new System.Drawing.Size(480, 500) };
        using PanelPluginConfiguration panel = new(_ => DialogResult.No)
        {
            Size = new System.Drawing.Size(480, 500),
            Agent = agent
        };
        viewport.Controls.Add(panel);
        viewport.PerformLayout();
        panel.PerformLayout();

        Button reset = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonResetDefaults", true)[0]);
        SmartLabel title = Assert.IsInstanceOfType<SmartLabel>(panel.Controls.Find("smartLabelAgentName", true)[0]);

        Assert.IsFalse(title.Bounds.IntersectsWith(reset.Bounds),
            $"標題邊界 ({title.Bounds}) 不得與恢復預設值按鈕邊界 ({reset.Bounds}) 重疊。");
        Assert.IsTrue(title.Right <= reset.Left,
            $"標題右邊緣 ({title.Right}) 必須位於恢復預設值按鈕左邊緣 ({reset.Left}) 之前。");
    }
}
