using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供全域軟封鎖與硬封鎖門檻參數設定之面板控制項。
/// </summary>
public partial class PanelLockoutConfiguration : UserControl
{
    private long editGeneration;
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

        checkBoxLockForever.CheckedChanged += (_, _) => UpdateLockoutControlsState();
        checkBoxEnableCrossAgentCorrelation.CheckedChanged += (_, _) => UpdateLockoutControlsState();
        LoadData();
        foreach (Control control in tableLayoutMain.Controls)
        {
            if (control is TextBoxBase text) text.TextChanged += (_, _) => editGeneration++;
            if (control is CheckBox check) check.CheckedChanged += (_, _) => editGeneration++;
            if (control is ComboBox combo) combo.SelectedIndexChanged += (_, _) => editGeneration++;
            if (control is NumericUpDown numeric) numeric.ValueChanged += (_, _) => editGeneration++;
        }
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
        UpdateLockoutControlsState();
    }

    private void UpdateLockoutControlsState()
    {
        textBoxHardLockDuration.Enabled = !checkBoxLockForever.Checked;
        bool correlationEnabled = checkBoxEnableCrossAgentCorrelation.Checked;
        numericSprayAccountThreshold.Enabled = correlationEnabled;
        numericSprayIpThreshold.Enabled = correlationEnabled;
        numericSlidingWindowMinutes.Enabled = correlationEnabled;
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
    private async void pictureBoxSave_Click(object sender, EventArgs e)
    {
        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxHardLocks.Text, out int hardLocks))
        {
            errHardLocks.Visible = true;
            hasError = true;
        }
        int hardLockDuration = 0;
        if (!checkBoxLockForever.Checked)
        {
            if (!int.TryParse(textBoxHardLockDuration.Text, out hardLockDuration))
            {
                errHardLockDuration.Visible = true;
                hasError = true;
            }
        }
        else
        {
            _ = int.TryParse(textBoxHardLockDuration.Text, out hardLockDuration);
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
        long generation = editGeneration;
        LockoutSnapshot snapshot = new(checkBoxLockForever.Checked, hardLocks, hardLockDuration, softLocks, softLockDuration,
            comboBoxFirewallMode.SelectedIndex == 1 ? FirewallBlockMode.Bidirectional : FirewallBlockMode.Inbound,
            decimal.ToInt32(numericSemanticDeduplicationSeconds.Value), checkBoxEnableCrossAgentCorrelation.Checked,
            decimal.ToInt32(numericSprayAccountThreshold.Value), decimal.ToInt32(numericSprayIpThreshold.Value),
            decimal.ToInt32(numericSlidingWindowMinutes.Value), normalizedTrustedProxies);
        buttonSave.Enabled = false;
        try
        {
            await Task.Run(() => Persist(snapshot));
            if (generation == editGeneration)
                MessageBox.Show(Shared.Localization.Strings.Get("Configuration was saved successfully."), Shared.Localization.Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            OnLockoutConfigurationChanged();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, Shared.Localization.Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { if (!IsDisposed) buttonSave.Enabled = true; }
    }

    private static void Persist(LockoutSnapshot value)
    {
        IddsConfig config = IddsConfig.Instance;
        config.LockForever = value.LockForever;
        config.HardLockAttempts = value.HardLocks;
        config.HardLockTimeHours = value.HardLockDuration;
        config.SoftLockAttempts = value.SoftLocks;
        config.SoftLockTimeMinutes = value.SoftLockDuration;
        config.FirewallBlockMode = value.FirewallMode;
        config.CrossAgentSemanticDeduplicationSeconds = value.SemanticDeduplicationSeconds;
        config.EnableCrossAgentCorrelation = value.EnableCorrelation;
        config.CrossAgentSprayAccountThreshold = value.SprayAccountThreshold;
        config.CrossAgentSprayIpThreshold = value.SprayIpThreshold;
        config.CrossAgentSlidingWindowMinutes = value.SlidingWindowMinutes;
        config.TrustedProxyCidrs = value.TrustedProxyCidrs;
        config.Save();
        config.SaveAppConfig();
    }

    private sealed record LockoutSnapshot(bool LockForever, int HardLocks, int HardLockDuration, int SoftLocks,
        int SoftLockDuration, FirewallBlockMode FirewallMode, int SemanticDeduplicationSeconds, bool EnableCorrelation,
        int SprayAccountThreshold, int SprayIpThreshold, int SlidingWindowMinutes, string TrustedProxyCidrs);
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
        UpdateLockoutControlsState();
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
