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
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);

    // Webhook UI Controls
    private readonly SmartLabel smartLabelWebhookHeader;
    private readonly CheckBox checkBoxEnableWebhook;
    private readonly Label labelWebhookPlatform;
    private readonly ComboBox comboBoxWebhookPlatform;
    private readonly Label labelWebhookUrl;
    private readonly TextBox textBoxWebhookUrl;
    private readonly Label labelTelegramToken;
    private readonly TextBox textBoxTelegramToken;
    private readonly Label labelTelegramChatId;
    private readonly TextBox textBoxTelegramChatId;
    private readonly CheckBox checkBoxWebhookSoftLock;
    private readonly CheckBox checkBoxWebhookHardLocks;
    private readonly CheckBox checkBoxWebhookOnUnlock;
    private readonly Button buttonTestWebhook;

    // Syslog UI Controls
    private readonly SmartLabel smartLabelSyslogHeader;
    private readonly CheckBox checkBoxEnableSyslog;
    private readonly Label labelSyslogHost;
    private readonly TextBox textBoxSyslogHost;
    private readonly Label labelSyslogPort;
    private readonly NumericUpDown numSyslogPort;
    private readonly Label labelSyslogProtocol;
    private readonly ComboBox comboBoxSyslogProtocol;
    private readonly Label labelSyslogFormat;
    private readonly ComboBox comboBoxSyslogFormat;
    private readonly CheckBox checkBoxSyslogSoftLock;
    private readonly CheckBox checkBoxSyslogHardLocks;
    private readonly CheckBox checkBoxSyslogOnUnlock;
    private readonly Button buttonTestSyslog;

    // Observability / Prometheus UI Controls
    private readonly SmartLabel smartLabelMetricsHeader;
    private readonly CheckBox checkBoxEnableMetrics;
    private readonly Label labelMetricsListenIp;
    private readonly TextBox textBoxMetricsListenIp;
    private readonly Label labelMetricsPort;
    private readonly NumericUpDown numMetricsPort;
    private readonly Label labelMetricsAllowed;
    private readonly TextBox textBoxMetricsAllowedNetworks;

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

        Font defaultFont = new("Segoe UI", 9F);
        int webhookX = 370;

        smartLabelWebhookHeader = new SmartLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            ForeColor = AccentColor,
            Location = new Point(webhookX, 0),
            Text = Strings.Get("Webhook notifications (Teams / Slack / Discord / Telegram)")
        };
        Controls.Add(smartLabelWebhookHeader);

        checkBoxEnableWebhook = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 40),
            Text = Strings.Get("Enable Webhook alerts")
        };
        checkBoxEnableWebhook.CheckedChanged += (_, _) =>
        {
            UpdateWebhookControlsState();
            SetEditMode(true);
        };
        Controls.Add(checkBoxEnableWebhook);

        labelWebhookPlatform = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 68),
            Text = Strings.Get("Webhook platform")
        };
        comboBoxWebhookPlatform = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 86),
            Size = new Size(260, 23)
        };
        comboBoxWebhookPlatform.Items.AddRange(["None (0)", "MicrosoftTeams (1)", "Slack (2)", "Discord (3)", "Telegram (4)", "GenericJson (5)"]);
        comboBoxWebhookPlatform.SelectedIndexChanged += (_, _) =>
        {
            UpdateWebhookControlsState();
            SetEditMode(true);
        };
        Controls.Add(labelWebhookPlatform);
        Controls.Add(comboBoxWebhookPlatform);

        labelWebhookUrl = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 116),
            Text = Strings.Get("Webhook URL / Endpoint")
        };
        textBoxWebhookUrl = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 134),
            Size = new Size(320, 23)
        };
        textBoxWebhookUrl.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelWebhookUrl);
        Controls.Add(textBoxWebhookUrl);

        labelTelegramToken = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 164),
            Text = Strings.Get("Telegram bot token")
        };
        textBoxTelegramToken = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 182),
            Size = new Size(320, 23)
        };
        textBoxTelegramToken.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelTelegramToken);
        Controls.Add(textBoxTelegramToken);

        labelTelegramChatId = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 212),
            Text = Strings.Get("Telegram chat ID")
        };
        textBoxTelegramChatId = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 230),
            Size = new Size(320, 23)
        };
        textBoxTelegramChatId.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelTelegramChatId);
        Controls.Add(textBoxTelegramChatId);

        checkBoxWebhookSoftLock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 260),
            Text = Strings.Get("On soft lock events")
        };
        checkBoxWebhookSoftLock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookSoftLock);

        checkBoxWebhookHardLocks = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 282),
            Text = Strings.Get("On hard lock events")
        };
        checkBoxWebhookHardLocks.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookHardLocks);

        checkBoxWebhookOnUnlock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 304),
            Text = Strings.Get("On unlock events")
        };
        checkBoxWebhookOnUnlock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookOnUnlock);

        buttonTestWebhook = new Button
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 339),
            Size = new Size(130, 26),
            Text = Strings.Get("Test Webhook"),
            UseVisualStyleBackColor = false
        };
        buttonTestWebhook.Click += async (_, _) => await RunTestWebhookAsync();
        Controls.Add(buttonTestWebhook);

        // Syslog UI Controls
        smartLabelSyslogHeader = new SmartLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            ForeColor = AccentColor,
            Location = new Point(webhookX, 385),
            Text = Strings.Get("Syslog & SIEM integration (RFC 5424 / RFC 3164 / CEF)")
        };
        Controls.Add(smartLabelSyslogHeader);

        checkBoxEnableSyslog = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 415),
            Text = Strings.Get("Enable Syslog alerts")
        };
        checkBoxEnableSyslog.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxEnableSyslog);

        labelSyslogHost = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 440),
            Text = Strings.Get("Syslog server host")
        };
        textBoxSyslogHost = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 458),
            Size = new Size(200, 23)
        };
        textBoxSyslogHost.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogHost);
        Controls.Add(textBoxSyslogHost);

        labelSyslogPort = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX + 215, 440),
            Text = Strings.Get("Syslog server port")
        };
        numSyslogPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX + 215, 458),
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 514
        };
        numSyslogPort.ValueChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogPort);
        Controls.Add(numSyslogPort);

        labelSyslogProtocol = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 488),
            Text = Strings.Get("Syslog protocol")
        };
        comboBoxSyslogProtocol = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 506),
            Size = new Size(130, 23)
        };
        comboBoxSyslogProtocol.Items.AddRange(["UDP (0)", "TCP (1)", "TLS (2)"]);
        comboBoxSyslogProtocol.SelectedIndexChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogProtocol);
        Controls.Add(comboBoxSyslogProtocol);

        labelSyslogFormat = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX + 145, 488),
            Text = Strings.Get("Syslog format")
        };
        comboBoxSyslogFormat = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX + 145, 506),
            Size = new Size(150, 23)
        };
        comboBoxSyslogFormat.Items.AddRange(["RFC 5424 (0)", "RFC 3164 (1)", "CEF (2)"]);
        comboBoxSyslogFormat.SelectedIndexChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogFormat);
        Controls.Add(comboBoxSyslogFormat);

        checkBoxSyslogSoftLock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 536),
            Text = Strings.Get("On soft lock events")
        };
        checkBoxSyslogSoftLock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogSoftLock);

        checkBoxSyslogHardLocks = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 558),
            Text = Strings.Get("On hard lock events")
        };
        checkBoxSyslogHardLocks.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogHardLocks);

        checkBoxSyslogOnUnlock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 580),
            Text = Strings.Get("On unlock events")
        };
        checkBoxSyslogOnUnlock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogOnUnlock);

        buttonTestSyslog = new Button
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(webhookX, 610),
            Size = new Size(130, 26),
            Text = Strings.Get("Test Syslog"),
            UseVisualStyleBackColor = false
        };
        buttonTestSyslog.Click += async (_, _) => await RunTestSyslogAsync();
        Controls.Add(buttonTestSyslog);

        // Observability / Prometheus UI Controls (Left column bottom)
        smartLabelMetricsHeader = new SmartLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            ForeColor = AccentColor,
            Location = new Point(0, 385),
            Text = Strings.Get("Prometheus / OpenMetrics observability")
        };
        Controls.Add(smartLabelMetricsHeader);

        checkBoxEnableMetrics = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(0, 415),
            Text = Strings.Get("Enable Prometheus metrics endpoint")
        };
        checkBoxEnableMetrics.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxEnableMetrics);

        labelMetricsListenIp = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(0, 440),
            Text = Strings.Get("Metrics listen IP address (e.g. 0.0.0.0 or 127.0.0.1)")
        };
        textBoxMetricsListenIp = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(0, 458),
            Size = new Size(200, 23)
        };
        textBoxMetricsListenIp.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsListenIp);
        Controls.Add(textBoxMetricsListenIp);

        labelMetricsPort = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(215, 440),
            Text = Strings.Get("Metrics port")
        };
        numMetricsPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(215, 458),
            Size = new Size(80, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 9100
        };
        numMetricsPort.ValueChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsPort);
        Controls.Add(numMetricsPort);

        labelMetricsAllowed = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(0, 488),
            Text = Strings.Get("Allowed monitoring networks CIDR (e.g. 10.0.0.0/8, 192.168.1.0/24)")
        };
        textBoxMetricsAllowedNetworks = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(0, 506),
            Size = new Size(300, 23)
        };
        textBoxMetricsAllowedNetworks.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsAllowed);
        Controls.Add(textBoxMetricsAllowedNetworks);

        Load += new EventHandler(PanelNotificationSettings_Load);
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click);
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

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                MessageBox.Show(
                    Strings.Get("Webhook test failed. Please verify the URL and network connectivity."),
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(targetUrl, content);

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
    /// 取得或設定 IsInEditMode。
    /// </summary>
    public bool IsInEditMode { get; set; }

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
        SetEditMode(false);
    }

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData();
        ToggleEditMode();
    }

    private void ToggleEditMode()
    {
        if (!IsInEditMode)
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_delete;
            IsInEditMode = true;
        }
        else
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_edit;
            IsInEditMode = false;
        }
        pictureBoxSave.Visible = IsInEditMode;
        checkBoxSoftLock.Enabled = IsInEditMode;
        checkBoxHardLocks.Enabled = IsInEditMode;
        checkBoxOnUnlock.Enabled = IsInEditMode;
        checkBoxDailySummary.Enabled = IsInEditMode;
        checkBoxWeeklyReport.Enabled = IsInEditMode;
        checkBoxMonthlyReport.Enabled = IsInEditMode;
    }

    private void pictureBoxSave_Click(object sender, EventArgs e) => ToggleEditMode();

    private void pictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X + 1, loc.Y + 1);
    }

    private void pictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X - 1, loc.Y - 1);
    }

    private void buttonSave_Click(object sender, EventArgs e)
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
        SetEditMode(false);
    }

    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();

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

        SetEditMode(true);
    }

    private void OnNotificationSettingsChanged() => NotificationSettingsChanged?.Invoke(this, EventArgs.Empty);

    private void textBox_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);

    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }

    private void checkBox_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);
}
