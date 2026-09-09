using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供事件通知收件者、報表排程與多平台 Webhook 即時告警設定之面板控制項。
/// </summary>
public partial class PanelNotificationSettings : UserControl
{
    private const string DefaultMetricsListenIp = "0.0.0.0";
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);
    private static readonly Color AccentColor = Color.FromArgb(15, 118, 110);
    private static readonly HttpClient SharedWebhookTestClient = IDDSCommunity.IntrusionDetection.Shared.Network.HttpClientHelper.CreatePooledClient(TimeSpan.FromSeconds(10));

    /// <summary>
    /// 當 NotificationSettingsChanged 時引發之事件。
    /// </summary>
    public event EventHandler? NotificationSettingsChanged;

    /// <summary>
    /// 初始化 <see cref="PanelNotificationSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelNotificationSettings()
    {
        InitializeComponent();

        comboBoxWebhookPlatform.Items.AddRange([
            Strings.Get("None"),
            "Microsoft Teams",
            "Slack",
            "Discord",
            "Telegram",
            Strings.Get("Generic JSON")
        ]);
        comboBoxWebhookPlatform.SelectedIndexChanged += (_, _) => UpdateWebhookControlsState();
        checkBoxEnableWebhook.CheckedChanged += (_, _) => UpdateWebhookControlsState();
        buttonTestWebhook.Click += async (_, _) => await RunTestWebhookAsync();
        buttonTestSyslog.Click += async (_, _) => await RunTestSyslogAsync();

        Load += new EventHandler(PanelNotificationSettings_Load);
        UpdateWebhookControlsState();
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click, container: headerPanel);
    }

    private void UpdateWebhookControlsState()
    {
        bool isTelegram = comboBoxWebhookPlatform.SelectedIndex == (int)WebhookPlatform.Telegram;
        labelWebhookUrl.Visible = !isTelegram;
        textBoxWebhookUrl.Visible = !isTelegram;
        labelTelegramToken.Visible = isTelegram;
        textBoxTelegramToken.Visible = isTelegram;
        labelTelegramChatId.Visible = isTelegram;
        textBoxTelegramChatId.Visible = isTelegram;
    }

    private async Task RunTestWebhookAsync()
    {
        try
        {
            buttonTestWebhook.Enabled = false;
            WebhookPlatform platform = (WebhookPlatform)Math.Clamp(comboBoxWebhookPlatform.SelectedIndex, 0, 5);
            string eventTitle = Strings.Get("AttackDetected") + " (Test)";
            string ipAddress = "203.0.113.199";
            string statusName = Strings.Get("Hard lock");
            string agentName = Strings.AppTitle;
            string details = Strings.Get("Configuration was saved successfully.");

            string json = WebhookPayloadBuilder.BuildPayload(
                platform,
                eventTitle,
                ipAddress,
                statusName,
                agentName,
                details,
                DateTime.UtcNow,
                textBoxTelegramChatId.Text.Trim());

            string targetUrl = platform == WebhookPlatform.Telegram
                ? $"https://api.telegram.org/bot{textBoxTelegramToken.Text.Trim()}/sendMessage"
                : textBoxWebhookUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(targetUrl) || IDDSCommunity.IntrusionDetection.Shared.Network.NetworkEndpointValidator.IsBlockedImdsOrLinkLocal(targetUrl))
            {
                MessageBox.Show(
                    Strings.Get("Webhook test failed. Please verify the URL and network connectivity."),
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await SharedWebhookTestClient.PostAsync(targetUrl, content);

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    Strings.Get("Webhook test was sent successfully."),
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    Strings.Get("Webhook test failed. Please verify the URL and network connectivity.") + $" (HTTP {(int)response.StatusCode})",
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Strings.Get("Webhook test failed. Please verify the URL and network connectivity.") + $" ({ex.Message})",
                Strings.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            buttonTestWebhook.Enabled = true;
        }
    }

    private async Task RunTestSyslogAsync()
    {
        try
        {
            buttonTestSyslog.Enabled = false;
            string host = textBoxSyslogHost.Text.Trim();
            int port = (int)numSyslogPort.Value;
            SyslogProtocol proto = (SyslogProtocol)Math.Clamp(comboBoxSyslogProtocol.SelectedIndex, 0, 2);
            SyslogFormat fmt = (SyslogFormat)Math.Clamp(comboBoxSyslogFormat.SelectedIndex, 0, 2);

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show(
                    Strings.Get("Syslog test failed. Please verify the host and network connectivity."),
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string testMessage = SyslogPayloadBuilder.BuildMessage(
                fmt,
                LockType.HardLock,
                "203.0.113.199",
                Strings.AppTitle,
                "IDDS Community Syslog connectivity test message.",
                DateTime.UtcNow);

            byte[] data = Encoding.UTF8.GetBytes(testMessage + "\n");
            if (proto == SyslogProtocol.Udp)
            {
                using var udpClient = new System.Net.Sockets.UdpClient();
                await udpClient.SendAsync(data, data.Length, host, port);
            }
            else if (proto == SyslogProtocol.Tcp)
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                await tcpClient.ConnectAsync(host, port);
                using var stream = tcpClient.GetStream();
                await stream.WriteAsync(data);
                await stream.FlushAsync();
            }
            else if (proto == SyslogProtocol.Tls)
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                await tcpClient.ConnectAsync(host, port);
                using var sslStream = new System.Net.Security.SslStream(tcpClient.GetStream(), false, (_, _, _, _) => true);
                await sslStream.AuthenticateAsClientAsync(host);
                await sslStream.WriteAsync(data);
                await sslStream.FlushAsync();
            }

            MessageBox.Show(
                Strings.Get("Syslog test was sent successfully."),
                Strings.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Strings.Get("Syslog test failed. Please verify the host and network connectivity.") + $" ({ex.Message})",
                Strings.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            buttonTestSyslog.Enabled = true;
        }
    }

    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    void PanelNotificationSettings_Load(object? sender, EventArgs e) => LoadData();

    /// <summary>
    /// Loads data.
    /// </summary>
    public void LoadData()
    {
        NotificationSettings settings = NotificationSettings.Instance;

        checkBoxSoftLock.Checked = settings.OnSoftLock;
        checkBoxHardLocks.Checked = settings.OnHardLock;
        checkBoxOnUnlock.Checked = settings.OnUnlock;
        checkBoxDailySummary.Checked = settings.SummaryReportDaily;
        checkBoxWeeklyReport.Checked = settings.SummaryReportWeekly;
        checkBoxMonthlyReport.Checked = settings.SummaryReportMonthly;
        checkBoxDailySummary.Enabled = true;
        checkBoxWeeklyReport.Enabled = true;
        checkBoxMonthlyReport.Enabled = true;

        checkBoxEnableWebhook.Checked = settings.EnableWebhook;
        comboBoxWebhookPlatform.SelectedIndex = (int)settings.WebhookPlatform;
        textBoxWebhookUrl.Text = settings.WebhookUrl;
        textBoxTelegramToken.Text = settings.TelegramBotToken;
        textBoxTelegramChatId.Text = settings.TelegramChatId;
        checkBoxWebhookSoftLock.Checked = settings.WebhookOnSoftLock;
        checkBoxWebhookHardLocks.Checked = settings.WebhookOnHardLock;
        checkBoxWebhookOnUnlock.Checked = settings.WebhookOnUnlock;

        checkBoxEnableSyslog.Checked = settings.EnableSyslog;
        textBoxSyslogHost.Text = settings.SyslogHost;
        numSyslogPort.Value = Math.Clamp(settings.SyslogPort, 1, 65535);
        comboBoxSyslogProtocol.SelectedIndex = (int)settings.SyslogProtocol;
        comboBoxSyslogFormat.SelectedIndex = (int)settings.SyslogFormat;
        checkBoxSyslogSoftLock.Checked = settings.SyslogOnSoftLock;
        checkBoxSyslogHardLocks.Checked = settings.SyslogOnHardLock;
        checkBoxSyslogOnUnlock.Checked = settings.SyslogOnUnlock;

        checkBoxEnableMetrics.Checked = settings.EnableMetricsEndpoint;
        textBoxMetricsListenIp.Text = settings.MetricsListenIp;
        numMetricsPort.Value = Math.Clamp(settings.MetricsPort, 1, 65535);
        textBoxMetricsAllowedNetworks.Text = settings.MetricsAllowedNetworks;

        UpdateWebhookControlsState();
    }

    private void buttonSave_Click(object? sender, EventArgs e)
    {
        NotificationSettings settings = NotificationSettings.Instance;

        settings.OnSoftLock = checkBoxSoftLock.Checked;
        settings.OnHardLock = checkBoxHardLocks.Checked;
        settings.OnUnlock = checkBoxOnUnlock.Checked;
        settings.SummaryReportDaily = checkBoxDailySummary.Checked;
        settings.SummaryReportWeekly = checkBoxWeeklyReport.Checked;
        settings.SummaryReportMonthly = checkBoxMonthlyReport.Checked;

        settings.EnableWebhook = checkBoxEnableWebhook.Checked;
        settings.WebhookPlatform = (WebhookPlatform)Math.Clamp(comboBoxWebhookPlatform.SelectedIndex, 0, 5);
        settings.WebhookUrl = textBoxWebhookUrl.Text.Trim();
        settings.TelegramBotToken = textBoxTelegramToken.Text.Trim();
        settings.TelegramChatId = textBoxTelegramChatId.Text.Trim();
        settings.WebhookOnSoftLock = checkBoxWebhookSoftLock.Checked;
        settings.WebhookOnHardLock = checkBoxWebhookHardLocks.Checked;
        settings.WebhookOnUnlock = checkBoxWebhookOnUnlock.Checked;

        settings.EnableSyslog = checkBoxEnableSyslog.Checked;
        settings.SyslogHost = textBoxSyslogHost.Text.Trim();
        settings.SyslogPort = (int)numSyslogPort.Value;
        settings.SyslogProtocol = (SyslogProtocol)Math.Clamp(comboBoxSyslogProtocol.SelectedIndex, 0, 2);
        settings.SyslogFormat = (SyslogFormat)Math.Clamp(comboBoxSyslogFormat.SelectedIndex, 0, 2);
        settings.SyslogOnSoftLock = checkBoxSyslogSoftLock.Checked;
        settings.SyslogOnHardLock = checkBoxSyslogHardLocks.Checked;
        settings.SyslogOnUnlock = checkBoxSyslogOnUnlock.Checked;

        settings.EnableMetricsEndpoint = checkBoxEnableMetrics.Checked;
        settings.MetricsListenIp = textBoxMetricsListenIp.Text.Trim();
        settings.MetricsPort = (int)numMetricsPort.Value;
        settings.MetricsAllowedNetworks = textBoxMetricsAllowedNetworks.Text.Trim();

        IddsConfig.Instance.SaveAppConfig();
        OnNotificationSettingsChanged();
        MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ResetDefaults_Click(object? sender, EventArgs e)
    {
        checkBoxSoftLock.Checked = false;
        checkBoxHardLocks.Checked = false;
        checkBoxOnUnlock.Checked = false;
        checkBoxDailySummary.Checked = false;
        checkBoxWeeklyReport.Checked = false;
        checkBoxMonthlyReport.Checked = false;

        checkBoxEnableWebhook.Checked = false;
        comboBoxWebhookPlatform.SelectedIndex = 0;
        textBoxWebhookUrl.Text = string.Empty;
        textBoxTelegramToken.Text = string.Empty;
        textBoxTelegramChatId.Text = string.Empty;
        checkBoxWebhookSoftLock.Checked = false;
        checkBoxWebhookHardLocks.Checked = false;
        checkBoxWebhookOnUnlock.Checked = false;

        checkBoxEnableSyslog.Checked = false;
        textBoxSyslogHost.Text = string.Empty;
        numSyslogPort.Value = 514;
        comboBoxSyslogProtocol.SelectedIndex = 0;
        comboBoxSyslogFormat.SelectedIndex = 0;
        checkBoxSyslogSoftLock.Checked = false;
        checkBoxSyslogHardLocks.Checked = false;
        checkBoxSyslogOnUnlock.Checked = false;

        checkBoxEnableMetrics.Checked = false;
        textBoxMetricsListenIp.Text = DefaultMetricsListenIp;
        numMetricsPort.Value = 9100;
        textBoxMetricsAllowedNetworks.Text = string.Empty;
    }

    private void OnNotificationSettingsChanged() => NotificationSettingsChanged?.Invoke(this, EventArgs.Empty);
}
