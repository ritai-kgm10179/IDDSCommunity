using System;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供全域軟封鎖與硬封鎖門檻參數設定之面板控制項。
/// </summary>
public partial class PanelLockoutConfiguration : UserControl
{
    /// <summary>
    /// 當 LockoutConfigurationChanged 時引發之事件。
    /// </summary>
    public event EventHandler? LockoutConfigurationChanged;

    /// <summary>
    /// 初始化 <see cref="PanelLockoutConfiguration"/> 類別的新執行個體。
    /// </summary>
    public PanelLockoutConfiguration()
    {
        InitializeComponent();

        trustedProxyToolTip.SetToolTip(
            textBoxTrustedProxyCidrs,
            Shared.Localization.Strings.Get("Used only to validate Forwarded/X-Forwarded-For and resolve the real client IP. This does not add addresses to the safe-network allowlist."));

        LoadData();
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click, container: headerPanel);
    }

    /// <summary>
    /// Loads data.
    /// </summary>
    private void LoadData()
    {
        textBoxHardLocks.Text = IddsConfig.Instance.HardLockAttempts.ToString();
        textBoxHardLockDuration.Text = IddsConfig.Instance.HardLockTimeHours.ToString();
        textBoxSoftLockDuration.Text = IddsConfig.Instance.SoftLockTimeMinutes.ToString();
        textBoxSoftLocks.Text = IddsConfig.Instance.SoftLockAttempts.ToString();
        checkBoxLockForever.Checked = IddsConfig.Instance.LockForever;
        comboBoxFirewallMode.SelectedIndex = IddsConfig.Instance.FirewallBlockMode == FirewallBlockMode.Bidirectional ? 1 : 0;
        numericSemanticDeduplicationSeconds.Value = IddsConfig.Instance.CrossAgentSemanticDeduplicationSeconds;
        checkBoxEnableCrossAgentCorrelation.Checked = IddsConfig.Instance.EnableCrossAgentCorrelation;
        numericSprayAccountThreshold.Value = IddsConfig.Instance.CrossAgentSprayAccountThreshold;
        numericSprayIpThreshold.Value = IddsConfig.Instance.CrossAgentSprayIpThreshold;
        numericSlidingWindowMinutes.Value = IddsConfig.Instance.CrossAgentSlidingWindowMinutes;
        textBoxTrustedProxyCidrs.Text = IddsConfig.Instance.TrustedProxyCidrs;
    }
    /// <summary>
    /// Clears errors.
    /// </summary>
    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxSave_Click(object sender, EventArgs e)
    {
        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxHardLocks.Text, out int hardLocks))
        {
            errHardLocks.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxHardLockDuration.Text, out int hardLockDuration))
        {
            errHardLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLockDuration.Text, out int softLockDuration))
        {
            errSoftLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLocks.Text, out int softLocks))
        {
            errSoftLocks.Visible = true;
            hasError = true;
        }
        if (hasError) return;

        string normalizedTrustedProxies;
        try
        {
            normalizedTrustedProxies = NormalizeTrustedProxyEntries(textBoxTrustedProxyCidrs.Text);
        }
        catch (FormatException exception)
        {
            MessageBox.Show(this, exception.Message, Shared.Localization.Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        IddsConfig.Instance.LockForever = checkBoxLockForever.Checked;
        IddsConfig.Instance.HardLockAttempts = hardLocks;
        IddsConfig.Instance.HardLockTimeHours = hardLockDuration;
        IddsConfig.Instance.SoftLockAttempts = softLocks;
        IddsConfig.Instance.SoftLockTimeMinutes = softLockDuration;
        IddsConfig.Instance.Save();
        IddsConfig.Instance.FirewallBlockMode = comboBoxFirewallMode.SelectedIndex == 1
            ? FirewallBlockMode.Bidirectional
            : FirewallBlockMode.Inbound;
        IddsConfig.Instance.CrossAgentSemanticDeduplicationSeconds = decimal.ToInt32(numericSemanticDeduplicationSeconds.Value);
        IddsConfig.Instance.EnableCrossAgentCorrelation = checkBoxEnableCrossAgentCorrelation.Checked;
        IddsConfig.Instance.CrossAgentSprayAccountThreshold = decimal.ToInt32(numericSprayAccountThreshold.Value);
        IddsConfig.Instance.CrossAgentSprayIpThreshold = decimal.ToInt32(numericSprayIpThreshold.Value);
        IddsConfig.Instance.CrossAgentSlidingWindowMinutes = decimal.ToInt32(numericSlidingWindowMinutes.Value);
        IddsConfig.Instance.TrustedProxyCidrs = normalizedTrustedProxies;
        IddsConfig.Instance.SaveAppConfig();
        OnLockoutConfigurationChanged();
        MessageBox.Show(Shared.Localization.Strings.Get("Configuration was saved successfully."), Shared.Localization.Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    /// <summary>
    /// Processes the lockout configuration changed notification.
    /// </summary>
    private void OnLockoutConfigurationChanged() => LockoutConfigurationChanged?.Invoke(this, EventArgs.Empty);

    private void ResetDefaults_Click(object? sender, EventArgs e)
    {
        textBoxHardLocks.Text = IddsConfig.DefaultHardLockAttempts.ToString();
        textBoxHardLockDuration.Text = IddsConfig.DefaultHardLockHours.ToString();
        textBoxSoftLocks.Text = IddsConfig.DefaultSoftLockAttempts.ToString();
        textBoxSoftLockDuration.Text = IddsConfig.DefaultSoftLockMinutes.ToString();
        checkBoxLockForever.Checked = false;
        comboBoxFirewallMode.SelectedIndex = 0;
        numericSemanticDeduplicationSeconds.Value = 15;
        checkBoxEnableCrossAgentCorrelation.Checked = false;
        numericSprayAccountThreshold.Value = 5;
        numericSprayIpThreshold.Value = 5;
        numericSlidingWindowMinutes.Value = 10;
        textBoxTrustedProxyCidrs.Clear();
    }

    private static string NormalizeTrustedProxyEntries(string value)
    {
        if (value.Length > 250)
            throw new FormatException(Shared.Localization.Strings.Get("Trusted proxy list must not exceed 250 characters."));
        string[] entries = value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string entry in entries)
        {
            string[] parts = entry.Split('/');
            if (parts.Length is < 1 or > 2 || !System.Net.IPAddress.TryParse(parts[0], out System.Net.IPAddress? address))
                throw new FormatException(Shared.Localization.Strings.Get("Trusted proxy entries must be IP addresses or CIDR ranges."));
            if (parts.Length == 2 && (!int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > address.GetAddressBytes().Length * 8))
                throw new FormatException(Shared.Localization.Strings.Get("Trusted proxy CIDR prefix is invalid."));
        }
        return string.Join(';', entries);
    }

    private void comboBoxFirewallMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        labelFirewallModeDescription.Text = comboBoxFirewallMode.SelectedIndex == 1
            ? Shared.Localization.Strings.Get("Blocks inbound traffic and outbound replies for the selected remote addresses.")
            : Shared.Localization.Strings.Get("Blocks inbound traffic from the selected remote addresses. Recommended for most servers.");
    }
}
