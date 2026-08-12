using System.Collections.Generic;
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
        Button discard = Assert.IsInstanceOfType<Button>(panel.Controls.Find("buttonDiscard", true)[0]);
        Assert.AreEqual(IddsConfig.DefaultHardLockAttempts.ToString(), hardLocks.Text);
        Assert.IsTrue(enabled.Checked);
        Assert.IsFalse(overwrite.Checked);
        Assert.IsTrue(save.Visible);
        Assert.IsTrue(discard.Visible);
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
        Assert.IsFalse(save.Visible);
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
}
