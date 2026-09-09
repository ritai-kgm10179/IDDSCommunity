namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelNotificationSettings
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">若要釋放受控資源則為 true；否則為 false。</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
        this.headerPanel = new System.Windows.Forms.Panel();
        this.smartLabel5 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxSoftLock = new System.Windows.Forms.CheckBox();
        this.checkBoxHardLocks = new System.Windows.Forms.CheckBox();
        this.checkBoxOnUnlock = new System.Windows.Forms.CheckBox();
        this.smartLabelSummary = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxDailySummary = new System.Windows.Forms.CheckBox();
        this.checkBoxWeeklyReport = new System.Windows.Forms.CheckBox();
        this.checkBoxMonthlyReport = new System.Windows.Forms.CheckBox();
        this.smartLabelWebhookHeader = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxEnableWebhook = new System.Windows.Forms.CheckBox();
        this.labelWebhookPlatform = new System.Windows.Forms.Label();
        this.comboBoxWebhookPlatform = new System.Windows.Forms.ComboBox();
        this.labelWebhookUrl = new System.Windows.Forms.Label();
        this.textBoxWebhookUrl = new System.Windows.Forms.TextBox();
        this.labelTelegramToken = new System.Windows.Forms.Label();
        this.textBoxTelegramToken = new System.Windows.Forms.TextBox();
        this.labelTelegramChatId = new System.Windows.Forms.Label();
        this.textBoxTelegramChatId = new System.Windows.Forms.TextBox();
        this.checkBoxWebhookSoftLock = new System.Windows.Forms.CheckBox();
        this.checkBoxWebhookHardLocks = new System.Windows.Forms.CheckBox();
        this.checkBoxWebhookOnUnlock = new System.Windows.Forms.CheckBox();
        this.buttonTestWebhook = new System.Windows.Forms.Button();
        this.smartLabelSyslogHeader = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxEnableSyslog = new System.Windows.Forms.CheckBox();
        this.labelSyslogHost = new System.Windows.Forms.Label();
        this.textBoxSyslogHost = new System.Windows.Forms.TextBox();
        this.labelSyslogPort = new System.Windows.Forms.Label();
        this.numSyslogPort = new System.Windows.Forms.NumericUpDown();
        this.labelSyslogProtocol = new System.Windows.Forms.Label();
        this.comboBoxSyslogProtocol = new System.Windows.Forms.ComboBox();
        this.labelSyslogFormat = new System.Windows.Forms.Label();
        this.comboBoxSyslogFormat = new System.Windows.Forms.ComboBox();
        this.checkBoxSyslogSoftLock = new System.Windows.Forms.CheckBox();
        this.checkBoxSyslogHardLocks = new System.Windows.Forms.CheckBox();
        this.checkBoxSyslogOnUnlock = new System.Windows.Forms.CheckBox();
        this.buttonTestSyslog = new System.Windows.Forms.Button();
        this.smartLabelMetricsHeader = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxEnableMetrics = new System.Windows.Forms.CheckBox();
        this.labelMetricsListenIp = new System.Windows.Forms.Label();
        this.textBoxMetricsListenIp = new System.Windows.Forms.TextBox();
        this.labelMetricsPort = new System.Windows.Forms.Label();
        this.numMetricsPort = new System.Windows.Forms.NumericUpDown();
        this.labelMetricsAllowed = new System.Windows.Forms.Label();
        this.textBoxMetricsAllowedNetworks = new System.Windows.Forms.TextBox();
        this.buttonSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numSyslogPort)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numMetricsPort)).BeginInit();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.headerPanel, 0, 0);
        this.tableLayoutMain.Controls.Add(this.smartLabel1, 0, 1);
        this.tableLayoutMain.Controls.Add(this.checkBoxSoftLock, 0, 2);
        this.tableLayoutMain.Controls.Add(this.checkBoxHardLocks, 0, 3);
        this.tableLayoutMain.Controls.Add(this.checkBoxOnUnlock, 0, 4);
        this.tableLayoutMain.Controls.Add(this.smartLabelSummary, 0, 5);
        this.tableLayoutMain.Controls.Add(this.checkBoxDailySummary, 0, 6);
        this.tableLayoutMain.Controls.Add(this.checkBoxWeeklyReport, 0, 7);
        this.tableLayoutMain.Controls.Add(this.checkBoxMonthlyReport, 0, 8);
        this.tableLayoutMain.Controls.Add(this.smartLabelWebhookHeader, 0, 9);
        this.tableLayoutMain.Controls.Add(this.checkBoxEnableWebhook, 0, 10);
        this.tableLayoutMain.Controls.Add(this.labelWebhookPlatform, 0, 11);
        this.tableLayoutMain.Controls.Add(this.comboBoxWebhookPlatform, 0, 12);
        this.tableLayoutMain.Controls.Add(this.labelWebhookUrl, 0, 13);
        this.tableLayoutMain.Controls.Add(this.textBoxWebhookUrl, 0, 14);
        this.tableLayoutMain.Controls.Add(this.labelTelegramToken, 0, 15);
        this.tableLayoutMain.Controls.Add(this.textBoxTelegramToken, 0, 16);
        this.tableLayoutMain.Controls.Add(this.labelTelegramChatId, 0, 17);
        this.tableLayoutMain.Controls.Add(this.textBoxTelegramChatId, 0, 18);
        this.tableLayoutMain.Controls.Add(this.checkBoxWebhookSoftLock, 0, 19);
        this.tableLayoutMain.Controls.Add(this.checkBoxWebhookHardLocks, 0, 20);
        this.tableLayoutMain.Controls.Add(this.checkBoxWebhookOnUnlock, 0, 21);
        this.tableLayoutMain.Controls.Add(this.buttonTestWebhook, 0, 22);
        this.tableLayoutMain.Controls.Add(this.smartLabelSyslogHeader, 0, 23);
        this.tableLayoutMain.Controls.Add(this.checkBoxEnableSyslog, 0, 24);
        this.tableLayoutMain.Controls.Add(this.labelSyslogHost, 0, 25);
        this.tableLayoutMain.Controls.Add(this.textBoxSyslogHost, 0, 26);
        this.tableLayoutMain.Controls.Add(this.labelSyslogPort, 0, 27);
        this.tableLayoutMain.Controls.Add(this.numSyslogPort, 0, 28);
        this.tableLayoutMain.Controls.Add(this.labelSyslogProtocol, 0, 29);
        this.tableLayoutMain.Controls.Add(this.comboBoxSyslogProtocol, 0, 30);
        this.tableLayoutMain.Controls.Add(this.labelSyslogFormat, 0, 31);
        this.tableLayoutMain.Controls.Add(this.comboBoxSyslogFormat, 0, 32);
        this.tableLayoutMain.Controls.Add(this.checkBoxSyslogSoftLock, 0, 33);
        this.tableLayoutMain.Controls.Add(this.checkBoxSyslogHardLocks, 0, 34);
        this.tableLayoutMain.Controls.Add(this.checkBoxSyslogOnUnlock, 0, 35);
        this.tableLayoutMain.Controls.Add(this.buttonTestSyslog, 0, 36);
        this.tableLayoutMain.Controls.Add(this.smartLabelMetricsHeader, 0, 37);
        this.tableLayoutMain.Controls.Add(this.checkBoxEnableMetrics, 0, 38);
        this.tableLayoutMain.Controls.Add(this.labelMetricsListenIp, 0, 39);
        this.tableLayoutMain.Controls.Add(this.textBoxMetricsListenIp, 0, 40);
        this.tableLayoutMain.Controls.Add(this.labelMetricsPort, 0, 41);
        this.tableLayoutMain.Controls.Add(this.numMetricsPort, 0, 42);
        this.tableLayoutMain.Controls.Add(this.labelMetricsAllowed, 0, 43);
        this.tableLayoutMain.Controls.Add(this.textBoxMetricsAllowedNetworks, 0, 44);
        this.tableLayoutMain.Controls.Add(this.buttonSave, 0, 45);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 46;
        for (int i = 0; i < 46; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // headerPanel
        //
        this.headerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.headerPanel.Controls.Add(this.smartLabel5);
        this.headerPanel.Height = 34;
        this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.headerPanel.Name = "headerPanel";
        //
        // smartLabel5
        //
        this.smartLabel5.AutoSize = true;
        this.smartLabel5.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabel5.ForeColor = PanelNotificationSettings.AccentColor;
        this.smartLabel5.Location = new System.Drawing.Point(0, 6);
        this.smartLabel5.Margin = new System.Windows.Forms.Padding(0);
        this.smartLabel5.Name = "smartLabel5";
        this.smartLabel5.Selected = false;
        this.smartLabel5.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel5.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("E-mail notification settings");
        //
        // smartLabel1
        //
        this.smartLabel1.AutoSize = true;
        this.smartLabel1.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel1.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.smartLabel1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel1.Name = "smartLabel1";
        this.smartLabel1.Selected = false;
        this.smartLabel1.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel1.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Basic notification");
        //
        // checkBoxSoftLock
        //
        this.checkBoxSoftLock.AutoSize = true;
        this.checkBoxSoftLock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxSoftLock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxSoftLock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxSoftLock.Name = "checkBoxSoftLock";
        this.checkBoxSoftLock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On soft lock events");
        //
        // checkBoxHardLocks
        //
        this.checkBoxHardLocks.AutoSize = true;
        this.checkBoxHardLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxHardLocks.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxHardLocks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxHardLocks.Name = "checkBoxHardLocks";
        this.checkBoxHardLocks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On hard lock events");
        //
        // checkBoxOnUnlock
        //
        this.checkBoxOnUnlock.AutoSize = true;
        this.checkBoxOnUnlock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxOnUnlock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxOnUnlock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.checkBoxOnUnlock.Name = "checkBoxOnUnlock";
        this.checkBoxOnUnlock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On unlock events");
        //
        // smartLabelSummary
        //
        this.smartLabelSummary.AutoSize = true;
        this.smartLabelSummary.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabelSummary.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.smartLabelSummary.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabelSummary.Name = "smartLabelSummary";
        this.smartLabelSummary.Selected = false;
        this.smartLabelSummary.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelSummary.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Reports");
        //
        // checkBoxDailySummary
        //
        this.checkBoxDailySummary.AutoSize = true;
        this.checkBoxDailySummary.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxDailySummary.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxDailySummary.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxDailySummary.Name = "checkBoxDailySummary";
        this.checkBoxDailySummary.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Daily report");
        //
        // checkBoxWeeklyReport
        //
        this.checkBoxWeeklyReport.AutoSize = true;
        this.checkBoxWeeklyReport.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxWeeklyReport.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxWeeklyReport.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxWeeklyReport.Name = "checkBoxWeeklyReport";
        this.checkBoxWeeklyReport.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Weekly report");
        //
        // checkBoxMonthlyReport
        //
        this.checkBoxMonthlyReport.AutoSize = true;
        this.checkBoxMonthlyReport.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxMonthlyReport.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxMonthlyReport.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.checkBoxMonthlyReport.Name = "checkBoxMonthlyReport";
        this.checkBoxMonthlyReport.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Monthly report");
        //
        // smartLabelWebhookHeader
        //
        this.smartLabelWebhookHeader.AutoSize = true;
        this.smartLabelWebhookHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabelWebhookHeader.ForeColor = PanelNotificationSettings.AccentColor;
        this.smartLabelWebhookHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.smartLabelWebhookHeader.Name = "smartLabelWebhookHeader";
        this.smartLabelWebhookHeader.Selected = false;
        this.smartLabelWebhookHeader.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelWebhookHeader.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook notifications (Teams / Slack / Discord / Telegram)");
        //
        // checkBoxEnableWebhook
        //
        this.checkBoxEnableWebhook.AutoSize = true;
        this.checkBoxEnableWebhook.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxEnableWebhook.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxEnableWebhook.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxEnableWebhook.Name = "checkBoxEnableWebhook";
        this.checkBoxEnableWebhook.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable Webhook alerts");
        //
        // labelWebhookPlatform
        //
        this.labelWebhookPlatform.AutoSize = true;
        this.labelWebhookPlatform.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelWebhookPlatform.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelWebhookPlatform.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelWebhookPlatform.Name = "labelWebhookPlatform";
        this.labelWebhookPlatform.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook platform");
        //
        // comboBoxWebhookPlatform
        //
        this.comboBoxWebhookPlatform.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.comboBoxWebhookPlatform.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxWebhookPlatform.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboBoxWebhookPlatform.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.comboBoxWebhookPlatform.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.comboBoxWebhookPlatform.Name = "comboBoxWebhookPlatform";
        this.comboBoxWebhookPlatform.Size = new System.Drawing.Size(380, 23);
        //
        // labelWebhookUrl
        //
        this.labelWebhookUrl.AutoSize = true;
        this.labelWebhookUrl.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelWebhookUrl.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelWebhookUrl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelWebhookUrl.Name = "labelWebhookUrl";
        this.labelWebhookUrl.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook URL / Endpoint");
        //
        // textBoxWebhookUrl
        //
        this.textBoxWebhookUrl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxWebhookUrl.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxWebhookUrl.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxWebhookUrl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.textBoxWebhookUrl.Name = "textBoxWebhookUrl";
        //
        // labelTelegramToken
        //
        this.labelTelegramToken.AutoSize = true;
        this.labelTelegramToken.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelTelegramToken.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelTelegramToken.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelTelegramToken.Name = "labelTelegramToken";
        this.labelTelegramToken.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Telegram bot token");
        this.labelTelegramToken.Visible = false;
        //
        // textBoxTelegramToken
        //
        this.textBoxTelegramToken.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxTelegramToken.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxTelegramToken.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxTelegramToken.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.textBoxTelegramToken.Name = "textBoxTelegramToken";
        this.textBoxTelegramToken.Visible = false;
        //
        // labelTelegramChatId
        //
        this.labelTelegramChatId.AutoSize = true;
        this.labelTelegramChatId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelTelegramChatId.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelTelegramChatId.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelTelegramChatId.Name = "labelTelegramChatId";
        this.labelTelegramChatId.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Telegram chat ID");
        this.labelTelegramChatId.Visible = false;
        //
        // textBoxTelegramChatId
        //
        this.textBoxTelegramChatId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxTelegramChatId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxTelegramChatId.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxTelegramChatId.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.textBoxTelegramChatId.Name = "textBoxTelegramChatId";
        this.textBoxTelegramChatId.Visible = false;
        //
        // checkBoxWebhookSoftLock
        //
        this.checkBoxWebhookSoftLock.AutoSize = true;
        this.checkBoxWebhookSoftLock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxWebhookSoftLock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxWebhookSoftLock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxWebhookSoftLock.Name = "checkBoxWebhookSoftLock";
        this.checkBoxWebhookSoftLock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On soft lock events");
        //
        // checkBoxWebhookHardLocks
        //
        this.checkBoxWebhookHardLocks.AutoSize = true;
        this.checkBoxWebhookHardLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxWebhookHardLocks.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxWebhookHardLocks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxWebhookHardLocks.Name = "checkBoxWebhookHardLocks";
        this.checkBoxWebhookHardLocks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On hard lock events");
        //
        // checkBoxWebhookOnUnlock
        //
        this.checkBoxWebhookOnUnlock.AutoSize = true;
        this.checkBoxWebhookOnUnlock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxWebhookOnUnlock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxWebhookOnUnlock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.checkBoxWebhookOnUnlock.Name = "checkBoxWebhookOnUnlock";
        this.checkBoxWebhookOnUnlock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On unlock events");
        //
        // buttonTestWebhook
        //
        this.buttonTestWebhook.AutoSize = true;
        this.buttonTestWebhook.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.buttonTestWebhook.BackColor = System.Drawing.Color.White;
        this.buttonTestWebhook.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonTestWebhook.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.buttonTestWebhook.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.buttonTestWebhook.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.buttonTestWebhook.MinimumSize = new System.Drawing.Size(140, 28);
        this.buttonTestWebhook.Name = "buttonTestWebhook";
        this.buttonTestWebhook.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Test Webhook");
        this.buttonTestWebhook.UseVisualStyleBackColor = false;
        //
        // smartLabelSyslogHeader
        //
        this.smartLabelSyslogHeader.AutoSize = true;
        this.smartLabelSyslogHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabelSyslogHeader.ForeColor = PanelNotificationSettings.AccentColor;
        this.smartLabelSyslogHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.smartLabelSyslogHeader.Name = "smartLabelSyslogHeader";
        this.smartLabelSyslogHeader.Selected = false;
        this.smartLabelSyslogHeader.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelSyslogHeader.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Syslog & SIEM integration (RFC 5424 / RFC 3164 / CEF)");
        //
        // checkBoxEnableSyslog
        //
        this.checkBoxEnableSyslog.AutoSize = true;
        this.checkBoxEnableSyslog.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxEnableSyslog.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxEnableSyslog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxEnableSyslog.Name = "checkBoxEnableSyslog";
        this.checkBoxEnableSyslog.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable Syslog alerts");
        //
        // labelSyslogHost
        //
        this.labelSyslogHost.AutoSize = true;
        this.labelSyslogHost.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelSyslogHost.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelSyslogHost.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelSyslogHost.Name = "labelSyslogHost";
        this.labelSyslogHost.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Syslog server host");
        //
        // textBoxSyslogHost
        //
        this.textBoxSyslogHost.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxSyslogHost.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSyslogHost.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxSyslogHost.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.textBoxSyslogHost.Name = "textBoxSyslogHost";
        //
        // labelSyslogPort
        //
        this.labelSyslogPort.AutoSize = true;
        this.labelSyslogPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelSyslogPort.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelSyslogPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelSyslogPort.Name = "labelSyslogPort";
        this.labelSyslogPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Syslog server port");
        //
        // numSyslogPort
        //
        this.numSyslogPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numSyslogPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numSyslogPort.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.numSyslogPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numSyslogPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        this.numSyslogPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numSyslogPort.Name = "numSyslogPort";
        this.numSyslogPort.Size = new System.Drawing.Size(150, 23);
        this.numSyslogPort.Value = new decimal(new int[] { 514, 0, 0, 0 });
        //
        // labelSyslogProtocol
        //
        this.labelSyslogProtocol.AutoSize = true;
        this.labelSyslogProtocol.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelSyslogProtocol.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelSyslogProtocol.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelSyslogProtocol.Name = "labelSyslogProtocol";
        this.labelSyslogProtocol.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Syslog protocol");
        //
        // comboBoxSyslogProtocol
        //
        this.comboBoxSyslogProtocol.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.comboBoxSyslogProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxSyslogProtocol.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboBoxSyslogProtocol.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.comboBoxSyslogProtocol.Items.AddRange(new object[] { "UDP", "TCP", "TLS" });
        this.comboBoxSyslogProtocol.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.comboBoxSyslogProtocol.Name = "comboBoxSyslogProtocol";
        this.comboBoxSyslogProtocol.Size = new System.Drawing.Size(160, 23);
        //
        // labelSyslogFormat
        //
        this.labelSyslogFormat.AutoSize = true;
        this.labelSyslogFormat.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelSyslogFormat.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelSyslogFormat.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelSyslogFormat.Name = "labelSyslogFormat";
        this.labelSyslogFormat.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Syslog format");
        //
        // comboBoxSyslogFormat
        //
        this.comboBoxSyslogFormat.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.comboBoxSyslogFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxSyslogFormat.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboBoxSyslogFormat.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.comboBoxSyslogFormat.Items.AddRange(new object[] { "RFC 5424", "RFC 3164", "CEF" });
        this.comboBoxSyslogFormat.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.comboBoxSyslogFormat.Name = "comboBoxSyslogFormat";
        this.comboBoxSyslogFormat.Size = new System.Drawing.Size(175, 23);
        //
        // checkBoxSyslogSoftLock
        //
        this.checkBoxSyslogSoftLock.AutoSize = true;
        this.checkBoxSyslogSoftLock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxSyslogSoftLock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxSyslogSoftLock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxSyslogSoftLock.Name = "checkBoxSyslogSoftLock";
        this.checkBoxSyslogSoftLock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On soft lock events");
        //
        // checkBoxSyslogHardLocks
        //
        this.checkBoxSyslogHardLocks.AutoSize = true;
        this.checkBoxSyslogHardLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxSyslogHardLocks.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxSyslogHardLocks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.checkBoxSyslogHardLocks.Name = "checkBoxSyslogHardLocks";
        this.checkBoxSyslogHardLocks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On hard lock events");
        //
        // checkBoxSyslogOnUnlock
        //
        this.checkBoxSyslogOnUnlock.AutoSize = true;
        this.checkBoxSyslogOnUnlock.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxSyslogOnUnlock.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxSyslogOnUnlock.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.checkBoxSyslogOnUnlock.Name = "checkBoxSyslogOnUnlock";
        this.checkBoxSyslogOnUnlock.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("On unlock events");
        //
        // buttonTestSyslog
        //
        this.buttonTestSyslog.AutoSize = true;
        this.buttonTestSyslog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.buttonTestSyslog.BackColor = System.Drawing.Color.White;
        this.buttonTestSyslog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonTestSyslog.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.buttonTestSyslog.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.buttonTestSyslog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.buttonTestSyslog.MinimumSize = new System.Drawing.Size(140, 28);
        this.buttonTestSyslog.Name = "buttonTestSyslog";
        this.buttonTestSyslog.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Test Syslog");
        this.buttonTestSyslog.UseVisualStyleBackColor = false;
        //
        // smartLabelMetricsHeader
        //
        this.smartLabelMetricsHeader.AutoSize = true;
        this.smartLabelMetricsHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabelMetricsHeader.ForeColor = PanelNotificationSettings.AccentColor;
        this.smartLabelMetricsHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.smartLabelMetricsHeader.Name = "smartLabelMetricsHeader";
        this.smartLabelMetricsHeader.Selected = false;
        this.smartLabelMetricsHeader.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelMetricsHeader.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Prometheus / OpenMetrics observability");
        //
        // checkBoxEnableMetrics
        //
        this.checkBoxEnableMetrics.AutoSize = true;
        this.checkBoxEnableMetrics.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxEnableMetrics.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.checkBoxEnableMetrics.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxEnableMetrics.Name = "checkBoxEnableMetrics";
        this.checkBoxEnableMetrics.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable Prometheus metrics endpoint");
        //
        // labelMetricsListenIp
        //
        this.labelMetricsListenIp.AutoSize = true;
        this.labelMetricsListenIp.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelMetricsListenIp.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelMetricsListenIp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelMetricsListenIp.Name = "labelMetricsListenIp";
        this.labelMetricsListenIp.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Metrics listen IP address (e.g. 0.0.0.0 or 127.0.0.1)");
        //
        // textBoxMetricsListenIp
        //
        this.textBoxMetricsListenIp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxMetricsListenIp.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxMetricsListenIp.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxMetricsListenIp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.textBoxMetricsListenIp.Name = "textBoxMetricsListenIp";
        //
        // labelMetricsPort
        //
        this.labelMetricsPort.AutoSize = true;
        this.labelMetricsPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelMetricsPort.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelMetricsPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelMetricsPort.Name = "labelMetricsPort";
        this.labelMetricsPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Metrics port");
        //
        // numMetricsPort
        //
        this.numMetricsPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numMetricsPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numMetricsPort.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.numMetricsPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numMetricsPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        this.numMetricsPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numMetricsPort.Name = "numMetricsPort";
        this.numMetricsPort.Size = new System.Drawing.Size(150, 23);
        this.numMetricsPort.Value = new decimal(new int[] { 9100, 0, 0, 0 });
        //
        // labelMetricsAllowed
        //
        this.labelMetricsAllowed.AutoSize = true;
        this.labelMetricsAllowed.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelMetricsAllowed.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.labelMetricsAllowed.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelMetricsAllowed.Name = "labelMetricsAllowed";
        this.labelMetricsAllowed.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Allowed monitoring networks CIDR (e.g. 10.0.0.0/8, 192.168.1.0/24)");
        //
        // textBoxMetricsAllowedNetworks
        //
        this.textBoxMetricsAllowedNetworks.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxMetricsAllowedNetworks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxMetricsAllowedNetworks.ForeColor = PanelNotificationSettings.BodyTextColor;
        this.textBoxMetricsAllowedNetworks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.textBoxMetricsAllowedNetworks.Name = "textBoxMetricsAllowedNetworks";
        //
        // buttonSave
        //
        this.buttonSave.BackColor = PanelNotificationSettings.AccentColor;
        this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.buttonSave.ForeColor = System.Drawing.Color.White;
        this.buttonSave.Margin = new System.Windows.Forms.Padding(0);
        this.buttonSave.Name = "buttonSave";
        this.buttonSave.Size = new System.Drawing.Size(120, 32);
        this.buttonSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
        //
        // PanelNotificationSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Name = "PanelNotificationSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numSyslogPort)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numMetricsPort)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Panel headerPanel;
    private SmartLabel smartLabel5;
    private SmartLabel smartLabel1;
    private System.Windows.Forms.CheckBox checkBoxSoftLock;
    private System.Windows.Forms.CheckBox checkBoxHardLocks;
    private System.Windows.Forms.CheckBox checkBoxOnUnlock;
    private SmartLabel smartLabelSummary;
    private System.Windows.Forms.CheckBox checkBoxDailySummary;
    private System.Windows.Forms.CheckBox checkBoxWeeklyReport;
    private System.Windows.Forms.CheckBox checkBoxMonthlyReport;
    private SmartLabel smartLabelWebhookHeader;
    private System.Windows.Forms.CheckBox checkBoxEnableWebhook;
    private System.Windows.Forms.Label labelWebhookPlatform;
    private System.Windows.Forms.ComboBox comboBoxWebhookPlatform;
    private System.Windows.Forms.Label labelWebhookUrl;
    private System.Windows.Forms.TextBox textBoxWebhookUrl;
    private System.Windows.Forms.Label labelTelegramToken;
    private System.Windows.Forms.TextBox textBoxTelegramToken;
    private System.Windows.Forms.Label labelTelegramChatId;
    private System.Windows.Forms.TextBox textBoxTelegramChatId;
    private System.Windows.Forms.CheckBox checkBoxWebhookSoftLock;
    private System.Windows.Forms.CheckBox checkBoxWebhookHardLocks;
    private System.Windows.Forms.CheckBox checkBoxWebhookOnUnlock;
    private System.Windows.Forms.Button buttonTestWebhook;
    private SmartLabel smartLabelSyslogHeader;
    private System.Windows.Forms.CheckBox checkBoxEnableSyslog;
    private System.Windows.Forms.Label labelSyslogHost;
    private System.Windows.Forms.TextBox textBoxSyslogHost;
    private System.Windows.Forms.Label labelSyslogPort;
    private System.Windows.Forms.NumericUpDown numSyslogPort;
    private System.Windows.Forms.Label labelSyslogProtocol;
    private System.Windows.Forms.ComboBox comboBoxSyslogProtocol;
    private System.Windows.Forms.Label labelSyslogFormat;
    private System.Windows.Forms.ComboBox comboBoxSyslogFormat;
    private System.Windows.Forms.CheckBox checkBoxSyslogSoftLock;
    private System.Windows.Forms.CheckBox checkBoxSyslogHardLocks;
    private System.Windows.Forms.CheckBox checkBoxSyslogOnUnlock;
    private System.Windows.Forms.Button buttonTestSyslog;
    private SmartLabel smartLabelMetricsHeader;
    private System.Windows.Forms.CheckBox checkBoxEnableMetrics;
    private System.Windows.Forms.Label labelMetricsListenIp;
    private System.Windows.Forms.TextBox textBoxMetricsListenIp;
    private System.Windows.Forms.Label labelMetricsPort;
    private System.Windows.Forms.NumericUpDown numMetricsPort;
    private System.Windows.Forms.Label labelMetricsAllowed;
    private System.Windows.Forms.TextBox textBoxMetricsAllowedNetworks;
    private System.Windows.Forms.Button buttonSave;
}
