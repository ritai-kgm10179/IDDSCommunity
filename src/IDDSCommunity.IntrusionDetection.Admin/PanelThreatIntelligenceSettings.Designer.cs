namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelThreatIntelligenceSettings
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
        this.pageTitle = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.lblSectionCluster = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.lblRole = new System.Windows.Forms.Label();
        this.comboClusterRole = new System.Windows.Forms.ComboBox();
        this.lblEndpoint = new System.Windows.Forms.Label();
        this.txtHubEndpoint = new System.Windows.Forms.TextBox();
        this.lblApiKey = new System.Windows.Forms.Label();
        this.txtHubApiKey = new System.Windows.Forms.TextBox();
        this.lblPort = new System.Windows.Forms.Label();
        this.numHubPort = new System.Windows.Forms.NumericUpDown();
        this.chkThreatHubReverseProxy = new System.Windows.Forms.CheckBox();
        this.chkThreatHubLoopbackOnly = new System.Windows.Forms.CheckBox();
        this.lblSync = new System.Windows.Forms.Label();
        this.numSyncInterval = new System.Windows.Forms.NumericUpDown();
        this.lblSectionFeeds = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.chkEnableFeeds = new System.Windows.Forms.CheckBox();
        this.lblFeedInterval = new System.Windows.Forms.Label();
        this.numFeedInterval = new System.Windows.Forms.NumericUpDown();
        this.lblIpsumLevel = new System.Windows.Forms.Label();
        this.numIpsumLevel = new System.Windows.Forms.NumericUpDown();
        this.lblFeedTtl = new System.Windows.Forms.Label();
        this.numFeedTtlDays = new System.Windows.Forms.NumericUpDown();
        this.lblAbuseMin = new System.Windows.Forms.Label();
        this.numAbuseMinConfidence = new System.Windows.Forms.NumericUpDown();
        this.lblAbuseKey = new System.Windows.Forms.Label();
        this.txtAbuseApiKey = new System.Windows.Forms.TextBox();
        this.lblCustomUrls = new System.Windows.Forms.Label();
        this.txtCustomUrls = new System.Windows.Forms.TextBox();
        this.lblSectionBogon = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.chkEnableDynamicBogon = new System.Windows.Forms.CheckBox();
        this.lblProbationDays = new System.Windows.Forms.Label();
        this.numProbationDays = new System.Windows.Forms.NumericUpDown();
        this.lblBogonV4 = new System.Windows.Forms.Label();
        this.txtBogonIpv4Url = new System.Windows.Forms.TextBox();
        this.lblBogonV6 = new System.Windows.Forms.Label();
        this.txtBogonIpv6Url = new System.Windows.Forms.TextBox();
        this.lblSectionGeo = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.chkEnableGeoBlocking = new System.Windows.Forms.CheckBox();
        this.lblBlockedCountries = new System.Windows.Forms.Label();
        this.txtBlockedCountries = new System.Windows.Forms.TextBox();
        this.chkEnableGeoIpAutoUpdate = new System.Windows.Forms.CheckBox();
        this.lblGeoV4 = new System.Windows.Forms.Label();
        this.txtGeoIpDatabaseIpv4Url = new System.Windows.Forms.TextBox();
        this.lblGeoV6 = new System.Windows.Forms.Label();
        this.txtGeoIpDatabaseIpv6Url = new System.Windows.Forms.TextBox();
        this.lblGeoLocal = new System.Windows.Forms.Label();
        this.flowGeoLocal = new System.Windows.Forms.FlowLayoutPanel();
        this.txtGeoIpLocalFilePath = new System.Windows.Forms.TextBox();
        this.btnBrowseGeoIpFile = new System.Windows.Forms.Button();
        this.lblGeoDays = new System.Windows.Forms.Label();
        this.numGeoIpUpdateDays = new System.Windows.Forms.NumericUpDown();
        this.btnUpdateGeoIpNow = new System.Windows.Forms.Button();
        this.lblGeoIpStatus = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numHubPort)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numFeedInterval)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIpsumLevel)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numFeedTtlDays)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numAbuseMinConfidence)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numProbationDays)).BeginInit();
        this.flowGeoLocal.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numGeoIpUpdateDays)).BeginInit();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.headerPanel, 0, 0);
        this.tableLayoutMain.Controls.Add(this.lblSectionCluster, 0, 1);
        this.tableLayoutMain.Controls.Add(this.lblRole, 0, 2);
        this.tableLayoutMain.Controls.Add(this.comboClusterRole, 0, 3);
        this.tableLayoutMain.Controls.Add(this.lblEndpoint, 0, 4);
        this.tableLayoutMain.Controls.Add(this.txtHubEndpoint, 0, 5);
        this.tableLayoutMain.Controls.Add(this.lblApiKey, 0, 6);
        this.tableLayoutMain.Controls.Add(this.txtHubApiKey, 0, 7);
        this.tableLayoutMain.Controls.Add(this.lblPort, 0, 8);
        this.tableLayoutMain.Controls.Add(this.numHubPort, 0, 9);
        this.tableLayoutMain.Controls.Add(this.chkThreatHubReverseProxy, 0, 10);
        this.tableLayoutMain.Controls.Add(this.chkThreatHubLoopbackOnly, 0, 11);
        this.tableLayoutMain.Controls.Add(this.lblSync, 0, 12);
        this.tableLayoutMain.Controls.Add(this.numSyncInterval, 0, 13);
        this.tableLayoutMain.Controls.Add(this.lblSectionFeeds, 0, 14);
        this.tableLayoutMain.Controls.Add(this.chkEnableFeeds, 0, 15);
        this.tableLayoutMain.Controls.Add(this.lblFeedInterval, 0, 16);
        this.tableLayoutMain.Controls.Add(this.numFeedInterval, 0, 17);
        this.tableLayoutMain.Controls.Add(this.lblIpsumLevel, 0, 18);
        this.tableLayoutMain.Controls.Add(this.numIpsumLevel, 0, 19);
        this.tableLayoutMain.Controls.Add(this.lblFeedTtl, 0, 20);
        this.tableLayoutMain.Controls.Add(this.numFeedTtlDays, 0, 21);
        this.tableLayoutMain.Controls.Add(this.lblAbuseMin, 0, 22);
        this.tableLayoutMain.Controls.Add(this.numAbuseMinConfidence, 0, 23);
        this.tableLayoutMain.Controls.Add(this.lblAbuseKey, 0, 24);
        this.tableLayoutMain.Controls.Add(this.txtAbuseApiKey, 0, 25);
        this.tableLayoutMain.Controls.Add(this.lblCustomUrls, 0, 26);
        this.tableLayoutMain.Controls.Add(this.txtCustomUrls, 0, 27);
        this.tableLayoutMain.Controls.Add(this.lblSectionBogon, 0, 28);
        this.tableLayoutMain.Controls.Add(this.chkEnableDynamicBogon, 0, 29);
        this.tableLayoutMain.Controls.Add(this.lblProbationDays, 0, 30);
        this.tableLayoutMain.Controls.Add(this.numProbationDays, 0, 31);
        this.tableLayoutMain.Controls.Add(this.lblBogonV4, 0, 32);
        this.tableLayoutMain.Controls.Add(this.txtBogonIpv4Url, 0, 33);
        this.tableLayoutMain.Controls.Add(this.lblBogonV6, 0, 34);
        this.tableLayoutMain.Controls.Add(this.txtBogonIpv6Url, 0, 35);
        this.tableLayoutMain.Controls.Add(this.lblSectionGeo, 0, 36);
        this.tableLayoutMain.Controls.Add(this.chkEnableGeoBlocking, 0, 37);
        this.tableLayoutMain.Controls.Add(this.lblBlockedCountries, 0, 38);
        this.tableLayoutMain.Controls.Add(this.txtBlockedCountries, 0, 39);
        this.tableLayoutMain.Controls.Add(this.chkEnableGeoIpAutoUpdate, 0, 40);
        this.tableLayoutMain.Controls.Add(this.lblGeoV4, 0, 41);
        this.tableLayoutMain.Controls.Add(this.txtGeoIpDatabaseIpv4Url, 0, 42);
        this.tableLayoutMain.Controls.Add(this.lblGeoV6, 0, 43);
        this.tableLayoutMain.Controls.Add(this.txtGeoIpDatabaseIpv6Url, 0, 44);
        this.tableLayoutMain.Controls.Add(this.lblGeoLocal, 0, 45);
        this.tableLayoutMain.Controls.Add(this.flowGeoLocal, 0, 46);
        this.tableLayoutMain.Controls.Add(this.lblGeoDays, 0, 47);
        this.tableLayoutMain.Controls.Add(this.numGeoIpUpdateDays, 0, 48);
        this.tableLayoutMain.Controls.Add(this.btnUpdateGeoIpNow, 0, 49);
        this.tableLayoutMain.Controls.Add(this.lblGeoIpStatus, 0, 50);
        this.tableLayoutMain.Controls.Add(this.btnSave, 0, 51);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 52;
        for (int i = 0; i < 52; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // headerPanel
        //
        this.headerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.headerPanel.Controls.Add(this.pageTitle);
        this.headerPanel.Height = 34;
        this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.headerPanel.Name = "headerPanel";
        //
        // pageTitle
        //
        this.pageTitle.AutoSize = true;
        this.pageTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.pageTitle.ForeColor = PanelThreatIntelligenceSettings.AccentColor;
        this.pageTitle.Location = new System.Drawing.Point(0, 4);
        this.pageTitle.Margin = new System.Windows.Forms.Padding(0);
        this.pageTitle.Name = "pageTitle";
        this.pageTitle.Selected = false;
        this.pageTitle.SelectedColor = System.Drawing.Color.Empty;
        this.pageTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat intelligence and cluster");
        //
        // lblSectionCluster
        //
        this.lblSectionCluster.AutoSize = true;
        this.lblSectionCluster.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblSectionCluster.ForeColor = PanelThreatIntelligenceSettings.AccentColor;
        this.lblSectionCluster.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.lblSectionCluster.Name = "lblSectionCluster";
        this.lblSectionCluster.Selected = false;
        this.lblSectionCluster.SelectedColor = System.Drawing.Color.Empty;
        this.lblSectionCluster.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cluster topology & Threat Hub");
        //
        // lblRole
        //
        this.lblRole.AutoSize = true;
        this.lblRole.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblRole.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblRole.Name = "lblRole";
        this.lblRole.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cluster node role");
        //
        // comboClusterRole
        //
        this.comboClusterRole.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.comboClusterRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboClusterRole.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboClusterRole.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.comboClusterRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.comboClusterRole.Name = "comboClusterRole";
        this.comboClusterRole.Size = new System.Drawing.Size(380, 23);
        //
        // lblEndpoint
        //
        this.lblEndpoint.AutoSize = true;
        this.lblEndpoint.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblEndpoint.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblEndpoint.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblEndpoint.Name = "lblEndpoint";
        this.lblEndpoint.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat Hub endpoint URL");
        //
        // txtHubEndpoint
        //
        this.txtHubEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtHubEndpoint.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtHubEndpoint.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtHubEndpoint.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtHubEndpoint.Name = "txtHubEndpoint";
        //
        // lblApiKey
        //
        this.lblApiKey.AutoSize = true;
        this.lblApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblApiKey.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblApiKey.Name = "lblApiKey";
        this.lblApiKey.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cluster API key");
        //
        // txtHubApiKey
        //
        this.txtHubApiKey.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtHubApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtHubApiKey.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtHubApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtHubApiKey.Name = "txtHubApiKey";
        //
        // lblPort
        //
        this.lblPort.AutoSize = true;
        this.lblPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblPort.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblPort.Name = "lblPort";
        this.lblPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat Hub port");
        //
        // numHubPort
        //
        this.numHubPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numHubPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numHubPort.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numHubPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numHubPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        this.numHubPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numHubPort.Name = "numHubPort";
        this.numHubPort.Size = new System.Drawing.Size(180, 23);
        this.numHubPort.Value = new decimal(new int[] { 8443, 0, 0, 0 });
        //
        // chkThreatHubReverseProxy
        //
        this.chkThreatHubReverseProxy.AutoSize = true;
        this.chkThreatHubReverseProxy.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkThreatHubReverseProxy.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkThreatHubReverseProxy.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.chkThreatHubReverseProxy.Name = "chkThreatHubReverseProxy";
        this.chkThreatHubReverseProxy.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat Hub TLS is terminated by a reverse proxy");
        //
        // chkThreatHubLoopbackOnly
        //
        this.chkThreatHubLoopbackOnly.AutoSize = true;
        this.chkThreatHubLoopbackOnly.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkThreatHubLoopbackOnly.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkThreatHubLoopbackOnly.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.chkThreatHubLoopbackOnly.Name = "chkThreatHubLoopbackOnly";
        this.chkThreatHubLoopbackOnly.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Restrict reverse proxy upstream to this computer (127.0.0.1)");
        //
        // lblSync
        //
        this.lblSync.AutoSize = true;
        this.lblSync.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSync.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblSync.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSync.Name = "lblSync";
        this.lblSync.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cluster sync interval (seconds)");
        //
        // numSyncInterval
        //
        this.numSyncInterval.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numSyncInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numSyncInterval.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numSyncInterval.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.numSyncInterval.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        this.numSyncInterval.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
        this.numSyncInterval.Name = "numSyncInterval";
        this.numSyncInterval.Size = new System.Drawing.Size(180, 23);
        this.numSyncInterval.Value = new decimal(new int[] { 60, 0, 0, 0 });
        //
        // lblSectionFeeds
        //
        this.lblSectionFeeds.AutoSize = true;
        this.lblSectionFeeds.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblSectionFeeds.ForeColor = PanelThreatIntelligenceSettings.AccentColor;
        this.lblSectionFeeds.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.lblSectionFeeds.Name = "lblSectionFeeds";
        this.lblSectionFeeds.Selected = false;
        this.lblSectionFeeds.SelectedColor = System.Drawing.Color.Empty;
        this.lblSectionFeeds.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("External threat feeds subscription");
        //
        // chkEnableFeeds
        //
        this.chkEnableFeeds.AutoSize = true;
        this.chkEnableFeeds.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableFeeds.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkEnableFeeds.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableFeeds.Name = "chkEnableFeeds";
        this.chkEnableFeeds.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable automated threat feed subscription");
        //
        // lblFeedInterval
        //
        this.lblFeedInterval.AutoSize = true;
        this.lblFeedInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblFeedInterval.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblFeedInterval.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblFeedInterval.Name = "lblFeedInterval";
        this.lblFeedInterval.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Feed update interval (hours)");
        //
        // numFeedInterval
        //
        this.numFeedInterval.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numFeedInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numFeedInterval.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numFeedInterval.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numFeedInterval.Maximum = new decimal(new int[] { 168, 0, 0, 0 });
        this.numFeedInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numFeedInterval.Name = "numFeedInterval";
        this.numFeedInterval.Size = new System.Drawing.Size(180, 23);
        this.numFeedInterval.Value = new decimal(new int[] { 24, 0, 0, 0 });
        //
        // lblIpsumLevel
        //
        this.lblIpsumLevel.AutoSize = true;
        this.lblIpsumLevel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblIpsumLevel.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblIpsumLevel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblIpsumLevel.Name = "lblIpsumLevel";
        this.lblIpsumLevel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IPsum minimum severity level (1-8)");
        //
        // numIpsumLevel
        //
        this.numIpsumLevel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numIpsumLevel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numIpsumLevel.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numIpsumLevel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numIpsumLevel.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        this.numIpsumLevel.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numIpsumLevel.Name = "numIpsumLevel";
        this.numIpsumLevel.Size = new System.Drawing.Size(180, 23);
        this.numIpsumLevel.Value = new decimal(new int[] { 3, 0, 0, 0 });
        //
        // lblFeedTtl
        //
        this.lblFeedTtl.AutoSize = true;
        this.lblFeedTtl.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblFeedTtl.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblFeedTtl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblFeedTtl.Name = "lblFeedTtl";
        this.lblFeedTtl.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Threat intelligence TTL (days)");
        //
        // numFeedTtlDays
        //
        this.numFeedTtlDays.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numFeedTtlDays.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numFeedTtlDays.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numFeedTtlDays.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numFeedTtlDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
        this.numFeedTtlDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numFeedTtlDays.Name = "numFeedTtlDays";
        this.numFeedTtlDays.Size = new System.Drawing.Size(180, 23);
        this.numFeedTtlDays.Value = new decimal(new int[] { 7, 0, 0, 0 });
        //
        // lblAbuseMin
        //
        this.lblAbuseMin.AutoSize = true;
        this.lblAbuseMin.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblAbuseMin.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblAbuseMin.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblAbuseMin.Name = "lblAbuseMin";
        this.lblAbuseMin.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("AbuseIPDB minimum confidence (%)");
        //
        // numAbuseMinConfidence
        //
        this.numAbuseMinConfidence.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numAbuseMinConfidence.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numAbuseMinConfidence.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numAbuseMinConfidence.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numAbuseMinConfidence.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this.numAbuseMinConfidence.Minimum = new decimal(new int[] { 25, 0, 0, 0 });
        this.numAbuseMinConfidence.Name = "numAbuseMinConfidence";
        this.numAbuseMinConfidence.Size = new System.Drawing.Size(180, 23);
        this.numAbuseMinConfidence.Value = new decimal(new int[] { 90, 0, 0, 0 });
        //
        // lblAbuseKey
        //
        this.lblAbuseKey.AutoSize = true;
        this.lblAbuseKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblAbuseKey.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblAbuseKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblAbuseKey.Name = "lblAbuseKey";
        this.lblAbuseKey.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("AbuseIPDB API key");
        //
        // txtAbuseApiKey
        //
        this.txtAbuseApiKey.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtAbuseApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtAbuseApiKey.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtAbuseApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtAbuseApiKey.Name = "txtAbuseApiKey";
        //
        // lblCustomUrls
        //
        this.lblCustomUrls.AutoSize = true;
        this.lblCustomUrls.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblCustomUrls.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblCustomUrls.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblCustomUrls.Name = "lblCustomUrls";
        this.lblCustomUrls.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Custom threat feed URLs (one per line)");
        //
        // txtCustomUrls
        //
        this.txtCustomUrls.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtCustomUrls.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtCustomUrls.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtCustomUrls.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.txtCustomUrls.Multiline = true;
        this.txtCustomUrls.Name = "txtCustomUrls";
        this.txtCustomUrls.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtCustomUrls.Size = new System.Drawing.Size(400, 48);
        //
        // lblSectionBogon
        //
        this.lblSectionBogon.AutoSize = true;
        this.lblSectionBogon.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblSectionBogon.ForeColor = PanelThreatIntelligenceSettings.AccentColor;
        this.lblSectionBogon.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.lblSectionBogon.Name = "lblSectionBogon";
        this.lblSectionBogon.Selected = false;
        this.lblSectionBogon.SelectedColor = System.Drawing.Color.Empty;
        this.lblSectionBogon.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Bogon & probation guardrails");
        //
        // chkEnableDynamicBogon
        //
        this.chkEnableDynamicBogon.AutoSize = true;
        this.chkEnableDynamicBogon.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableDynamicBogon.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkEnableDynamicBogon.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableDynamicBogon.Name = "chkEnableDynamicBogon";
        this.chkEnableDynamicBogon.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable Team Cymru Fullbogons dynamic updates");
        //
        // lblProbationDays
        //
        this.lblProbationDays.AutoSize = true;
        this.lblProbationDays.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblProbationDays.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblProbationDays.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblProbationDays.Name = "lblProbationDays";
        this.lblProbationDays.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Probation decay period (days)");
        //
        // numProbationDays
        //
        this.numProbationDays.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numProbationDays.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numProbationDays.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numProbationDays.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numProbationDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
        this.numProbationDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numProbationDays.Name = "numProbationDays";
        this.numProbationDays.Size = new System.Drawing.Size(180, 23);
        this.numProbationDays.Value = new decimal(new int[] { 90, 0, 0, 0 });
        //
        // lblBogonV4
        //
        this.lblBogonV4.AutoSize = true;
        this.lblBogonV4.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblBogonV4.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblBogonV4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblBogonV4.Name = "lblBogonV4";
        this.lblBogonV4.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Dynamic Bogon IPv4 list URL");
        //
        // txtBogonIpv4Url
        //
        this.txtBogonIpv4Url.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtBogonIpv4Url.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtBogonIpv4Url.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtBogonIpv4Url.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtBogonIpv4Url.Name = "txtBogonIpv4Url";
        //
        // lblBogonV6
        //
        this.lblBogonV6.AutoSize = true;
        this.lblBogonV6.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblBogonV6.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblBogonV6.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblBogonV6.Name = "lblBogonV6";
        this.lblBogonV6.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Dynamic Bogon IPv6 list URL");
        //
        // txtBogonIpv6Url
        //
        this.txtBogonIpv6Url.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtBogonIpv6Url.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtBogonIpv6Url.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtBogonIpv6Url.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.txtBogonIpv6Url.Name = "txtBogonIpv6Url";
        //
        // lblSectionGeo
        //
        this.lblSectionGeo.AutoSize = true;
        this.lblSectionGeo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblSectionGeo.ForeColor = PanelThreatIntelligenceSettings.AccentColor;
        this.lblSectionGeo.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.lblSectionGeo.Name = "lblSectionGeo";
        this.lblSectionGeo.Selected = false;
        this.lblSectionGeo.SelectedColor = System.Drawing.Color.Empty;
        this.lblSectionGeo.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("GeoIP & Country-level Blocking (Geo-fencing)");
        //
        // chkEnableGeoBlocking
        //
        this.chkEnableGeoBlocking.AutoSize = true;
        this.chkEnableGeoBlocking.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableGeoBlocking.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkEnableGeoBlocking.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableGeoBlocking.Name = "chkEnableGeoBlocking";
        this.chkEnableGeoBlocking.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable country-level Geo-blocking");
        //
        // lblBlockedCountries
        //
        this.lblBlockedCountries.AutoSize = true;
        this.lblBlockedCountries.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblBlockedCountries.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblBlockedCountries.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblBlockedCountries.Name = "lblBlockedCountries";
        this.lblBlockedCountries.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Blocked country codes (ISO 3166-1 alpha-2, e.g. CN, RU)");
        //
        // txtBlockedCountries
        //
        this.txtBlockedCountries.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtBlockedCountries.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtBlockedCountries.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtBlockedCountries.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.txtBlockedCountries.Name = "txtBlockedCountries";
        //
        // chkEnableGeoIpAutoUpdate
        //
        this.chkEnableGeoIpAutoUpdate.AutoSize = true;
        this.chkEnableGeoIpAutoUpdate.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableGeoIpAutoUpdate.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.chkEnableGeoIpAutoUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableGeoIpAutoUpdate.Name = "chkEnableGeoIpAutoUpdate";
        this.chkEnableGeoIpAutoUpdate.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable GeoIP automatic database update");
        //
        // lblGeoV4
        //
        this.lblGeoV4.AutoSize = true;
        this.lblGeoV4.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblGeoV4.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblGeoV4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblGeoV4.Name = "lblGeoV4";
        this.lblGeoV4.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("GeoIP IPv4 Database URL:");
        //
        // txtGeoIpDatabaseIpv4Url
        //
        this.txtGeoIpDatabaseIpv4Url.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtGeoIpDatabaseIpv4Url.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtGeoIpDatabaseIpv4Url.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtGeoIpDatabaseIpv4Url.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtGeoIpDatabaseIpv4Url.Name = "txtGeoIpDatabaseIpv4Url";
        //
        // lblGeoV6
        //
        this.lblGeoV6.AutoSize = true;
        this.lblGeoV6.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblGeoV6.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblGeoV6.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblGeoV6.Name = "lblGeoV6";
        this.lblGeoV6.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("GeoIP IPv6 Database URL:");
        //
        // txtGeoIpDatabaseIpv6Url
        //
        this.txtGeoIpDatabaseIpv6Url.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtGeoIpDatabaseIpv6Url.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtGeoIpDatabaseIpv6Url.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtGeoIpDatabaseIpv6Url.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtGeoIpDatabaseIpv6Url.Name = "txtGeoIpDatabaseIpv6Url";
        //
        // lblGeoLocal
        //
        this.lblGeoLocal.AutoSize = true;
        this.lblGeoLocal.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblGeoLocal.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblGeoLocal.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblGeoLocal.Name = "lblGeoLocal";
        this.lblGeoLocal.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Local GeoIP CSV file path (optional)");
        //
        // flowGeoLocal
        //
        this.flowGeoLocal.AutoSize = true;
        this.flowGeoLocal.Controls.Add(this.txtGeoIpLocalFilePath);
        this.flowGeoLocal.Controls.Add(this.btnBrowseGeoIpFile);
        this.flowGeoLocal.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.flowGeoLocal.Name = "flowGeoLocal";
        this.flowGeoLocal.WrapContents = true;
        //
        // txtGeoIpLocalFilePath
        //
        this.txtGeoIpLocalFilePath.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtGeoIpLocalFilePath.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.txtGeoIpLocalFilePath.Margin = new System.Windows.Forms.Padding(0, 0, 10, 4);
        this.txtGeoIpLocalFilePath.Name = "txtGeoIpLocalFilePath";
        this.txtGeoIpLocalFilePath.Size = new System.Drawing.Size(300, 23);
        //
        // btnBrowseGeoIpFile
        //
        this.btnBrowseGeoIpFile.AutoSize = true;
        this.btnBrowseGeoIpFile.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnBrowseGeoIpFile.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnBrowseGeoIpFile.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.btnBrowseGeoIpFile.MinimumSize = new System.Drawing.Size(90, 28);
        this.btnBrowseGeoIpFile.Name = "btnBrowseGeoIpFile";
        this.btnBrowseGeoIpFile.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Browse...");
        //
        // lblGeoDays
        //
        this.lblGeoDays.AutoSize = true;
        this.lblGeoDays.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblGeoDays.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblGeoDays.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblGeoDays.Name = "lblGeoDays";
        this.lblGeoDays.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("GeoIP update interval (days)");
        //
        // numGeoIpUpdateDays
        //
        this.numGeoIpUpdateDays.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numGeoIpUpdateDays.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numGeoIpUpdateDays.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.numGeoIpUpdateDays.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.numGeoIpUpdateDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
        this.numGeoIpUpdateDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numGeoIpUpdateDays.Name = "numGeoIpUpdateDays";
        this.numGeoIpUpdateDays.Size = new System.Drawing.Size(150, 23);
        this.numGeoIpUpdateDays.Value = new decimal(new int[] { 7, 0, 0, 0 });
        //
        // btnUpdateGeoIpNow
        //
        this.btnUpdateGeoIpNow.AutoSize = true;
        this.btnUpdateGeoIpNow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnUpdateGeoIpNow.BackColor = PanelThreatIntelligenceSettings.AccentColor;
        this.btnUpdateGeoIpNow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnUpdateGeoIpNow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnUpdateGeoIpNow.ForeColor = System.Drawing.Color.White;
        this.btnUpdateGeoIpNow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.btnUpdateGeoIpNow.MinimumSize = new System.Drawing.Size(220, 32);
        this.btnUpdateGeoIpNow.Name = "btnUpdateGeoIpNow";
        this.btnUpdateGeoIpNow.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Update GeoIP Database Now");
        //
        // lblGeoIpStatus
        //
        this.lblGeoIpStatus.AutoSize = true;
        this.lblGeoIpStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblGeoIpStatus.ForeColor = PanelThreatIntelligenceSettings.BodyTextColor;
        this.lblGeoIpStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.lblGeoIpStatus.MaximumSize = new System.Drawing.Size(560, 0);
        this.lblGeoIpStatus.Name = "lblGeoIpStatus";
        //
        // btnSave
        //
        this.btnSave.BackColor = PanelThreatIntelligenceSettings.AccentColor;
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Margin = new System.Windows.Forms.Padding(0);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 32);
        this.btnSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        //
        // PanelThreatIntelligenceSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelThreatIntelligenceSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numHubPort)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numSyncInterval)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numFeedInterval)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numIpsumLevel)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numFeedTtlDays)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numAbuseMinConfidence)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numProbationDays)).EndInit();
        this.flowGeoLocal.ResumeLayout(false);
        this.flowGeoLocal.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numGeoIpUpdateDays)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Panel headerPanel;
    private SmartLabel pageTitle;
    private SmartLabel lblSectionCluster;
    private System.Windows.Forms.Label lblRole;
    private System.Windows.Forms.ComboBox comboClusterRole;
    private System.Windows.Forms.Label lblEndpoint;
    private System.Windows.Forms.TextBox txtHubEndpoint;
    private System.Windows.Forms.Label lblApiKey;
    private System.Windows.Forms.TextBox txtHubApiKey;
    private System.Windows.Forms.Label lblPort;
    private System.Windows.Forms.NumericUpDown numHubPort;
    private System.Windows.Forms.CheckBox chkThreatHubReverseProxy;
    private System.Windows.Forms.CheckBox chkThreatHubLoopbackOnly;
    private System.Windows.Forms.Label lblSync;
    private System.Windows.Forms.NumericUpDown numSyncInterval;
    private SmartLabel lblSectionFeeds;
    private System.Windows.Forms.CheckBox chkEnableFeeds;
    private System.Windows.Forms.Label lblFeedInterval;
    private System.Windows.Forms.NumericUpDown numFeedInterval;
    private System.Windows.Forms.Label lblIpsumLevel;
    private System.Windows.Forms.NumericUpDown numIpsumLevel;
    private System.Windows.Forms.Label lblFeedTtl;
    private System.Windows.Forms.NumericUpDown numFeedTtlDays;
    private System.Windows.Forms.Label lblAbuseMin;
    private System.Windows.Forms.NumericUpDown numAbuseMinConfidence;
    private System.Windows.Forms.Label lblAbuseKey;
    private System.Windows.Forms.TextBox txtAbuseApiKey;
    private System.Windows.Forms.Label lblCustomUrls;
    private System.Windows.Forms.TextBox txtCustomUrls;
    private SmartLabel lblSectionBogon;
    private System.Windows.Forms.CheckBox chkEnableDynamicBogon;
    private System.Windows.Forms.Label lblProbationDays;
    private System.Windows.Forms.NumericUpDown numProbationDays;
    private System.Windows.Forms.Label lblBogonV4;
    private System.Windows.Forms.TextBox txtBogonIpv4Url;
    private System.Windows.Forms.Label lblBogonV6;
    private System.Windows.Forms.TextBox txtBogonIpv6Url;
    private SmartLabel lblSectionGeo;
    private System.Windows.Forms.CheckBox chkEnableGeoBlocking;
    private System.Windows.Forms.Label lblBlockedCountries;
    private System.Windows.Forms.TextBox txtBlockedCountries;
    private System.Windows.Forms.CheckBox chkEnableGeoIpAutoUpdate;
    private System.Windows.Forms.Label lblGeoV4;
    private System.Windows.Forms.TextBox txtGeoIpDatabaseIpv4Url;
    private System.Windows.Forms.Label lblGeoV6;
    private System.Windows.Forms.TextBox txtGeoIpDatabaseIpv6Url;
    private System.Windows.Forms.Label lblGeoLocal;
    private System.Windows.Forms.FlowLayoutPanel flowGeoLocal;
    private System.Windows.Forms.TextBox txtGeoIpLocalFilePath;
    private System.Windows.Forms.Button btnBrowseGeoIpFile;
    private System.Windows.Forms.Label lblGeoDays;
    private System.Windows.Forms.NumericUpDown numGeoIpUpdateDays;
    private System.Windows.Forms.Button btnUpdateGeoIpNow;
    private System.Windows.Forms.Label lblGeoIpStatus;
    private System.Windows.Forms.Button btnSave;
}
