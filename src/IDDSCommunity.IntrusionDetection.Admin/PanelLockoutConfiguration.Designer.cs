namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelLockoutConfiguration
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
        this.components = new System.ComponentModel.Container();
        this.tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
        this.headerPanel = new System.Windows.Forms.Panel();
        this.smartLabel5 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowSoftLocks = new System.Windows.Forms.FlowLayoutPanel();
        this.textBoxSoftLocks = new System.Windows.Forms.TextBox();
        this.errSoftLocks = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel2 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowSoftLockDuration = new System.Windows.Forms.FlowLayoutPanel();
        this.textBoxSoftLockDuration = new System.Windows.Forms.TextBox();
        this.errSoftLockDuration = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel3 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowHardLocks = new System.Windows.Forms.FlowLayoutPanel();
        this.textBoxHardLocks = new System.Windows.Forms.TextBox();
        this.errHardLocks = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel4 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowHardLockDuration = new System.Windows.Forms.FlowLayoutPanel();
        this.textBoxHardLockDuration = new System.Windows.Forms.TextBox();
        this.errHardLockDuration = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxLockForever = new System.Windows.Forms.CheckBox();
        this.checkBoxEnableCrossAgentCorrelation = new System.Windows.Forms.CheckBox();
        this.lblSprayAccount = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.numericSprayAccountThreshold = new System.Windows.Forms.NumericUpDown();
        this.lblSprayIp = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.numericSprayIpThreshold = new System.Windows.Forms.NumericUpDown();
        this.lblSlidingWindow = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.numericSlidingWindowMinutes = new System.Windows.Forms.NumericUpDown();
        this.labelSemanticDeduplicationSeconds = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.numericSemanticDeduplicationSeconds = new System.Windows.Forms.NumericUpDown();
        this.lblTrustedProxy = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxTrustedProxyCidrs = new System.Windows.Forms.TextBox();
        this.labelFirewallMode = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.comboBoxFirewallMode = new System.Windows.Forms.ComboBox();
        this.labelFirewallModeDescription = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.buttonSave = new System.Windows.Forms.Button();
        this.trustedProxyToolTip = new System.Windows.Forms.ToolTip(this.components);
        this.tableLayoutMain.SuspendLayout();
        this.flowSoftLocks.SuspendLayout();
        this.flowSoftLockDuration.SuspendLayout();
        this.flowHardLocks.SuspendLayout();
        this.flowHardLockDuration.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numericSprayAccountThreshold)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSprayIpThreshold)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSlidingWindowMinutes)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSemanticDeduplicationSeconds)).BeginInit();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.headerPanel, 0, 0);
        this.tableLayoutMain.Controls.Add(this.smartLabel1, 0, 1);
        this.tableLayoutMain.Controls.Add(this.flowSoftLocks, 0, 2);
        this.tableLayoutMain.Controls.Add(this.smartLabel2, 0, 3);
        this.tableLayoutMain.Controls.Add(this.flowSoftLockDuration, 0, 4);
        this.tableLayoutMain.Controls.Add(this.smartLabel3, 0, 5);
        this.tableLayoutMain.Controls.Add(this.flowHardLocks, 0, 6);
        this.tableLayoutMain.Controls.Add(this.smartLabel4, 0, 7);
        this.tableLayoutMain.Controls.Add(this.flowHardLockDuration, 0, 8);
        this.tableLayoutMain.Controls.Add(this.checkBoxLockForever, 0, 9);
        this.tableLayoutMain.Controls.Add(this.checkBoxEnableCrossAgentCorrelation, 0, 10);
        this.tableLayoutMain.Controls.Add(this.lblSprayAccount, 0, 11);
        this.tableLayoutMain.Controls.Add(this.numericSprayAccountThreshold, 0, 12);
        this.tableLayoutMain.Controls.Add(this.lblSprayIp, 0, 13);
        this.tableLayoutMain.Controls.Add(this.numericSprayIpThreshold, 0, 14);
        this.tableLayoutMain.Controls.Add(this.lblSlidingWindow, 0, 15);
        this.tableLayoutMain.Controls.Add(this.numericSlidingWindowMinutes, 0, 16);
        this.tableLayoutMain.Controls.Add(this.labelSemanticDeduplicationSeconds, 0, 17);
        this.tableLayoutMain.Controls.Add(this.numericSemanticDeduplicationSeconds, 0, 18);
        this.tableLayoutMain.Controls.Add(this.lblTrustedProxy, 0, 19);
        this.tableLayoutMain.Controls.Add(this.textBoxTrustedProxyCidrs, 0, 20);
        this.tableLayoutMain.Controls.Add(this.labelFirewallMode, 0, 21);
        this.tableLayoutMain.Controls.Add(this.comboBoxFirewallMode, 0, 22);
        this.tableLayoutMain.Controls.Add(this.labelFirewallModeDescription, 0, 23);
        this.tableLayoutMain.Controls.Add(this.buttonSave, 0, 24);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 25;
        for (int i = 0; i < 25; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // headerPanel
        //
        this.headerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.headerPanel.Controls.Add(this.smartLabel5);
        this.headerPanel.Height = 34;
        this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.headerPanel.Name = "headerPanel";
        //
        // smartLabel5 (page title)
        //
        this.smartLabel5.AutoSize = true;
        this.smartLabel5.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.smartLabel5.ForeColor = System.Drawing.Color.FromArgb(15, 118, 110);
        this.smartLabel5.Location = new System.Drawing.Point(0, 4);
        this.smartLabel5.Margin = new System.Windows.Forms.Padding(0);
        this.smartLabel5.Name = "smartLabel5";
        this.smartLabel5.Selected = false;
        this.smartLabel5.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel5.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Lock out configuration");
        this.smartLabel5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // smartLabel1
        //
        this.smartLabel1.AutoSize = true;
        this.smartLabel1.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel1.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel1.Name = "smartLabel1";
        this.smartLabel1.Selected = false;
        this.smartLabel1.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel1.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Soft lock threshold (unsuccessful logins)");
        //
        // flowSoftLocks
        //
        this.flowSoftLocks.AutoSize = true;
        this.flowSoftLocks.Controls.Add(this.textBoxSoftLocks);
        this.flowSoftLocks.Controls.Add(this.errSoftLocks);
        this.flowSoftLocks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.flowSoftLocks.Name = "flowSoftLocks";
        this.flowSoftLocks.WrapContents = false;
        //
        // textBoxSoftLocks
        //
        this.textBoxSoftLocks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.textBoxSoftLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSoftLocks.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxSoftLocks.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.textBoxSoftLocks.Name = "textBoxSoftLocks";
        this.textBoxSoftLocks.Size = new System.Drawing.Size(65, 23);
        //
        // errSoftLocks
        //
        this.errSoftLocks.AutoSize = true;
        this.errSoftLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.errSoftLocks.ForeColor = System.Drawing.Color.Red;
        this.errSoftLocks.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.errSoftLocks.Name = "errSoftLocks";
        this.errSoftLocks.Selected = false;
        this.errSoftLocks.SelectedColor = System.Drawing.Color.Empty;
        this.errSoftLocks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("value must be a number");
        this.errSoftLocks.Visible = false;
        //
        // smartLabel2
        //
        this.smartLabel2.AutoSize = true;
        this.smartLabel2.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel2.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel2.Name = "smartLabel2";
        this.smartLabel2.Selected = false;
        this.smartLabel2.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel2.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Soft lock duration (minutes)");
        //
        // flowSoftLockDuration
        //
        this.flowSoftLockDuration.AutoSize = true;
        this.flowSoftLockDuration.Controls.Add(this.textBoxSoftLockDuration);
        this.flowSoftLockDuration.Controls.Add(this.errSoftLockDuration);
        this.flowSoftLockDuration.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.flowSoftLockDuration.Name = "flowSoftLockDuration";
        this.flowSoftLockDuration.WrapContents = false;
        //
        // textBoxSoftLockDuration
        //
        this.textBoxSoftLockDuration.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.textBoxSoftLockDuration.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSoftLockDuration.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxSoftLockDuration.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.textBoxSoftLockDuration.Name = "textBoxSoftLockDuration";
        this.textBoxSoftLockDuration.Size = new System.Drawing.Size(65, 23);
        //
        // errSoftLockDuration
        //
        this.errSoftLockDuration.AutoSize = true;
        this.errSoftLockDuration.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.errSoftLockDuration.ForeColor = System.Drawing.Color.Red;
        this.errSoftLockDuration.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.errSoftLockDuration.Name = "errSoftLockDuration";
        this.errSoftLockDuration.Selected = false;
        this.errSoftLockDuration.SelectedColor = System.Drawing.Color.Empty;
        this.errSoftLockDuration.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("value must be a number");
        this.errSoftLockDuration.Visible = false;
        //
        // smartLabel3
        //
        this.smartLabel3.AutoSize = true;
        this.smartLabel3.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel3.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel3.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel3.Name = "smartLabel3";
        this.smartLabel3.Selected = false;
        this.smartLabel3.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel3.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Hard lock threshold (unsuccessful logins)");
        //
        // flowHardLocks
        //
        this.flowHardLocks.AutoSize = true;
        this.flowHardLocks.Controls.Add(this.textBoxHardLocks);
        this.flowHardLocks.Controls.Add(this.errHardLocks);
        this.flowHardLocks.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.flowHardLocks.Name = "flowHardLocks";
        this.flowHardLocks.WrapContents = false;
        //
        // textBoxHardLocks
        //
        this.textBoxHardLocks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.textBoxHardLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxHardLocks.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxHardLocks.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.textBoxHardLocks.Name = "textBoxHardLocks";
        this.textBoxHardLocks.Size = new System.Drawing.Size(65, 23);
        //
        // errHardLocks
        //
        this.errHardLocks.AutoSize = true;
        this.errHardLocks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.errHardLocks.ForeColor = System.Drawing.Color.Red;
        this.errHardLocks.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.errHardLocks.Name = "errHardLocks";
        this.errHardLocks.Selected = false;
        this.errHardLocks.SelectedColor = System.Drawing.Color.Empty;
        this.errHardLocks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("value must be a number");
        this.errHardLocks.Visible = false;
        //
        // smartLabel4
        //
        this.smartLabel4.AutoSize = true;
        this.smartLabel4.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel4.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel4.Name = "smartLabel4";
        this.smartLabel4.Selected = false;
        this.smartLabel4.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel4.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Hard lock duration (hours)");
        //
        // flowHardLockDuration
        //
        this.flowHardLockDuration.AutoSize = true;
        this.flowHardLockDuration.Controls.Add(this.textBoxHardLockDuration);
        this.flowHardLockDuration.Controls.Add(this.errHardLockDuration);
        this.flowHardLockDuration.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.flowHardLockDuration.Name = "flowHardLockDuration";
        this.flowHardLockDuration.WrapContents = false;
        //
        // textBoxHardLockDuration
        //
        this.textBoxHardLockDuration.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.textBoxHardLockDuration.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxHardLockDuration.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxHardLockDuration.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.textBoxHardLockDuration.Name = "textBoxHardLockDuration";
        this.textBoxHardLockDuration.Size = new System.Drawing.Size(65, 23);
        //
        // errHardLockDuration
        //
        this.errHardLockDuration.AutoSize = true;
        this.errHardLockDuration.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.errHardLockDuration.ForeColor = System.Drawing.Color.Red;
        this.errHardLockDuration.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.errHardLockDuration.Name = "errHardLockDuration";
        this.errHardLockDuration.Selected = false;
        this.errHardLockDuration.SelectedColor = System.Drawing.Color.Empty;
        this.errHardLockDuration.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("value must be a number");
        this.errHardLockDuration.Visible = false;
        //
        // checkBoxLockForever
        //
        this.checkBoxLockForever.AutoSize = true;
        this.checkBoxLockForever.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxLockForever.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.checkBoxLockForever.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.checkBoxLockForever.Name = "checkBoxLockForever";
        this.checkBoxLockForever.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Hard lock forever");
        this.checkBoxLockForever.UseVisualStyleBackColor = true;
        //
        // checkBoxEnableCrossAgentCorrelation
        //
        this.checkBoxEnableCrossAgentCorrelation.AutoSize = true;
        this.checkBoxEnableCrossAgentCorrelation.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxEnableCrossAgentCorrelation.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxEnableCrossAgentCorrelation.Name = "checkBoxEnableCrossAgentCorrelation";
        this.checkBoxEnableCrossAgentCorrelation.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable cross-agent password-spray detection");
        //
        // lblSprayAccount
        //
        this.lblSprayAccount.AutoSize = true;
        this.lblSprayAccount.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSprayAccount.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.lblSprayAccount.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSprayAccount.Name = "lblSprayAccount";
        this.lblSprayAccount.Selected = false;
        this.lblSprayAccount.SelectedColor = System.Drawing.Color.Empty;
        this.lblSprayAccount.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Accounts per source IP threshold");
        //
        // numericSprayAccountThreshold
        //
        this.numericSprayAccountThreshold.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numericSprayAccountThreshold.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numericSprayAccountThreshold.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.numericSprayAccountThreshold.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this.numericSprayAccountThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        this.numericSprayAccountThreshold.Name = "numericSprayAccountThreshold";
        this.numericSprayAccountThreshold.Size = new System.Drawing.Size(80, 23);
        this.numericSprayAccountThreshold.Value = new decimal(new int[] { 5, 0, 0, 0 });
        //
        // lblSprayIp
        //
        this.lblSprayIp.AutoSize = true;
        this.lblSprayIp.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSprayIp.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.lblSprayIp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSprayIp.Name = "lblSprayIp";
        this.lblSprayIp.Selected = false;
        this.lblSprayIp.SelectedColor = System.Drawing.Color.Empty;
        this.lblSprayIp.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Source IPs per account threshold");
        //
        // numericSprayIpThreshold
        //
        this.numericSprayIpThreshold.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numericSprayIpThreshold.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numericSprayIpThreshold.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.numericSprayIpThreshold.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        this.numericSprayIpThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        this.numericSprayIpThreshold.Name = "numericSprayIpThreshold";
        this.numericSprayIpThreshold.Size = new System.Drawing.Size(80, 23);
        this.numericSprayIpThreshold.Value = new decimal(new int[] { 5, 0, 0, 0 });
        //
        // lblSlidingWindow
        //
        this.lblSlidingWindow.AutoSize = true;
        this.lblSlidingWindow.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSlidingWindow.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.lblSlidingWindow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSlidingWindow.Name = "lblSlidingWindow";
        this.lblSlidingWindow.Selected = false;
        this.lblSlidingWindow.SelectedColor = System.Drawing.Color.Empty;
        this.lblSlidingWindow.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cross-agent sliding window (minutes)");
        //
        // numericSlidingWindowMinutes
        //
        this.numericSlidingWindowMinutes.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numericSlidingWindowMinutes.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numericSlidingWindowMinutes.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.numericSlidingWindowMinutes.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
        this.numericSlidingWindowMinutes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numericSlidingWindowMinutes.Name = "numericSlidingWindowMinutes";
        this.numericSlidingWindowMinutes.Size = new System.Drawing.Size(80, 23);
        this.numericSlidingWindowMinutes.Value = new decimal(new int[] { 10, 0, 0, 0 });
        //
        // labelSemanticDeduplicationSeconds
        //
        this.labelSemanticDeduplicationSeconds.AutoEllipsis = true;
        this.labelSemanticDeduplicationSeconds.AutoSize = true;
        this.labelSemanticDeduplicationSeconds.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelSemanticDeduplicationSeconds.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.labelSemanticDeduplicationSeconds.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelSemanticDeduplicationSeconds.Name = "labelSemanticDeduplicationSeconds";
        this.labelSemanticDeduplicationSeconds.Selected = false;
        this.labelSemanticDeduplicationSeconds.SelectedColor = System.Drawing.Color.Empty;
        this.labelSemanticDeduplicationSeconds.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cross-agent duplicate tolerance (seconds)");
        this.labelSemanticDeduplicationSeconds.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // numericSemanticDeduplicationSeconds
        //
        this.numericSemanticDeduplicationSeconds.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numericSemanticDeduplicationSeconds.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numericSemanticDeduplicationSeconds.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.numericSemanticDeduplicationSeconds.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        this.numericSemanticDeduplicationSeconds.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numericSemanticDeduplicationSeconds.Name = "numericSemanticDeduplicationSeconds";
        this.numericSemanticDeduplicationSeconds.Size = new System.Drawing.Size(65, 23);
        this.numericSemanticDeduplicationSeconds.Value = new decimal(new int[] { 15, 0, 0, 0 });
        //
        // lblTrustedProxy
        //
        this.lblTrustedProxy.AutoSize = true;
        this.lblTrustedProxy.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblTrustedProxy.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.lblTrustedProxy.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblTrustedProxy.Name = "lblTrustedProxy";
        this.lblTrustedProxy.Selected = false;
        this.lblTrustedProxy.SelectedColor = System.Drawing.Color.Empty;
        this.lblTrustedProxy.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Trusted proxy IP/CIDR list");
        //
        // textBoxTrustedProxyCidrs
        //
        this.textBoxTrustedProxyCidrs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxTrustedProxyCidrs.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxTrustedProxyCidrs.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.textBoxTrustedProxyCidrs.Name = "textBoxTrustedProxyCidrs";
        //
        // labelFirewallMode
        //
        this.labelFirewallMode.AutoEllipsis = true;
        this.labelFirewallMode.AutoSize = true;
        this.labelFirewallMode.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.labelFirewallMode.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.labelFirewallMode.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.labelFirewallMode.Name = "labelFirewallMode";
        this.labelFirewallMode.Selected = false;
        this.labelFirewallMode.SelectedColor = System.Drawing.Color.Empty;
        this.labelFirewallMode.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Firewall blocking mode");
        //
        // comboBoxFirewallMode
        //
        this.comboBoxFirewallMode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.comboBoxFirewallMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxFirewallMode.DropDownWidth = 260;
        this.comboBoxFirewallMode.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboBoxFirewallMode.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.comboBoxFirewallMode.FormattingEnabled = true;
        this.comboBoxFirewallMode.Items.AddRange(new object[] {
            global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Inbound blocking (recommended)"),
            global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Bidirectional blocking")});
        this.comboBoxFirewallMode.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.comboBoxFirewallMode.Name = "comboBoxFirewallMode";
        this.comboBoxFirewallMode.Size = new System.Drawing.Size(260, 23);
        this.comboBoxFirewallMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxFirewallMode_SelectedIndexChanged);
        //
        // labelFirewallModeDescription
        //
        this.labelFirewallModeDescription.AutoEllipsis = true;
        this.labelFirewallModeDescription.AutoSize = true;
        this.labelFirewallModeDescription.Font = new System.Drawing.Font("Segoe UI", 8F);
        this.labelFirewallModeDescription.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.labelFirewallModeDescription.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.labelFirewallModeDescription.MaximumSize = new System.Drawing.Size(460, 0);
        this.labelFirewallModeDescription.Name = "labelFirewallModeDescription";
        this.labelFirewallModeDescription.Selected = false;
        this.labelFirewallModeDescription.SelectedColor = System.Drawing.Color.Empty;
        //
        //
        // buttonSave
        //
        this.buttonSave.BackColor = System.Drawing.Color.FromArgb(15, 118, 110);
        this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.buttonSave.ForeColor = System.Drawing.Color.White;
        this.buttonSave.Margin = new System.Windows.Forms.Padding(0);
        this.buttonSave.Name = "buttonSave";
        this.buttonSave.Size = new System.Drawing.Size(120, 32);
        this.buttonSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        this.buttonSave.UseVisualStyleBackColor = false;
        this.buttonSave.Click += new System.EventHandler(this.pictureBoxSave_Click);
        //
        // PanelLockoutConfiguration
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Name = "PanelLockoutConfiguration";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowSoftLocks.ResumeLayout(false);
        this.flowSoftLocks.PerformLayout();
        this.flowSoftLockDuration.ResumeLayout(false);
        this.flowSoftLockDuration.PerformLayout();
        this.flowHardLocks.ResumeLayout(false);
        this.flowHardLocks.PerformLayout();
        this.flowHardLockDuration.ResumeLayout(false);
        this.flowHardLockDuration.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numericSprayAccountThreshold)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSprayIpThreshold)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSlidingWindowMinutes)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericSemanticDeduplicationSeconds)).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Panel headerPanel;
    private SmartLabel smartLabel5;
    private SmartLabel smartLabel1;
    private System.Windows.Forms.FlowLayoutPanel flowSoftLocks;
    private System.Windows.Forms.TextBox textBoxSoftLocks;
    private SmartLabel errSoftLocks;
    private SmartLabel smartLabel2;
    private System.Windows.Forms.FlowLayoutPanel flowSoftLockDuration;
    private System.Windows.Forms.TextBox textBoxSoftLockDuration;
    private SmartLabel errSoftLockDuration;
    private SmartLabel smartLabel3;
    private System.Windows.Forms.FlowLayoutPanel flowHardLocks;
    private System.Windows.Forms.TextBox textBoxHardLocks;
    private SmartLabel errHardLocks;
    private SmartLabel smartLabel4;
    private System.Windows.Forms.FlowLayoutPanel flowHardLockDuration;
    private System.Windows.Forms.TextBox textBoxHardLockDuration;
    private SmartLabel errHardLockDuration;
    private System.Windows.Forms.CheckBox checkBoxLockForever;
    private System.Windows.Forms.CheckBox checkBoxEnableCrossAgentCorrelation;
    private SmartLabel lblSprayAccount;
    private System.Windows.Forms.NumericUpDown numericSprayAccountThreshold;
    private SmartLabel lblSprayIp;
    private System.Windows.Forms.NumericUpDown numericSprayIpThreshold;
    private SmartLabel lblSlidingWindow;
    private System.Windows.Forms.NumericUpDown numericSlidingWindowMinutes;
    private SmartLabel labelSemanticDeduplicationSeconds;
    private System.Windows.Forms.NumericUpDown numericSemanticDeduplicationSeconds;
    private SmartLabel lblTrustedProxy;
    private System.Windows.Forms.TextBox textBoxTrustedProxyCidrs;
    private SmartLabel labelFirewallMode;
    private System.Windows.Forms.ComboBox comboBoxFirewallMode;
    private SmartLabel labelFirewallModeDescription;
    private System.Windows.Forms.Button buttonSave;
    private System.Windows.Forms.ToolTip trustedProxyToolTip;
}
