using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供多雲邊界安全聯防（AWS WAFv2、Azure NSG、GCP Cloud Armor、Cloudflare、中華電信 HiCloud）視覺化配置面板。
/// </summary>
public sealed class PanelCloudPerimeterSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    private readonly CheckBox chkEnableCloudPerimeter;
    private readonly ComboBox comboProviderType;
    private readonly TextBox txtApiKey;
    private readonly TextBox txtEndpointUrl;
    private readonly TextBox txtResourceId;
    private readonly TextBox txtSecondaryId;
    private readonly TextBox txtTertiaryId;
    private readonly Button btnTestConnection;
    private readonly Label lblStatus;

    /// <summary>
    /// 當雲端邊界安全設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? CloudPerimeterSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelCloudPerimeterSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelCloudPerimeterSettings()
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
            Text = Strings.Get("Cloud perimeter defense"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        Controls.Add(lblTitle);
        y += 30;

        chkEnableCloudPerimeter = new CheckBox
        {
            Text = Strings.Get("Enable cloud perimeter synchronization"),
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(chkEnableCloudPerimeter);
        y += 32;

        // Provider ComboBox
        Label lblProvider = new()
        {
            Text = Strings.Get("Cloud service provider"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblProvider);
        y += 18;

        comboProviderType = new ComboBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont
        };
        comboProviderType.Items.AddRange([
            "None (0)",
            "AWS WAFv2 (1)",
            "Azure NSG (2)",
            "GCP Cloud Armor (3)",
            "Cloudflare WAF (4)",
            "Chunghwa HiCloud (5)",
            "Generic Webhook (6)"
        ]);
        comboProviderType.SelectedIndex = 0;
        Controls.Add(comboProviderType);
        y += 32;

        // API Key
        Label lblApiKey = new()
        {
            Text = Strings.Get("API Key / Access Token"),
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
            Size = new Size(controlWidth, 24),
            UseSystemPasswordChar = true,
            Font = defaultFont
        };
        Controls.Add(txtApiKey);
        y += 32;

        // Endpoint
        Label lblEndpoint = new()
        {
            Text = Strings.Get("Endpoint URL / Region Endpoint"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblEndpoint);
        y += 18;

        txtEndpointUrl = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(txtEndpointUrl);
        y += 32;

        // Resource ID
        Label lblResource = new()
        {
            Text = Strings.Get("Primary Resource ID (IPSet ID / Zone ID)"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblResource);
        y += 18;

        txtResourceId = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(txtResourceId);
        y += 32;

        // Secondary ID
        Label lblSecondary = new()
        {
            Text = Strings.Get("Secondary Resource ID (Scope / Policy Name / Project ID)"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblSecondary);
        y += 18;

        txtSecondaryId = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(txtSecondaryId);
        y += 32;

        // Tertiary ID
        Label lblTertiary = new()
        {
            Text = Strings.Get("Tertiary Resource ID (Network ID / HiCloud CVPC ID)"),
            Location = new Point(leftMargin, y),
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor
        };
        Controls.Add(lblTertiary);
        y += 18;

        txtTertiaryId = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(controlWidth, 24),
            Font = defaultFont
        };
        Controls.Add(txtTertiaryId);
        y += 38;

        // Test button
        btnTestConnection = new Button
        {
            Text = Strings.Get("Test Connection"),
            Location = new Point(leftMargin, y),
            Size = new Size(160, 30),
            Font = defaultFont
        };
        btnTestConnection.Click += async (s, e) => await TestConnectionAsync().ConfigureAwait(true);
        Controls.Add(btnTestConnection);

        lblStatus = new Label
        {
            Location = new Point(leftMargin + 170, y + 6),
            Size = new Size(210, 24),
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Text = string.Empty
        };
        Controls.Add(lblStatus);
        y += 48;

        // Save button
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
        chkEnableCloudPerimeter.Checked = config.EnableCloudPerimeter;
        comboProviderType.SelectedIndex = (int)config.CloudPerimeterType;
        txtApiKey.Text = config.CloudPerimeterApiKey;
        txtEndpointUrl.Text = config.CloudPerimeterEndpoint;
        txtResourceId.Text = config.CloudPerimeterResourceId;
        txtSecondaryId.Text = config.CloudPerimeterSecondaryId;
        txtTertiaryId.Text = config.CloudPerimeterTertiaryId;
    }

    /// <summary>
    /// 儲存當前面板設定。
    /// </summary>
    public void SaveSettings()
    {
        IddsConfig config = IddsConfig.Instance;
        config.EnableCloudPerimeter = chkEnableCloudPerimeter.Checked;
        config.CloudPerimeterType = (CloudPerimeterType)Math.Max(0, comboProviderType.SelectedIndex);
        config.CloudPerimeterApiKey = txtApiKey.Text;
        config.CloudPerimeterEndpoint = txtEndpointUrl.Text;
        config.CloudPerimeterResourceId = txtResourceId.Text;
        config.CloudPerimeterSecondaryId = txtSecondaryId.Text;
        config.CloudPerimeterTertiaryId = txtTertiaryId.Text;
        config.SaveAppConfig();

        MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        CloudPerimeterSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task TestConnectionAsync()
    {
        lblStatus.ForeColor = Color.DarkOrange;
        lblStatus.Text = string.Empty;
        btnTestConnection.Enabled = false;

        try
        {
            var settings = new CloudPerimeterSettings
            {
                EnableCloudPerimeter = true,
                ProviderType = (CloudPerimeterType)Math.Max(0, comboProviderType.SelectedIndex),
                ApiKey = txtApiKey.Text,
                EndpointUrl = txtEndpointUrl.Text,
                ResourceId = txtResourceId.Text,
                SecondaryId = txtSecondaryId.Text,
                TertiaryId = txtTertiaryId.Text
            };

            var provider = CloudPerimeterProviderFactory.Create(settings);
            if (provider == null)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = string.Empty;
                return;
            }

            (bool success, string message) = await provider.TestConnectionAsync().ConfigureAwait(true);
            if (success)
            {
                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = string.Empty;
            }
            else
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = message;
            }
        }
        catch (Exception ex)
        {
            lblStatus.ForeColor = Color.Red;
            lblStatus.Text = ex.Message;
        }
        finally
        {
            btnTestConnection.Enabled = true;
        }
    }
}
