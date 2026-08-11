using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證全域與 Agent 設定使用一致且安全的原廠預設值。
/// </summary>
[TestClass]
public sealed class SettingsDefaultsTest
{
    /// <summary>
    /// 驗證全域預設值包含安全的封鎖、SMTP 與通知初始狀態。
    /// </summary>
    [TestMethod]
    public void GlobalDefaults_AreCompleteAndSafe()
    {
        IddsConfig defaults = IddsConfig.GetDefaultConfiguration();

        Assert.AreEqual(IddsConfig.DefaultSoftLockAttempts, defaults.SoftLockAttempts);
        Assert.AreEqual(IddsConfig.DefaultSoftLockMinutes, defaults.SoftLockTimeMinutes);
        Assert.AreEqual(IddsConfig.DefaultHardLockAttempts, defaults.HardLockAttempts);
        Assert.AreEqual(IddsConfig.DefaultHardLockHours, defaults.HardLockTimeHours);
        Assert.AreEqual(IddsConfig.DefaultSmtpPort, defaults.SmtpPort);
        Assert.IsFalse(defaults.LockForever);
        Assert.IsFalse(defaults.UseSafeNetworkList);
        Assert.IsFalse(defaults.SendInfoMail);
        Assert.IsFalse(defaults.SmtpSslRequired);
        Assert.IsFalse(defaults.SmtpRequiresAuthentication);
        Assert.IsEmpty(defaults.SafeNetworks);
    }

    /// <summary>
    /// 驗證 Agent 重設會還原共用與自訂設定，但不會改變啟用狀態或預設值快照。
    /// </summary>
    [TestMethod]
    public void AgentReset_RestoresDefaultsAndPreservesEnabledState()
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
            CustomConfiguration = new Dictionary<string, string> { ["Port"] = "2525" }
        };

        agent.ResetConfigurationToDefaults();

        Assert.IsTrue(agent.Enabled);
        Assert.IsFalse(agent.OverrideConfig);
        Assert.IsFalse(agent.LockForever);
        Assert.AreEqual(IddsConfig.DefaultHardLockAttempts, agent.HardLockAttempts);
        Assert.AreEqual(IddsConfig.DefaultHardLockHours, agent.HardLockTimeHours);
        Assert.AreEqual(IddsConfig.DefaultSoftLockAttempts, agent.SoftLockAttempts);
        Assert.AreEqual(IddsConfig.DefaultSoftLockMinutes, agent.SoftLockTimeMinutes);
        Assert.AreEqual("25", agent.CustomConfiguration["Port"]);
        agent.CustomConfiguration["Port"] = "110";
        Assert.AreEqual("25", agent.DefaultCustomConfiguration["Port"]);
    }
}
