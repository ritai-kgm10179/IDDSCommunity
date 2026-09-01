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
public sealed class PanelSelfServiceSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    private readonly CheckBox chkEnablePortal;
    private readonly NumericUpDown numPort;
    private readonly TextBox txtListenIp;
    private readonly TextBox txtTotpSecret;
    private readonly TextBox txtTestCode;
    private readonly Label lblVerificationResult;

    /// <summary>
    /// 當自助門戶設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? SelfServiceSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelSelfServiceSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelSelfServiceSettings()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        AutoScroll = true;

        Font defaultFont = new("Segoe UI", 9F);
        Font sectionHeaderFont = new("Segoe UI", 10F, FontStyle.Bold);

        int leftMargin = 15;
        int controlWidth = 380;
        int y = 15;

        // Title
        Label lblTitle = new()
        {
            Text = Strings.Get("Self-service unblock portal"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        Controls.Add(lblTitle);
        y += 30;

        chkEnablePortal = new CheckBox
        {
            Text = Strings.Get("Enable self-service unblock portal"),
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(chkEnablePortal);
        y += 32;

        // Port
        Label lblPort = new()
        {
            Text = Strings.Get("Portal listening port"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblPort);
        y += 18;

        numPort = new NumericUpDown
        {
            Location = new Point(leftMargin, y),
            Size = new Size(180, 24),
            Minimum = 1,
            Maximum = 65535,
            Value = 8444,
            Font = defaultFont
        };
        Controls.Add(numPort);
        y += 32;

        // Listen IP
        Label lblListenIp = new()
        {
            Text = Strings.Get("Portal listening IP address"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblListenIp);
        y += 18;

        txtListenIp = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Text = "0.0.0.0",
            Font = defaultFont
        };
        Controls.Add(txtListenIp);
        y += 32;

        // TOTP Secret
        Label lblSecret = new()
        {
            Text = Strings.Get("RFC 6238 TOTP Secret Key (Base32)"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblSecret);
        y += 18;

        txtTotpSecret = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(250, 24),
            Font = defaultFont
        };
        Controls.Add(txtTotpSecret);

        Button btnGenerate = new()
        {
            Text = Strings.Get("Generate Secret"),
            Location = new Point(leftMargin + 260, y - 2),
            Size = new Size(120, 28),
            Font = defaultFont
        };
        btnGenerate.Click += (s, e) => GenerateNewSecret();
        Controls.Add(btnGenerate);
        y += 32;

        // Test TOTP code
        Label lblTest = new()
        {
            Text = Strings.Get("Test Verification Code"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblTest);
        y += 18;

        txtTestCode = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(140, 24),
            MaxLength = 6,
            Font = defaultFont
        };
        Controls.Add(txtTestCode);

        Button btnVerify = new()
        {
            Text = Strings.Get("Verify Code"),
            Location = new Point(leftMargin + 150, y - 2),
            Size = new Size(100, 28),
            Font = defaultFont
        };
        btnVerify.Click += (s, e) => VerifyCode();
        Controls.Add(btnVerify);

        lblVerificationResult = new Label
        {
            Location = new Point(leftMargin, y + 28),
            Size = new Size(controlWidth, 24),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Text = string.Empty
        };
        Controls.Add(lblVerificationResult);
        y += 56;

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
