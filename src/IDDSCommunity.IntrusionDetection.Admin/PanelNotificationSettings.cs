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
        AutoScroll = true;
        HorizontalScroll.Enabled = false;
        HorizontalScroll.Visible = false;

        Font defaultFont = new("Segoe UI", 9F);
        Font sectionHeaderFont = new("Segoe UI", 10F, FontStyle.Bold);
        int x = 20;
        int controlWidth = 380;
        int currentY = 15;

        // === Section 1: E-Mail 通知設定 ===
        smartLabel5.AutoSize = true;
        smartLabel5.Font = sectionHeaderFont;
        smartLabel5.ForeColor = AccentColor;
        smartLabel5.Location = new Point(x, currentY);
        smartLabel5.Text = Strings.Get("E-mail notification settings");
        Controls.Add(smartLabel5);
        currentY += 28;

        smartLabel1.AutoSize = true;
        smartLabel1.Font = defaultFont;
        smartLabel1.ForeColor = BodyTextColor;
        smartLabel1.Location = new Point(x, currentY);
        smartLabel1.Text = Strings.Get("Basic notification");
        Controls.Add(smartLabel1);
        currentY += 20;

        checkBoxSoftLock.AutoSize = true;
        checkBoxSoftLock.Font = defaultFont;
        checkBoxSoftLock.ForeColor = BodyTextColor;
        checkBoxSoftLock.Location = new Point(x, currentY);
        checkBoxSoftLock.Text = Strings.Get("On soft lock events");
        checkBoxSoftLock.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxSoftLock);
        currentY += 24;

        checkBoxHardLocks.AutoSize = true;
        checkBoxHardLocks.Font = defaultFont;
        checkBoxHardLocks.ForeColor = BodyTextColor;
        checkBoxHardLocks.Location = new Point(x, currentY);
        checkBoxHardLocks.Text = Strings.Get("On hard lock events");
        checkBoxHardLocks.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxHardLocks);
        currentY += 24;

        checkBoxOnUnlock.AutoSize = true;
        checkBoxOnUnlock.Font = defaultFont;
        checkBoxOnUnlock.ForeColor = BodyTextColor;
        checkBoxOnUnlock.Location = new Point(x, currentY);
        checkBoxOnUnlock.Text = Strings.Get("On unlock events");
        checkBoxOnUnlock.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxOnUnlock);
        currentY += 26;

        smartLabelSummary.AutoSize = true;
        smartLabelSummary.Font = defaultFont;
        smartLabelSummary.ForeColor = BodyTextColor;
        smartLabelSummary.Location = new Point(x, currentY);
        smartLabelSummary.Text = Strings.Get("Reports");
        Controls.Add(smartLabelSummary);
        currentY += 20;

        checkBoxDailySummary.AutoSize = true;
        checkBoxDailySummary.Font = defaultFont;
        checkBoxDailySummary.ForeColor = BodyTextColor;
        checkBoxDailySummary.Location = new Point(x, currentY);
        checkBoxDailySummary.Text = Strings.Get("Daily report");
        checkBoxDailySummary.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxDailySummary);
        currentY += 24;

        checkBoxWeeklyReport.AutoSize = true;
        checkBoxWeeklyReport.Font = defaultFont;
        checkBoxWeeklyReport.ForeColor = BodyTextColor;
        checkBoxWeeklyReport.Location = new Point(x, currentY);
        checkBoxWeeklyReport.Text = Strings.Get("Weekly report");
        checkBoxWeeklyReport.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxWeeklyReport);
        currentY += 24;

        checkBoxMonthlyReport.AutoSize = true;
        checkBoxMonthlyReport.Font = defaultFont;
        checkBoxMonthlyReport.ForeColor = BodyTextColor;
        checkBoxMonthlyReport.Location = new Point(x, currentY);
        checkBoxMonthlyReport.Text = Strings.Get("Monthly report");
        checkBoxMonthlyReport.CheckedChanged += checkBox_CheckedChanged;
        Controls.Add(checkBoxMonthlyReport);
        currentY += 35;

        // === Section 2: Webhook 即時告警 ===
        smartLabelWebhookHeader = new SmartLabel
        {
            AutoSize = true,
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Webhook notifications (Teams / Slack / Discord / Telegram)")
        };
        Controls.Add(smartLabelWebhookHeader);
        currentY += 28;

        checkBoxEnableWebhook = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Enable Webhook alerts")
        };
        checkBoxEnableWebhook.CheckedChanged += (_, _) =>
        {
            UpdateWebhookControlsState();
            SetEditMode(true);
        };
        Controls.Add(checkBoxEnableWebhook);
        currentY += 26;

        labelWebhookPlatform = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Webhook platform")
        };
        comboBoxWebhookPlatform = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        comboBoxWebhookPlatform.Items.AddRange(["None (0)", "MicrosoftTeams (1)", "Slack (2)", "Discord (3)", "Telegram (4)", "GenericJson (5)"]);
        comboBoxWebhookPlatform.SelectedIndexChanged += (_, _) =>
        {
            UpdateWebhookControlsState();
            SetEditMode(true);
        };
        Controls.Add(labelWebhookPlatform);
        Controls.Add(comboBoxWebhookPlatform);
        currentY += 46;

        labelWebhookUrl = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Webhook URL / Endpoint")
        };
        textBoxWebhookUrl = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        textBoxWebhookUrl.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelWebhookUrl);
        Controls.Add(textBoxWebhookUrl);
        currentY += 46;

        labelTelegramToken = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Telegram bot token")
        };
        textBoxTelegramToken = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        textBoxTelegramToken.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelTelegramToken);
        Controls.Add(textBoxTelegramToken);
        currentY += 46;

        labelTelegramChatId = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Telegram chat ID")
        };
        textBoxTelegramChatId = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        textBoxTelegramChatId.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelTelegramChatId);
        Controls.Add(textBoxTelegramChatId);
        currentY += 46;

        checkBoxWebhookSoftLock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On soft lock events")
        };
        checkBoxWebhookSoftLock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookSoftLock);
        currentY += 24;

        checkBoxWebhookHardLocks = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On hard lock events")
        };
        checkBoxWebhookHardLocks.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookHardLocks);
        currentY += 24;

        checkBoxWebhookOnUnlock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On unlock events")
        };
        checkBoxWebhookOnUnlock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxWebhookOnUnlock);
        currentY += 26;

        buttonTestWebhook = new Button
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Size = new Size(140, 28),
            Text = Strings.Get("Test Webhook"),
            UseVisualStyleBackColor = false
        };
        buttonTestWebhook.Click += async (_, _) => await RunTestWebhookAsync();
        Controls.Add(buttonTestWebhook);
        currentY += 38;

        // === Section 3: Syslog & SIEM 整合 ===
        smartLabelSyslogHeader = new SmartLabel
        {
            AutoSize = true,
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Syslog & SIEM integration (RFC 5424 / RFC 3164 / CEF)")
        };
        Controls.Add(smartLabelSyslogHeader);
        currentY += 28;

        checkBoxEnableSyslog = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Enable Syslog alerts")
        };
        checkBoxEnableSyslog.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxEnableSyslog);
        currentY += 26;

        labelSyslogHost = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Syslog server host")
        };
        textBoxSyslogHost = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(230, 23)
        };
        textBoxSyslogHost.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogHost);
        Controls.Add(textBoxSyslogHost);

        labelSyslogPort = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x + 245, currentY),
            Text = Strings.Get("Syslog server port")
        };
        numSyslogPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x + 245, currentY + 18),
            Size = new Size(105, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 514
        };
        numSyslogPort.ValueChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogPort);
        Controls.Add(numSyslogPort);
        currentY += 46;

        labelSyslogProtocol = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Syslog protocol")
        };
        comboBoxSyslogProtocol = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(160, 23)
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
            Location = new Point(x + 175, currentY),
            Text = Strings.Get("Syslog format")
        };
        comboBoxSyslogFormat = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x + 175, currentY + 18),
            Size = new Size(175, 23)
        };
        comboBoxSyslogFormat.Items.AddRange(["RFC 5424 (0)", "RFC 3164 (1)", "CEF (2)"]);
        comboBoxSyslogFormat.SelectedIndexChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelSyslogFormat);
        Controls.Add(comboBoxSyslogFormat);
        currentY += 46;

        checkBoxSyslogSoftLock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On soft lock events")
        };
        checkBoxSyslogSoftLock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogSoftLock);
        currentY += 24;

        checkBoxSyslogHardLocks = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On hard lock events")
        };
        checkBoxSyslogHardLocks.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogHardLocks);
        currentY += 24;

        checkBoxSyslogOnUnlock = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("On unlock events")
        };
        checkBoxSyslogOnUnlock.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxSyslogOnUnlock);
        currentY += 26;

        buttonTestSyslog = new Button
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Size = new Size(140, 28),
            Text = Strings.Get("Test Syslog"),
            UseVisualStyleBackColor = false
        };
        buttonTestSyslog.Click += async (_, _) => await RunTestSyslogAsync();
        Controls.Add(buttonTestSyslog);
        currentY += 38;

        // === Section 4: Prometheus / OpenMetrics 可觀測性 ===
        smartLabelMetricsHeader = new SmartLabel
        {
            AutoSize = true,
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Prometheus / OpenMetrics observability")
        };
        Controls.Add(smartLabelMetricsHeader);
        currentY += 28;

        checkBoxEnableMetrics = new CheckBox
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Enable Prometheus metrics endpoint")
        };
        checkBoxEnableMetrics.CheckedChanged += (_, _) => SetEditMode(true);
        Controls.Add(checkBoxEnableMetrics);
        currentY += 26;

        labelMetricsListenIp = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Metrics listen IP address (e.g. 0.0.0.0 or 127.0.0.1)")
        };
        textBoxMetricsListenIp = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(230, 23)
        };
        textBoxMetricsListenIp.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsListenIp);
        Controls.Add(textBoxMetricsListenIp);

        labelMetricsPort = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x + 245, currentY),
            Text = Strings.Get("Metrics port")
        };
        numMetricsPort = new NumericUpDown
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x + 245, currentY + 18),
            Size = new Size(105, 23),
            Minimum = 1,
            Maximum = 65535,
            Value = 9100
        };
        numMetricsPort.ValueChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsPort);
        Controls.Add(numMetricsPort);
        currentY += 46;

        labelMetricsAllowed = new Label
        {
            AutoSize = true,
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY),
            Text = Strings.Get("Allowed monitoring networks CIDR (e.g. 10.0.0.0/8, 192.168.1.0/24)")
        };
        textBoxMetricsAllowedNetworks = new TextBox
        {
            Font = defaultFont,
            ForeColor = BodyTextColor,
            Location = new Point(x, currentY + 18),
            Size = new Size(controlWidth, 23)
        };
        textBoxMetricsAllowedNetworks.TextChanged += (_, _) => SetEditMode(true);
        Controls.Add(labelMetricsAllowed);
        Controls.Add(textBoxMetricsAllowedNetworks);
        currentY += 50;

        // === Section 5: 儲存與復原按鈕 ===
        buttonSave.BackColor = AccentColor;
        buttonSave.ForeColor = Color.White;
        buttonSave.FlatStyle = FlatStyle.Flat;
        buttonSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        buttonSave.Location = new Point(x, currentY);
        buttonSave.Size = new Size(120, 32);
        buttonSave.Text = Strings.Get("&Save");
        buttonSave.Click += buttonSave_Click;
        Controls.Add(buttonSave);

        buttonDiscard.BackColor = Color.White;
        buttonDiscard.ForeColor = BodyTextColor;
        buttonDiscard.FlatStyle = FlatStyle.Flat;
        buttonDiscard.Font = defaultFont;
        buttonDiscard.Location = new Point(x + 130, currentY);
        buttonDiscard.Size = new Size(120, 32);
        buttonDiscard.Text = Strings.Get("&Discard");
        buttonDiscard.Click += buttonDiscard_Click;
        Controls.Add(buttonDiscard);
        currentY += 50;

        Load += new EventHandler(PanelNotificationSettings_Load);
        UpdateWebhookControlsState();
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

        int x = 20;
        int currentY = comboBoxWebhookPlatform.Location.Y + 28;

        if (isTelegram)
        {
            labelTelegramToken.Location = new Point(x, currentY);
            textBoxTelegramToken.Location = new Point(x, currentY + 18);
            currentY += 46;

            labelTelegramChatId.Location = new Point(x, currentY);
            textBoxTelegramChatId.Location = new Point(x, currentY + 18);
            currentY += 46;
        }
        else
        {
            labelWebhookUrl.Location = new Point(x, currentY);
            textBoxWebhookUrl.Location = new Point(x, currentY + 18);
            currentY += 46;
        }

        checkBoxWebhookSoftLock.Location = new Point(x, currentY);
        currentY += 24;
        checkBoxWebhookHardLocks.Location = new Point(x, currentY);
        currentY += 24;
        checkBoxWebhookOnUnlock.Location = new Point(x, currentY);
        currentY += 26;
        buttonTestWebhook.Location = new Point(x, currentY);
        currentY += 38;

        smartLabelSyslogHeader.Location = new Point(x, currentY);
        currentY += 28;
        checkBoxEnableSyslog.Location = new Point(x, currentY);
        currentY += 26;
        labelSyslogHost.Location = new Point(x, currentY);
        textBoxSyslogHost.Location = new Point(x, currentY + 18);
        labelSyslogPort.Location = new Point(x + 245, currentY);
        numSyslogPort.Location = new Point(x + 245, currentY + 18);
        currentY += 46;

        labelSyslogProtocol.Location = new Point(x, currentY);
        comboBoxSyslogProtocol.Location = new Point(x, currentY + 18);
        labelSyslogFormat.Location = new Point(x + 175, currentY);
        comboBoxSyslogFormat.Location = new Point(x + 175, currentY + 18);
        currentY += 46;

        checkBoxSyslogSoftLock.Location = new Point(x, currentY);
        currentY += 24;
        checkBoxSyslogHardLocks.Location = new Point(x, currentY);
        currentY += 24;
        checkBoxSyslogOnUnlock.Location = new Point(x, currentY);
        currentY += 26;
        buttonTestSyslog.Location = new Point(x, currentY);
        currentY += 38;

        smartLabelMetricsHeader.Location = new Point(x, currentY);
        currentY += 28;
        checkBoxEnableMetrics.Location = new Point(x, currentY);
        currentY += 26;
        labelMetricsListenIp.Location = new Point(x, currentY);
        textBoxMetricsListenIp.Location = new Point(x, currentY + 18);
        labelMetricsPort.Location = new Point(x + 245, currentY);
        numMetricsPort.Location = new Point(x + 245, currentY + 18);
        currentY += 46;

        labelMetricsAllowed.Location = new Point(x, currentY);
        textBoxMetricsAllowedNetworks.Location = new Point(x, currentY + 18);
        currentY += 50;

        buttonSave.Location = new Point(x, currentY);
        buttonDiscard.Location = new Point(x + 130, currentY);
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
        SetEditMode(false);
        MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void buttonDiscard_Click(object? sender, EventArgs e) => LoadData();

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

    private void checkBox_CheckedChanged(object? sender, EventArgs e) => SetEditMode(true);
}
