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
public sealed partial class PanelCloudPerimeterSettings : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(15, 118, 110);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    /// <summary>
    /// 當雲端邊界安全設定變更並儲存時引發之事件。
    /// </summary>
    public event EventHandler? CloudPerimeterSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelCloudPerimeterSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelCloudPerimeterSettings()
    {
        InitializeComponent();

        comboProviderType.Items.AddRange([
            Strings.Get("None"),
            "AWS WAFv2",
            "Azure NSG",
            "GCP Cloud Armor",
            "Cloudflare WAF",
            Strings.Get("Chunghwa HiCloud (deny unsupported)"),
            Strings.Get("Generic Webhook")
        ]);
        comboProviderType.SelectedIndex = 0;

        btnTestConnection.Click += async (s, e) => await TestConnectionAsync().ConfigureAwait(true);
        btnSave.Click += (s, e) => SaveSettings();

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

            using var providerLifetime = provider as IDisposable;
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
