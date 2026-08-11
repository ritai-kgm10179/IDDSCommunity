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
        using PanelPluginConfiguration panel = new() { Agent = agent };

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
}
