namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelPluginConfiguration
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
        this.smartLabelAgentName = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel7 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxEnableSecurityAgent = new System.Windows.Forms.CheckBox();
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
        this.checkBoxOverrideConfiguration = new System.Windows.Forms.CheckBox();
        this.smartLabelCustomConfig = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowLayoutPanelCustomPluginSettings = new System.Windows.Forms.FlowLayoutPanel();
        this.buttonSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        this.headerPanel.SuspendLayout();
        this.flowSoftLocks.SuspendLayout();
        this.flowSoftLockDuration.SuspendLayout();
        this.flowHardLocks.SuspendLayout();
        this.flowHardLockDuration.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.headerPanel, 0, 0);
        this.tableLayoutMain.Controls.Add(this.smartLabel7, 0, 1);
        this.tableLayoutMain.Controls.Add(this.checkBoxEnableSecurityAgent, 0, 2);
        this.tableLayoutMain.Controls.Add(this.smartLabel1, 0, 3);
        this.tableLayoutMain.Controls.Add(this.flowSoftLocks, 0, 4);
        this.tableLayoutMain.Controls.Add(this.smartLabel2, 0, 5);
        this.tableLayoutMain.Controls.Add(this.flowSoftLockDuration, 0, 6);
        this.tableLayoutMain.Controls.Add(this.smartLabel3, 0, 7);
        this.tableLayoutMain.Controls.Add(this.flowHardLocks, 0, 8);
        this.tableLayoutMain.Controls.Add(this.smartLabel4, 0, 9);
        this.tableLayoutMain.Controls.Add(this.flowHardLockDuration, 0, 10);
        this.tableLayoutMain.Controls.Add(this.checkBoxLockForever, 0, 11);
        this.tableLayoutMain.Controls.Add(this.checkBoxOverrideConfiguration, 0, 12);
        this.tableLayoutMain.Controls.Add(this.smartLabelCustomConfig, 0, 13);
        this.tableLayoutMain.Controls.Add(this.flowLayoutPanelCustomPluginSettings, 0, 14);
        this.tableLayoutMain.Controls.Add(this.buttonSave, 0, 15);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 16;
        for (int i = 0; i < 16; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // headerPanel
        //
        this.headerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.headerPanel.Controls.Add(this.smartLabelAgentName);
        this.headerPanel.Height = 34;
        this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.headerPanel.Name = "headerPanel";
        //
        // smartLabelAgentName
        //
        this.smartLabelAgentName.AutoSize = true;
        this.smartLabelAgentName.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.smartLabelAgentName.ForeColor = System.Drawing.Color.FromArgb(15, 118, 110);
        this.smartLabelAgentName.Location = new System.Drawing.Point(0, 4);
        this.smartLabelAgentName.Margin = new System.Windows.Forms.Padding(0);
        this.smartLabelAgentName.Name = "smartLabelAgentName";
        this.smartLabelAgentName.Selected = false;
        this.smartLabelAgentName.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelAgentName.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Agent configuration");
        this.smartLabelAgentName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // smartLabel7
        //
        this.smartLabel7.AutoSize = true;
        this.smartLabel7.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabel7.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel7.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.smartLabel7.Name = "smartLabel7";
        this.smartLabel7.Selected = false;
        this.smartLabel7.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel7.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Lock out configuration");
        this.smartLabel7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // checkBoxEnableSecurityAgent
        //
        this.checkBoxEnableSecurityAgent.AutoSize = true;
        this.checkBoxEnableSecurityAgent.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxEnableSecurityAgent.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.checkBoxEnableSecurityAgent.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxEnableSecurityAgent.Name = "checkBoxEnableSecurityAgent";
        this.checkBoxEnableSecurityAgent.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable this Security Agent");
        this.checkBoxEnableSecurityAgent.UseVisualStyleBackColor = true;
        this.checkBoxEnableSecurityAgent.CheckedChanged += new System.EventHandler(this.checkBox_CheckedChanged);
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
        this.textBoxSoftLocks.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_KeyPress);
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
        this.textBoxSoftLockDuration.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_KeyPress);
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
        this.textBoxHardLocks.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_KeyPress);
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
        this.flowHardLockDuration.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
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
        this.textBoxHardLockDuration.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox_KeyPress);
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
        this.checkBoxLockForever.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxLockForever.Name = "checkBoxLockForever";
        this.checkBoxLockForever.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Hard lock forever");
        this.checkBoxLockForever.UseVisualStyleBackColor = true;
        this.checkBoxLockForever.CheckedChanged += new System.EventHandler(this.checkBox_CheckedChanged);
        //
        // checkBoxOverrideConfiguration
        //
        this.checkBoxOverrideConfiguration.AutoSize = true;
        this.checkBoxOverrideConfiguration.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxOverrideConfiguration.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.checkBoxOverrideConfiguration.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.checkBoxOverrideConfiguration.Name = "checkBoxOverrideConfiguration";
        this.checkBoxOverrideConfiguration.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Override configuration");
        this.checkBoxOverrideConfiguration.UseVisualStyleBackColor = true;
        this.checkBoxOverrideConfiguration.CheckedChanged += new System.EventHandler(this.checkBoxOverrideConfiguration_CheckedChanged);
        //
        // smartLabelCustomConfig
        //
        this.smartLabelCustomConfig.AutoSize = true;
        this.smartLabelCustomConfig.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.smartLabelCustomConfig.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabelCustomConfig.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
        this.smartLabelCustomConfig.Name = "smartLabelCustomConfig";
        this.smartLabelCustomConfig.Selected = false;
        this.smartLabelCustomConfig.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelCustomConfig.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Extended configuration");
        this.smartLabelCustomConfig.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // flowLayoutPanelCustomPluginSettings
        //
        this.flowLayoutPanelCustomPluginSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.flowLayoutPanelCustomPluginSettings.AutoScroll = true;
        this.flowLayoutPanelCustomPluginSettings.AutoScrollMargin = new System.Drawing.Size(0, 8);
        this.flowLayoutPanelCustomPluginSettings.BackColor = System.Drawing.Color.FromArgb(248, 250, 251);
        this.flowLayoutPanelCustomPluginSettings.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.flowLayoutPanelCustomPluginSettings.Height = 260;
        this.flowLayoutPanelCustomPluginSettings.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.flowLayoutPanelCustomPluginSettings.Name = "flowLayoutPanelCustomPluginSettings";
        this.flowLayoutPanelCustomPluginSettings.Padding = new System.Windows.Forms.Padding(8);
        this.flowLayoutPanelCustomPluginSettings.WrapContents = false;
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
        //
        // PanelPluginConfiguration
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelPluginConfiguration";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.headerPanel.ResumeLayout(false);
        this.flowSoftLocks.ResumeLayout(false);
        this.flowSoftLocks.PerformLayout();
        this.flowSoftLockDuration.ResumeLayout(false);
        this.flowSoftLockDuration.PerformLayout();
        this.flowHardLocks.ResumeLayout(false);
        this.flowHardLocks.PerformLayout();
        this.flowHardLockDuration.ResumeLayout(false);
        this.flowHardLockDuration.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Panel headerPanel;
    private SmartLabel smartLabelAgentName;
    private SmartLabel smartLabel7;
    private System.Windows.Forms.CheckBox checkBoxEnableSecurityAgent;
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
    private System.Windows.Forms.CheckBox checkBoxOverrideConfiguration;
    private SmartLabel smartLabelCustomConfig;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelCustomPluginSettings;
    private System.Windows.Forms.Button buttonSave;
}
