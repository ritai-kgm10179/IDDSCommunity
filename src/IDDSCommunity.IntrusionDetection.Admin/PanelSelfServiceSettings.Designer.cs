namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelSelfServiceSettings
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
        this.lblTitle = new System.Windows.Forms.Label();
        this.chkEnablePortal = new System.Windows.Forms.CheckBox();
        this.lblPort = new System.Windows.Forms.Label();
        this.numPort = new System.Windows.Forms.NumericUpDown();
        this.lblListenIp = new System.Windows.Forms.Label();
        this.txtListenIp = new System.Windows.Forms.TextBox();
        this.lblSecret = new System.Windows.Forms.Label();
        this.flowSecret = new System.Windows.Forms.FlowLayoutPanel();
        this.txtTotpSecret = new System.Windows.Forms.TextBox();
        this.btnGenerate = new System.Windows.Forms.Button();
        this.lblTest = new System.Windows.Forms.Label();
        this.flowTest = new System.Windows.Forms.FlowLayoutPanel();
        this.txtTestCode = new System.Windows.Forms.TextBox();
        this.btnVerify = new System.Windows.Forms.Button();
        this.lblVerificationResult = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
        this.flowSecret.SuspendLayout();
        this.flowTest.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.lblTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.chkEnablePortal, 0, 1);
        this.tableLayoutMain.Controls.Add(this.lblPort, 0, 2);
        this.tableLayoutMain.Controls.Add(this.numPort, 0, 3);
        this.tableLayoutMain.Controls.Add(this.lblListenIp, 0, 4);
        this.tableLayoutMain.Controls.Add(this.txtListenIp, 0, 5);
        this.tableLayoutMain.Controls.Add(this.lblSecret, 0, 6);
        this.tableLayoutMain.Controls.Add(this.flowSecret, 0, 7);
        this.tableLayoutMain.Controls.Add(this.lblTest, 0, 8);
        this.tableLayoutMain.Controls.Add(this.flowTest, 0, 9);
        this.tableLayoutMain.Controls.Add(this.lblVerificationResult, 0, 10);
        this.tableLayoutMain.Controls.Add(this.btnSave, 0, 11);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 12;
        for (int i = 0; i < 12; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // lblTitle
        //
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = PanelSelfServiceSettings.AccentColor;
        this.lblTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Self-service unblock portal");
        //
        // chkEnablePortal
        //
        this.chkEnablePortal.AutoSize = true;
        this.chkEnablePortal.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnablePortal.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.chkEnablePortal.Name = "chkEnablePortal";
        this.chkEnablePortal.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable self-service unblock portal");
        //
        // lblPort
        //
        this.lblPort.AutoSize = true;
        this.lblPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblPort.ForeColor = PanelSelfServiceSettings.BodyTextColor;
        this.lblPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblPort.Name = "lblPort";
        this.lblPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Portal listening port");
        //
        // numPort
        //
        this.numPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        this.numPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numPort.Name = "numPort";
        this.numPort.Size = new System.Drawing.Size(180, 23);
        this.numPort.Value = new decimal(new int[] { 8444, 0, 0, 0 });
        //
        // lblListenIp
        //
        this.lblListenIp.AutoSize = true;
        this.lblListenIp.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblListenIp.ForeColor = PanelSelfServiceSettings.BodyTextColor;
        this.lblListenIp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblListenIp.Name = "lblListenIp";
        this.lblListenIp.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Portal listening IP address");
        //
        // txtListenIp
        //
        this.txtListenIp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtListenIp.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtListenIp.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtListenIp.Name = "txtListenIp";
        //
        // lblSecret
        //
        this.lblSecret.AutoSize = true;
        this.lblSecret.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSecret.ForeColor = PanelSelfServiceSettings.BodyTextColor;
        this.lblSecret.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSecret.Name = "lblSecret";
        this.lblSecret.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("RFC 6238 TOTP Secret Key (Base32)");
        //
        // flowSecret
        //
        this.flowSecret.AutoSize = true;
        this.flowSecret.Controls.Add(this.txtTotpSecret);
        this.flowSecret.Controls.Add(this.btnGenerate);
        this.flowSecret.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.flowSecret.Name = "flowSecret";
        this.flowSecret.WrapContents = true;
        //
        // txtTotpSecret
        //
        this.txtTotpSecret.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtTotpSecret.Margin = new System.Windows.Forms.Padding(0, 0, 10, 4);
        this.txtTotpSecret.Name = "txtTotpSecret";
        this.txtTotpSecret.Size = new System.Drawing.Size(260, 23);
        //
        // btnGenerate
        //
        this.btnGenerate.AutoSize = true;
        this.btnGenerate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnGenerate.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnGenerate.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.btnGenerate.MinimumSize = new System.Drawing.Size(130, 28);
        this.btnGenerate.Name = "btnGenerate";
        this.btnGenerate.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Generate Secret");
        //
        // lblTest
        //
        this.lblTest.AutoSize = true;
        this.lblTest.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblTest.ForeColor = PanelSelfServiceSettings.BodyTextColor;
        this.lblTest.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblTest.Name = "lblTest";
        this.lblTest.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Test Verification Code");
        //
        // flowTest
        //
        this.flowTest.AutoSize = true;
        this.flowTest.Controls.Add(this.txtTestCode);
        this.flowTest.Controls.Add(this.btnVerify);
        this.flowTest.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.flowTest.Name = "flowTest";
        this.flowTest.WrapContents = true;
        //
        // txtTestCode
        //
        this.txtTestCode.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtTestCode.Margin = new System.Windows.Forms.Padding(0, 0, 10, 4);
        this.txtTestCode.MaxLength = 6;
        this.txtTestCode.Name = "txtTestCode";
        this.txtTestCode.Size = new System.Drawing.Size(140, 23);
        //
        // btnVerify
        //
        this.btnVerify.AutoSize = true;
        this.btnVerify.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnVerify.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnVerify.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.btnVerify.MinimumSize = new System.Drawing.Size(110, 28);
        this.btnVerify.Name = "btnVerify";
        this.btnVerify.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Verify Code");
        //
        // lblVerificationResult
        //
        this.lblVerificationResult.AutoSize = true;
        this.lblVerificationResult.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblVerificationResult.ForeColor = PanelSelfServiceSettings.BodyTextColor;
        this.lblVerificationResult.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.lblVerificationResult.Name = "lblVerificationResult";
        //
        // btnSave
        //
        this.btnSave.BackColor = PanelSelfServiceSettings.AccentColor;
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Margin = new System.Windows.Forms.Padding(0);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 32);
        this.btnSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        //
        // PanelSelfServiceSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelSelfServiceSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
        this.flowSecret.ResumeLayout(false);
        this.flowSecret.PerformLayout();
        this.flowTest.ResumeLayout(false);
        this.flowTest.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.CheckBox chkEnablePortal;
    private System.Windows.Forms.Label lblPort;
    private System.Windows.Forms.NumericUpDown numPort;
    private System.Windows.Forms.Label lblListenIp;
    private System.Windows.Forms.TextBox txtListenIp;
    private System.Windows.Forms.Label lblSecret;
    private System.Windows.Forms.FlowLayoutPanel flowSecret;
    private System.Windows.Forms.TextBox txtTotpSecret;
    private System.Windows.Forms.Button btnGenerate;
    private System.Windows.Forms.Label lblTest;
    private System.Windows.Forms.FlowLayoutPanel flowTest;
    private System.Windows.Forms.TextBox txtTestCode;
    private System.Windows.Forms.Button btnVerify;
    private System.Windows.Forms.Label lblVerificationResult;
    private System.Windows.Forms.Button btnSave;
}
