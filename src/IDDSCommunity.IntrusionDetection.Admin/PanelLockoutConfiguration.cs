using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供全域軟封鎖與硬封鎖門檻參數設定之面板控制項。
/// </summary>
public partial class PanelLockoutConfiguration : UserControl
{
    private readonly NumericUpDown numericSemanticDeduplicationSeconds = new();
    private readonly SmartLabel labelSemanticDeduplicationSeconds = new();
    private readonly CheckBox checkBoxEnableCrossAgentCorrelation = new();
    private readonly NumericUpDown numericSprayAccountThreshold = new();
    private readonly NumericUpDown numericSprayIpThreshold = new();
    private readonly NumericUpDown numericSlidingWindowMinutes = new();
    private readonly TextBox textBoxTrustedProxyCidrs = new();
    private readonly ToolTip trustedProxyToolTip = new();
    private readonly TableLayoutPanel advancedSettingsLayout = new();

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
        InitializeCorrelationControls();
        BackColor = Color.White;
        LoadData();
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click);
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X + 1, pictureBoxEdit.Location.Y + 1);
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X - 1, pictureBoxEdit.Location.Y - 1);

        /// <summary>
    /// 取得或設定 IsInEditMode。
    /// </summary>
public bool IsInEditMode { get; set; }
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
        SetEditMode(false);
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        //if (IsInEditMode) LoadData();
        //ToggleEditMode();
        //ClearErrors();
    }
    /// <summary>
    /// 執行 toggle edit mode 作業。
    /// </summary>
    private static void ToggleEditMode()
    {
        //if (!IsInEditMode) {
        //    pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_delete;
        //    IsInEditMode = true;
        //} else {
        //    pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_edit;
        //    IsInEditMode = false;
        //}
        //pictureBoxSave.Visible = IsInEditMode;
        //textBoxHardLockDuration.Enabled = IsInEditMode;
        //textBoxHardLocks.Enabled = IsInEditMode;
        //textBoxSoftLockDuration.Enabled = IsInEditMode;
        //textBoxSoftLocks.Enabled = IsInEditMode;
        //checkBoxLockForever.Enabled = IsInEditMode;
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
        if (!hasError)
        {
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
            ToggleEditMode();
            OnLockoutConfigurationChanged();
        }
        SetEditMode(false);
    }
    /// <summary>
    /// Processes the lockout configuration changed notification.
    /// </summary>
    private void OnLockoutConfigurationChanged() => LockoutConfigurationChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonDiscard_Click(object sender, EventArgs e)
    {
        LoadData();
        SetEditMode(false);
    }
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
        SetEditMode(true);
    }
    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void textBoxSoftLocks_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);

    private void InitializeCorrelationControls()
    {
        advancedSettingsLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        advancedSettingsLayout.ColumnCount = 2;
        advancedSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59F));
        advancedSettingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41F));
        advancedSettingsLayout.Location = new Point(24, 187);
        advancedSettingsLayout.Margin = Padding.Empty;
        advancedSettingsLayout.Name = "advancedSettingsLayout";
        advancedSettingsLayout.RowCount = 9;
        for (int row = 0; row < 8; row++)
            advancedSettingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        advancedSettingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        advancedSettingsLayout.Size = new Size(414, 312);

        checkBoxLockForever.Anchor = AnchorStyles.Left;
        checkBoxLockForever.Margin = Padding.Empty;

        labelSemanticDeduplicationSeconds.AutoEllipsis = true;
        labelSemanticDeduplicationSeconds.AutoSize = false;
        labelSemanticDeduplicationSeconds.Dock = DockStyle.Fill;
        labelSemanticDeduplicationSeconds.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        labelSemanticDeduplicationSeconds.ForeColor = Color.FromArgb(102, 102, 102);
        labelSemanticDeduplicationSeconds.Margin = new Padding(0, 0, 8, 0);
        labelSemanticDeduplicationSeconds.TextAlign = ContentAlignment.MiddleLeft;
        labelSemanticDeduplicationSeconds.Text = IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cross-agent duplicate tolerance (seconds)");

        numericSemanticDeduplicationSeconds.Anchor = AnchorStyles.Left;
        numericSemanticDeduplicationSeconds.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        numericSemanticDeduplicationSeconds.Margin = Padding.Empty;
        numericSemanticDeduplicationSeconds.Minimum = 1;
        numericSemanticDeduplicationSeconds.Maximum = 300;
        numericSemanticDeduplicationSeconds.Size = new Size(65, 23);
        numericSemanticDeduplicationSeconds.ValueChanged += (_, _) => SetEditMode(true);

        ConfigureCheckBox(checkBoxEnableCrossAgentCorrelation, "Enable cross-agent password-spray detection");
        checkBoxEnableCrossAgentCorrelation.CheckedChanged += (_, _) => SetEditMode(true);
        ConfigureNumeric(numericSprayAccountThreshold, 2, 100000);
        ConfigureNumeric(numericSprayIpThreshold, 2, 100000);
        ConfigureNumeric(numericSlidingWindowMinutes, 1, 1440);
        textBoxTrustedProxyCidrs.Dock = DockStyle.Fill;
        textBoxTrustedProxyCidrs.Margin = new Padding(0, 5, 0, 5);
        textBoxTrustedProxyCidrs.TextChanged += (_, _) => SetEditMode(true);
        trustedProxyToolTip.SetToolTip(
            textBoxTrustedProxyCidrs,
            Shared.Localization.Strings.Get("Used only to validate Forwarded/X-Forwarded-For and resolve the real client IP. This does not add addresses to the safe-network allowlist."));

        labelFirewallMode.AutoEllipsis = true;
        labelFirewallMode.AutoSize = false;
        labelFirewallMode.Dock = DockStyle.Fill;
        labelFirewallMode.Margin = new Padding(0, 0, 8, 0);
        labelFirewallMode.TextAlign = ContentAlignment.MiddleLeft;
        comboBoxFirewallMode.Dock = DockStyle.Fill;
        comboBoxFirewallMode.Margin = new Padding(0, 3, 0, 3);
        labelFirewallModeDescription.AutoEllipsis = true;
        labelFirewallModeDescription.Dock = DockStyle.Fill;
        labelFirewallModeDescription.Margin = Padding.Empty;
        labelFirewallModeDescription.TextAlign = ContentAlignment.TopLeft;

        Controls.Remove(checkBoxLockForever);
        Controls.Remove(labelFirewallMode);
        Controls.Remove(comboBoxFirewallMode);
        Controls.Remove(labelFirewallModeDescription);
        advancedSettingsLayout.Controls.Add(checkBoxLockForever, 0, 0);
        advancedSettingsLayout.SetColumnSpan(checkBoxLockForever, 2);
        advancedSettingsLayout.Controls.Add(checkBoxEnableCrossAgentCorrelation, 0, 1);
        advancedSettingsLayout.SetColumnSpan(checkBoxEnableCrossAgentCorrelation, 2);
        AddSettingRow(advancedSettingsLayout, 2, "Accounts per source IP threshold", numericSprayAccountThreshold);
        AddSettingRow(advancedSettingsLayout, 3, "Source IPs per account threshold", numericSprayIpThreshold);
        AddSettingRow(advancedSettingsLayout, 4, "Cross-agent sliding window (minutes)", numericSlidingWindowMinutes);
        advancedSettingsLayout.Controls.Add(labelSemanticDeduplicationSeconds, 0, 5);
        advancedSettingsLayout.Controls.Add(numericSemanticDeduplicationSeconds, 1, 5);
        AddSettingRow(advancedSettingsLayout, 6, "Trusted proxy IP/CIDR list", textBoxTrustedProxyCidrs);
        advancedSettingsLayout.Controls.Add(labelFirewallMode, 0, 7);
        advancedSettingsLayout.Controls.Add(comboBoxFirewallMode, 1, 7);
        advancedSettingsLayout.Controls.Add(labelFirewallModeDescription, 0, 8);
        advancedSettingsLayout.SetColumnSpan(labelFirewallModeDescription, 2);
        Controls.Add(advancedSettingsLayout);

        buttonSave.Location = new Point(112, 522);
        buttonDiscard.Location = new Point(220, 522);
        AutoScroll = true;
        Size = new Size(462, 562);
    }

    private static void ConfigureCheckBox(CheckBox checkBox, string text)
    {
        checkBox.Anchor = AnchorStyles.Left;
        checkBox.AutoSize = true;
        checkBox.Font = new Font("Segoe UI", 9F);
        checkBox.Margin = Padding.Empty;
        checkBox.Text = Shared.Localization.Strings.Get(text);
    }

    private void ConfigureNumeric(NumericUpDown numeric, int minimum, int maximum)
    {
        numeric.Anchor = AnchorStyles.Left;
        numeric.Font = new Font("Segoe UI", 9F);
        numeric.Minimum = minimum;
        numeric.Maximum = maximum;
        numeric.Size = new Size(80, 23);
        numeric.ValueChanged += (_, _) => SetEditMode(true);
    }

    private static void AddSettingRow(TableLayoutPanel layout, int row, string labelText, Control editor)
    {
        SmartLabel label = new()
        {
            AutoEllipsis = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(102, 102, 102),
            Margin = new Padding(0, 0, 8, 0),
            Text = Shared.Localization.Strings.Get(labelText),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(editor, 1, row);
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
    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>
    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }
    /// <summary>
    /// 處理 checked changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void checkBoxLockForever_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);

    private void comboBoxFirewallMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        labelFirewallModeDescription.Text = comboBoxFirewallMode.SelectedIndex == 1
            ? Shared.Localization.Strings.Get("Blocks inbound traffic and outbound replies for the selected remote addresses.")
            : Shared.Localization.Strings.Get("Blocks inbound traffic from the selected remote addresses. Recommended for most servers.");
        SetEditMode(true);
    }
}
