using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.SelfService;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供合法使用者自助解除誤封鎖門戶（Self-Service Unblock Portal 與 RFC 6238 TOTP 認證）視覺化設定面板。
/// </summary>
public sealed partial class PanelSelfServiceSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(15, 118, 110);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);
    private const string AnyIpv4Address = "0.0.0.0";

    /// <summary>
    /// 當自助門戶設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? SelfServiceSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelSelfServiceSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelSelfServiceSettings()
    {
        InitializeComponent();

        txtListenIp.Text = AnyIpv4Address;

        btnGenerate.Click += (s, e) => GenerateNewSecret();
        btnVerify.Click += (s, e) => VerifyCode();
        btnSave.Click += (s, e) => SaveSettings();

        LoadSettings();
    }

    /// <summary>
    /// 載入目前的組態設定值。
    /// </summary>
    public void LoadSettings()
    {
        IddsConfig config = IddsConfig.Instance;
        chkEnablePortal.Checked = config.EnableSelfServicePortal;
        numPort.Value = config.SelfServicePortalPort is >= 1 and <= 65535 ? config.SelfServicePortalPort : 8444;
        txtListenIp.Text = string.IsNullOrWhiteSpace(config.SelfServicePortalListenIp) ? "0.0.0.0" : config.SelfServicePortalListenIp;
        txtTotpSecret.Text = config.SelfServiceTotpSecret;
    }

    /// <summary>
    /// 儲存當前面板設定。
    /// </summary>
    public void SaveSettings()
    {
        IddsConfig config = IddsConfig.Instance;
        config.EnableSelfServicePortal = chkEnablePortal.Checked;
        config.SelfServicePortalPort = (int)numPort.Value;
        config.SelfServicePortalListenIp = txtListenIp.Text.Trim();
        config.SelfServiceTotpSecret = txtTotpSecret.Text.Trim();
        config.SaveAppConfig();

        MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        SelfServiceSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GenerateNewSecret()
    {
        txtTotpSecret.Text = TotpAuthenticator.GenerateSecretKey();
    }

    private void VerifyCode()
    {
        string secret = txtTotpSecret.Text.Trim();
        string code = txtTestCode.Text.Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            lblVerificationResult.ForeColor = Color.Red;
            lblVerificationResult.Text = string.Empty;
            return;
        }

        if (TotpAuthenticator.VerifyCode(secret, code))
        {
            lblVerificationResult.ForeColor = Color.Green;
            lblVerificationResult.Text = string.Empty;
        }
        else
        {
            lblVerificationResult.ForeColor = Color.Red;
            lblVerificationResult.Text = string.Empty;
        }
    }
}
