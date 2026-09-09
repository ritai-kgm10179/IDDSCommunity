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
public sealed partial class PanelDeceptionAndApiSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    /// <summary>
    /// 當欺敵、SOAR 或 API 設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? DeceptionAndApiSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelDeceptionAndApiSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelDeceptionAndApiSettings()
    {
        InitializeComponent();

        btnBrowseScript.Click += (s, e) => BrowseScript();
        btnGenApiKey.Click += (s, e) => GenerateNewApiKey();
        btnSave.Click += (s, e) => SaveSettings();

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
