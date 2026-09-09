namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelDeceptionAndApiSettings
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
        this.lblHoneyTitle = new System.Windows.Forms.Label();
        this.chkEnableHoneyAccounts = new System.Windows.Forms.CheckBox();
        this.lblHoneyAccounts = new System.Windows.Forms.Label();
        this.txtHoneyAccounts = new System.Windows.Forms.TextBox();
        this.lblSoarTitle = new System.Windows.Forms.Label();
        this.lblScript = new System.Windows.Forms.Label();
        this.flowScript = new System.Windows.Forms.FlowLayoutPanel();
        this.txtSoarScriptPath = new System.Windows.Forms.TextBox();
        this.btnBrowseScript = new System.Windows.Forms.Button();
        this.lblApiTitle = new System.Windows.Forms.Label();
        this.chkEnableManagementApi = new System.Windows.Forms.CheckBox();
        this.lblApiPort = new System.Windows.Forms.Label();
        this.numApiPort = new System.Windows.Forms.NumericUpDown();
        this.lblApiKey = new System.Windows.Forms.Label();
        this.flowApiKey = new System.Windows.Forms.FlowLayoutPanel();
        this.txtApiKey = new System.Windows.Forms.TextBox();
        this.btnGenApiKey = new System.Windows.Forms.Button();
        this.btnSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        this.flowScript.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numApiPort)).BeginInit();
        this.flowApiKey.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.lblHoneyTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.chkEnableHoneyAccounts, 0, 1);
        this.tableLayoutMain.Controls.Add(this.lblHoneyAccounts, 0, 2);
        this.tableLayoutMain.Controls.Add(this.txtHoneyAccounts, 0, 3);
        this.tableLayoutMain.Controls.Add(this.lblSoarTitle, 0, 4);
        this.tableLayoutMain.Controls.Add(this.lblScript, 0, 5);
        this.tableLayoutMain.Controls.Add(this.flowScript, 0, 6);
        this.tableLayoutMain.Controls.Add(this.lblApiTitle, 0, 7);
        this.tableLayoutMain.Controls.Add(this.chkEnableManagementApi, 0, 8);
        this.tableLayoutMain.Controls.Add(this.lblApiPort, 0, 9);
        this.tableLayoutMain.Controls.Add(this.numApiPort, 0, 10);
        this.tableLayoutMain.Controls.Add(this.lblApiKey, 0, 11);
        this.tableLayoutMain.Controls.Add(this.flowApiKey, 0, 12);
        this.tableLayoutMain.Controls.Add(this.btnSave, 0, 13);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 14;
        for (int i = 0; i < 14; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // lblHoneyTitle
        //
        this.lblHoneyTitle.AutoSize = true;
        this.lblHoneyTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblHoneyTitle.ForeColor = PanelDeceptionAndApiSettings.AccentColor;
        this.lblHoneyTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.lblHoneyTitle.Name = "lblHoneyTitle";
        this.lblHoneyTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Deception & Honey-Accounts");
        //
        // chkEnableHoneyAccounts
        //
        this.chkEnableHoneyAccounts.AutoSize = true;
        this.chkEnableHoneyAccounts.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableHoneyAccounts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableHoneyAccounts.Name = "chkEnableHoneyAccounts";
        this.chkEnableHoneyAccounts.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable honey-account one-strike defense");
        //
        // lblHoneyAccounts
        //
        this.lblHoneyAccounts.AutoSize = true;
        this.lblHoneyAccounts.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblHoneyAccounts.ForeColor = PanelDeceptionAndApiSettings.BodyTextColor;
        this.lblHoneyAccounts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblHoneyAccounts.Name = "lblHoneyAccounts";
        this.lblHoneyAccounts.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Honey-accounts list");
        //
        // txtHoneyAccounts
        //
        this.txtHoneyAccounts.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtHoneyAccounts.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtHoneyAccounts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.txtHoneyAccounts.Multiline = true;
        this.txtHoneyAccounts.Name = "txtHoneyAccounts";
        this.txtHoneyAccounts.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtHoneyAccounts.Size = new System.Drawing.Size(400, 60);
        //
        // lblSoarTitle
        //
        this.lblSoarTitle.AutoSize = true;
        this.lblSoarTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblSoarTitle.ForeColor = PanelDeceptionAndApiSettings.AccentColor;
        this.lblSoarTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.lblSoarTitle.Name = "lblSoarTitle";
        this.lblSoarTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SOAR remediation automation script");
        //
        // lblScript
        //
        this.lblScript.AutoSize = true;
        this.lblScript.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblScript.ForeColor = PanelDeceptionAndApiSettings.BodyTextColor;
        this.lblScript.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblScript.Name = "lblScript";
        this.lblScript.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SOAR script path");
        //
        // flowScript
        //
        this.flowScript.AutoSize = true;
        this.flowScript.Controls.Add(this.txtSoarScriptPath);
        this.flowScript.Controls.Add(this.btnBrowseScript);
        this.flowScript.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.flowScript.Name = "flowScript";
        this.flowScript.WrapContents = true;
        //
        // txtSoarScriptPath
        //
        this.txtSoarScriptPath.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtSoarScriptPath.Margin = new System.Windows.Forms.Padding(0, 0, 10, 4);
        this.txtSoarScriptPath.Name = "txtSoarScriptPath";
        this.txtSoarScriptPath.Size = new System.Drawing.Size(300, 23);
        //
        // btnBrowseScript
        //
        this.btnBrowseScript.AutoSize = true;
        this.btnBrowseScript.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnBrowseScript.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnBrowseScript.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.btnBrowseScript.MinimumSize = new System.Drawing.Size(100, 28);
        this.btnBrowseScript.Name = "btnBrowseScript";
        this.btnBrowseScript.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Browse...");
        //
        // lblApiTitle
        //
        this.lblApiTitle.AutoSize = true;
        this.lblApiTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblApiTitle.ForeColor = PanelDeceptionAndApiSettings.AccentColor;
        this.lblApiTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.lblApiTitle.Name = "lblApiTitle";
        this.lblApiTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("RESTful Management API");
        //
        // chkEnableManagementApi
        //
        this.chkEnableManagementApi.AutoSize = true;
        this.chkEnableManagementApi.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableManagementApi.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.chkEnableManagementApi.Name = "chkEnableManagementApi";
        this.chkEnableManagementApi.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable RESTful Management API");
        //
        // lblApiPort
        //
        this.lblApiPort.AutoSize = true;
        this.lblApiPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblApiPort.ForeColor = PanelDeceptionAndApiSettings.BodyTextColor;
        this.lblApiPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblApiPort.Name = "lblApiPort";
        this.lblApiPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Management API port");
        //
        // numApiPort
        //
        this.numApiPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.numApiPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.numApiPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.numApiPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        this.numApiPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numApiPort.Name = "numApiPort";
        this.numApiPort.Size = new System.Drawing.Size(180, 23);
        this.numApiPort.Value = new decimal(new int[] { 8443, 0, 0, 0 });
        //
        // lblApiKey
        //
        this.lblApiKey.AutoSize = true;
        this.lblApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblApiKey.ForeColor = PanelDeceptionAndApiSettings.BodyTextColor;
        this.lblApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblApiKey.Name = "lblApiKey";
        this.lblApiKey.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Management API key (X-Api-Key)");
        //
        // flowApiKey
        //
        this.flowApiKey.AutoSize = true;
        this.flowApiKey.Controls.Add(this.txtApiKey);
        this.flowApiKey.Controls.Add(this.btnGenApiKey);
        this.flowApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 20);
        this.flowApiKey.Name = "flowApiKey";
        this.flowApiKey.WrapContents = true;
        //
        // txtApiKey
        //
        this.txtApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 10, 4);
        this.txtApiKey.Name = "txtApiKey";
        this.txtApiKey.Size = new System.Drawing.Size(260, 23);
        //
        // btnGenApiKey
        //
        this.btnGenApiKey.AutoSize = true;
        this.btnGenApiKey.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnGenApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnGenApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.btnGenApiKey.MinimumSize = new System.Drawing.Size(130, 28);
        this.btnGenApiKey.Name = "btnGenApiKey";
        this.btnGenApiKey.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Generate API Key");
        //
        // btnSave
        //
        this.btnSave.BackColor = PanelDeceptionAndApiSettings.AccentColor;
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Margin = new System.Windows.Forms.Padding(0);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 32);
        this.btnSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        //
        // PanelDeceptionAndApiSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelDeceptionAndApiSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowScript.ResumeLayout(false);
        this.flowScript.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numApiPort)).EndInit();
        this.flowApiKey.ResumeLayout(false);
        this.flowApiKey.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label lblHoneyTitle;
    private System.Windows.Forms.CheckBox chkEnableHoneyAccounts;
    private System.Windows.Forms.Label lblHoneyAccounts;
    private System.Windows.Forms.TextBox txtHoneyAccounts;
    private System.Windows.Forms.Label lblSoarTitle;
    private System.Windows.Forms.Label lblScript;
    private System.Windows.Forms.FlowLayoutPanel flowScript;
    private System.Windows.Forms.TextBox txtSoarScriptPath;
    private System.Windows.Forms.Button btnBrowseScript;
    private System.Windows.Forms.Label lblApiTitle;
    private System.Windows.Forms.CheckBox chkEnableManagementApi;
    private System.Windows.Forms.Label lblApiPort;
    private System.Windows.Forms.NumericUpDown numApiPort;
    private System.Windows.Forms.Label lblApiKey;
    private System.Windows.Forms.FlowLayoutPanel flowApiKey;
    private System.Windows.Forms.TextBox txtApiKey;
    private System.Windows.Forms.Button btnGenApiKey;
    private System.Windows.Forms.Button btnSave;
}
