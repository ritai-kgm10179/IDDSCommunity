using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供動態欺敵誘餌帳號、SOAR 處置腳本與 RESTful Management API 視覺化設定面板。
/// </summary>
public sealed class PanelDeceptionAndApiSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    // Section 1: Honey Accounts
    private readonly CheckBox chkEnableHoneyAccounts;
    private readonly TextBox txtHoneyAccounts;

    // Section 2: SOAR
    private readonly TextBox txtSoarScriptPath;
    private readonly Button btnBrowseScript;

    // Section 3: Management API
    private readonly CheckBox chkEnableManagementApi;
    private readonly NumericUpDown numApiPort;
    private readonly TextBox txtApiKey;

    /// <summary>
    /// 當欺敵、SOAR 或 API 設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? DeceptionAndApiSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelDeceptionAndApiSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelDeceptionAndApiSettings()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        AutoScroll = true;

        Font defaultFont = new("Segoe UI", 9F);
        Font sectionHeaderFont = new("Segoe UI", 10F, FontStyle.Bold);

        int leftMargin = 15;
        int controlWidth = 380;
        int y = 15;

        // Section 1: Honey Accounts
        Label lblHoneyTitle = new()
        {
            Text = Strings.Get("Deception & Honey-Accounts"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        Controls.Add(lblHoneyTitle);
        y += 30;

        chkEnableHoneyAccounts = new CheckBox
        {
            Text = Strings.Get("Enable honey-account one-strike defense"),
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(chkEnableHoneyAccounts);
        y += 28;

        Label lblHoneyAccounts = new()
        {
            Text = Strings.Get("Honey-accounts list"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblHoneyAccounts);
        y += 18;

        txtHoneyAccounts = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = defaultFont
        };
        Controls.Add(txtHoneyAccounts);
        y += 72;

        // Section 2: SOAR
        Label lblSoarTitle = new()
        {
            Text = Strings.Get("SOAR remediation automation script"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        Controls.Add(lblSoarTitle);
        y += 30;

        Label lblScript = new()
        {
            Text = Strings.Get("SOAR script path"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblScript);
        y += 18;

        txtSoarScriptPath = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(275, 24),
            Font = defaultFont
        };
        Controls.Add(txtSoarScriptPath);

        btnBrowseScript = new Button
        {
            Text = Strings.Get("Browse..."),
            Location = new Point(leftMargin + 285, y - 2),
            Size = new Size(95, 28),
            Font = defaultFont
        };
        btnBrowseScript.Click += (s, e) => BrowseScript();
        Controls.Add(btnBrowseScript);
        y += 42;

        // Section 3: RESTful Management API
        Label lblApiTitle = new()
        {
            Text = Strings.Get("RESTful Management API"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        Controls.Add(lblApiTitle);
        y += 30;

        chkEnableManagementApi = new CheckBox
        {
            Text = Strings.Get("Enable RESTful Management API"),
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(chkEnableManagementApi);
        y += 28;

        Label lblApiPort = new()
        {
            Text = Strings.Get("Management API port"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblApiPort);
        y += 18;

        numApiPort = new NumericUpDown
        {
            Location = new Point(leftMargin, y),
            Size = new Size(180, 24),
            Minimum = 1,
            Maximum = 65535,
            Value = 8443,
            Font = defaultFont
        };
        Controls.Add(numApiPort);
        y += 32;

        Label lblApiKey = new()
        {
            Text = Strings.Get("Management API key (X-Api-Key)"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblApiKey);
        y += 18;

        txtApiKey = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(250, 24),
            Font = defaultFont
        };
        Controls.Add(txtApiKey);

        Button btnGenApiKey = new()
        {
            Text = Strings.Get("Generate API Key"),
            Location = new Point(leftMargin + 260, y - 2),
            Size = new Size(120, 28),
            Font = defaultFont
        };
        btnGenApiKey.Click += (s, e) => GenerateNewApiKey();
        Controls.Add(btnGenApiKey);
        y += 48;

        // Save Button
        Button btnSave = new()
        {
            Text = Strings.Get("&Save"),
            Location = new Point(leftMargin, y),
            Size = new Size(120, 32),
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnSave.Click += (s, e) => SaveSettings();
        Controls.Add(btnSave);

        LoadSettings();
    }

    /// <summary>
    /// 載入目前的組態設定值。
    /// </summary>
    public void LoadSettings()
    {
        IddsConfig config = IddsConfig.Instance;
        chkEnableHoneyAccounts.Checked = config.EnableHoneyAccounts;
        txtHoneyAccounts.Text = string.Join(Environment.NewLine, config.HoneyAccounts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        txtSoarScriptPath.Text = config.SoarRemediationScriptPath;
        chkEnableManagementApi.Checked = config.EnableManagementApi;
        numApiPort.Value = config.ManagementApiPort is >= 1 and <= 65535 ? config.ManagementApiPort : 8443;
        txtApiKey.Text = config.ManagementApiKey;
    }

    /// <summary>
    /// 儲存當前面板設定。
    /// </summary>
    public void SaveSettings()
    {
        IddsConfig config = IddsConfig.Instance;
        config.EnableHoneyAccounts = chkEnableHoneyAccounts.Checked;
        config.HoneyAccounts = string.Join(",", txtHoneyAccounts.Text.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        config.SoarRemediationScriptPath = txtSoarScriptPath.Text.Trim();
        config.EnableManagementApi = chkEnableManagementApi.Checked;
        config.ManagementApiPort = (int)numApiPort.Value;
        config.ManagementApiKey = txtApiKey.Text.Trim();
        config.SaveAppConfig();

        MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        DeceptionAndApiSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BrowseScript()
    {
        using OpenFileDialog dlg = new()
        {
            Filter = "Script files (*.ps1;*.bat;*.cmd)|*.ps1;*.bat;*.cmd|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            txtSoarScriptPath.Text = dlg.FileName;
        }
    }

    private void GenerateNewApiKey()
    {
        byte[] random = RandomNumberGenerator.GetBytes(24);
        txtApiKey.Text = Convert.ToBase64String(random);
    }
}
