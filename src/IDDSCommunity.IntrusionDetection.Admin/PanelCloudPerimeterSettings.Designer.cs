namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelCloudPerimeterSettings
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
        this.chkEnableCloudPerimeter = new System.Windows.Forms.CheckBox();
        this.lblProvider = new System.Windows.Forms.Label();
        this.comboProviderType = new System.Windows.Forms.ComboBox();
        this.lblApiKey = new System.Windows.Forms.Label();
        this.txtApiKey = new System.Windows.Forms.TextBox();
        this.lblEndpoint = new System.Windows.Forms.Label();
        this.txtEndpointUrl = new System.Windows.Forms.TextBox();
        this.lblResource = new System.Windows.Forms.Label();
        this.txtResourceId = new System.Windows.Forms.TextBox();
        this.lblSecondary = new System.Windows.Forms.Label();
        this.txtSecondaryId = new System.Windows.Forms.TextBox();
        this.lblTertiary = new System.Windows.Forms.Label();
        this.txtTertiaryId = new System.Windows.Forms.TextBox();
        this.flowTest = new System.Windows.Forms.FlowLayoutPanel();
        this.btnTestConnection = new System.Windows.Forms.Button();
        this.lblStatus = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        this.flowTest.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.lblTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.chkEnableCloudPerimeter, 0, 1);
        this.tableLayoutMain.Controls.Add(this.lblProvider, 0, 2);
        this.tableLayoutMain.Controls.Add(this.comboProviderType, 0, 3);
        this.tableLayoutMain.Controls.Add(this.lblApiKey, 0, 4);
        this.tableLayoutMain.Controls.Add(this.txtApiKey, 0, 5);
        this.tableLayoutMain.Controls.Add(this.lblEndpoint, 0, 6);
        this.tableLayoutMain.Controls.Add(this.txtEndpointUrl, 0, 7);
        this.tableLayoutMain.Controls.Add(this.lblResource, 0, 8);
        this.tableLayoutMain.Controls.Add(this.txtResourceId, 0, 9);
        this.tableLayoutMain.Controls.Add(this.lblSecondary, 0, 10);
        this.tableLayoutMain.Controls.Add(this.txtSecondaryId, 0, 11);
        this.tableLayoutMain.Controls.Add(this.lblTertiary, 0, 12);
        this.tableLayoutMain.Controls.Add(this.txtTertiaryId, 0, 13);
        this.tableLayoutMain.Controls.Add(this.flowTest, 0, 14);
        this.tableLayoutMain.Controls.Add(this.btnSave, 0, 15);
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
        // lblTitle
        //
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = PanelCloudPerimeterSettings.AccentColor;
        this.lblTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cloud perimeter defense");
        //
        // chkEnableCloudPerimeter
        //
        this.chkEnableCloudPerimeter.AutoSize = true;
        this.chkEnableCloudPerimeter.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.chkEnableCloudPerimeter.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.chkEnableCloudPerimeter.Name = "chkEnableCloudPerimeter";
        this.chkEnableCloudPerimeter.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Enable cloud perimeter synchronization");
        //
        // lblProvider
        //
        this.lblProvider.AutoSize = true;
        this.lblProvider.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblProvider.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblProvider.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblProvider.Name = "lblProvider";
        this.lblProvider.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cloud service provider");
        //
        // comboProviderType
        //
        this.comboProviderType.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.comboProviderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboProviderType.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.comboProviderType.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.comboProviderType.Name = "comboProviderType";
        //
        // lblApiKey
        //
        this.lblApiKey.AutoSize = true;
        this.lblApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblApiKey.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblApiKey.Name = "lblApiKey";
        this.lblApiKey.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("API token / AWS profile (blank: default credentials)");
        //
        // txtApiKey
        //
        this.txtApiKey.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtApiKey.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtApiKey.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtApiKey.Name = "txtApiKey";
        this.txtApiKey.UseSystemPasswordChar = true;
        //
        // lblEndpoint
        //
        this.lblEndpoint.AutoSize = true;
        this.lblEndpoint.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblEndpoint.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblEndpoint.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblEndpoint.Name = "lblEndpoint";
        this.lblEndpoint.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Endpoint URL / Region Endpoint");
        //
        // txtEndpointUrl
        //
        this.txtEndpointUrl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtEndpointUrl.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtEndpointUrl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtEndpointUrl.Name = "txtEndpointUrl";
        //
        // lblResource
        //
        this.lblResource.AutoSize = true;
        this.lblResource.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblResource.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblResource.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblResource.Name = "lblResource";
        this.lblResource.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Primary resource (AWS IP set ARN / Azure NSG / GCP policy / CF zone)");
        //
        // txtResourceId
        //
        this.txtResourceId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtResourceId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtResourceId.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtResourceId.Name = "txtResourceId";
        //
        // lblSecondary
        //
        this.lblSecondary.AutoSize = true;
        this.lblSecondary.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblSecondary.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblSecondary.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblSecondary.Name = "lblSecondary";
        this.lblSecondary.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Secondary resource (AWS region / Azure subscription / GCP project)");
        //
        // txtSecondaryId
        //
        this.txtSecondaryId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtSecondaryId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtSecondaryId.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.txtSecondaryId.Name = "txtSecondaryId";
        //
        // lblTertiary
        //
        this.lblTertiary.AutoSize = true;
        this.lblTertiary.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblTertiary.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblTertiary.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblTertiary.Name = "lblTertiary";
        this.lblTertiary.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Third resource (AWS IP set name / Azure resource group)");
        //
        // txtTertiaryId
        //
        this.txtTertiaryId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.txtTertiaryId.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.txtTertiaryId.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.txtTertiaryId.Name = "txtTertiaryId";
        //
        // flowTest
        //
        this.flowTest.AutoSize = true;
        this.flowTest.Controls.Add(this.btnTestConnection);
        this.flowTest.Controls.Add(this.lblStatus);
        this.flowTest.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.flowTest.Name = "flowTest";
        this.flowTest.WrapContents = false;
        //
        // btnTestConnection
        //
        this.btnTestConnection.AutoSize = true;
        this.btnTestConnection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.btnTestConnection.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnTestConnection.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.btnTestConnection.MinimumSize = new System.Drawing.Size(160, 30);
        this.btnTestConnection.Name = "btnTestConnection";
        this.btnTestConnection.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Test Connection");
        //
        // lblStatus
        //
        this.lblStatus.AutoSize = true;
        this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblStatus.ForeColor = PanelCloudPerimeterSettings.BodyTextColor;
        this.lblStatus.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
        this.lblStatus.Name = "lblStatus";
        //
        // btnSave
        //
        this.btnSave.BackColor = PanelCloudPerimeterSettings.AccentColor;
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSave.ForeColor = System.Drawing.Color.White;
        this.btnSave.Margin = new System.Windows.Forms.Padding(0);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(120, 32);
        this.btnSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        //
        // PanelCloudPerimeterSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelCloudPerimeterSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowTest.ResumeLayout(false);
        this.flowTest.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.CheckBox chkEnableCloudPerimeter;
    private System.Windows.Forms.Label lblProvider;
    private System.Windows.Forms.ComboBox comboProviderType;
    private System.Windows.Forms.Label lblApiKey;
    private System.Windows.Forms.TextBox txtApiKey;
    private System.Windows.Forms.Label lblEndpoint;
    private System.Windows.Forms.TextBox txtEndpointUrl;
    private System.Windows.Forms.Label lblResource;
    private System.Windows.Forms.TextBox txtResourceId;
    private System.Windows.Forms.Label lblSecondary;
    private System.Windows.Forms.TextBox txtSecondaryId;
    private System.Windows.Forms.Label lblTertiary;
    private System.Windows.Forms.TextBox txtTertiaryId;
    private System.Windows.Forms.FlowLayoutPanel flowTest;
    private System.Windows.Forms.Button btnTestConnection;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnSave;
}
