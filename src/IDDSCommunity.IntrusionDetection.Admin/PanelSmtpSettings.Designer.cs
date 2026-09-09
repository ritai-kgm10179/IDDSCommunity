namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelSmtpSettings
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
        this.pictureBoxEdit = new System.Windows.Forms.PictureBox();
        this.pictureBoxSave = new System.Windows.Forms.PictureBox();
        this.smartLabel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxSender = new System.Windows.Forms.TextBox();
        this.smartLabel2 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxRecipient = new System.Windows.Forms.TextBox();
        this.smartLabel3 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxSmtpServer = new System.Windows.Forms.TextBox();
        this.smartLabel8 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.flowSmtpPort = new System.Windows.Forms.FlowLayoutPanel();
        this.textBoxSmtpPort = new System.Windows.Forms.TextBox();
        this.errSmtpPort = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxUseSSL = new System.Windows.Forms.CheckBox();
        this.checkBoxAuthentication = new System.Windows.Forms.CheckBox();
        this.smartLabel4 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxUsername = new System.Windows.Forms.TextBox();
        this.smartLabel6 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.textBoxPassword = new System.Windows.Forms.TextBox();
        this.flowTest = new System.Windows.Forms.FlowLayoutPanel();
        this.buttonTestSmtpSettings = new System.Windows.Forms.Button();
        this.smartLabelTestError = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.buttonSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        this.headerPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEdit)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSave)).BeginInit();
        this.flowSmtpPort.SuspendLayout();
        this.flowTest.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.headerPanel, 0, 0);
        this.tableLayoutMain.Controls.Add(this.smartLabel1, 0, 1);
        this.tableLayoutMain.Controls.Add(this.textBoxSender, 0, 2);
        this.tableLayoutMain.Controls.Add(this.smartLabel2, 0, 3);
        this.tableLayoutMain.Controls.Add(this.textBoxRecipient, 0, 4);
        this.tableLayoutMain.Controls.Add(this.smartLabel3, 0, 5);
        this.tableLayoutMain.Controls.Add(this.textBoxSmtpServer, 0, 6);
        this.tableLayoutMain.Controls.Add(this.smartLabel8, 0, 7);
        this.tableLayoutMain.Controls.Add(this.flowSmtpPort, 0, 8);
        this.tableLayoutMain.Controls.Add(this.checkBoxUseSSL, 0, 9);
        this.tableLayoutMain.Controls.Add(this.checkBoxAuthentication, 0, 10);
        this.tableLayoutMain.Controls.Add(this.smartLabel4, 0, 11);
        this.tableLayoutMain.Controls.Add(this.textBoxUsername, 0, 12);
        this.tableLayoutMain.Controls.Add(this.smartLabel6, 0, 13);
        this.tableLayoutMain.Controls.Add(this.textBoxPassword, 0, 14);
        this.tableLayoutMain.Controls.Add(this.flowTest, 0, 15);
        this.tableLayoutMain.Controls.Add(this.smartLabelTestError, 0, 16);
        this.tableLayoutMain.Controls.Add(this.buttonSave, 0, 17);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 18;
        for (int i = 0; i < 18; i++)
        {
            this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        }
        //
        // headerPanel
        //
        this.headerPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.headerPanel.Controls.Add(this.smartLabel5);
        this.headerPanel.Controls.Add(this.pictureBoxSave);
        this.headerPanel.Controls.Add(this.pictureBoxEdit);
        this.headerPanel.Height = 34;
        this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.headerPanel.Name = "headerPanel";
        //
        // smartLabel5
        //
        this.smartLabel5.AutoSize = true;
        this.smartLabel5.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.smartLabel5.ForeColor = System.Drawing.Color.FromArgb(19, 184, 166);
        this.smartLabel5.Location = new System.Drawing.Point(0, 4);
        this.smartLabel5.Margin = new System.Windows.Forms.Padding(0);
        this.smartLabel5.Name = "smartLabel5";
        this.smartLabel5.Selected = false;
        this.smartLabel5.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel5.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SMTP server configuration");
        this.smartLabel5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // pictureBoxEdit
        //
        this.pictureBoxEdit.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        this.pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_edit;
        this.pictureBoxEdit.Location = new System.Drawing.Point(438, 0);
        this.pictureBoxEdit.Name = "pictureBoxEdit";
        this.pictureBoxEdit.Size = new System.Drawing.Size(25, 25);
        this.pictureBoxEdit.TabStop = false;
        this.pictureBoxEdit.Visible = false;
        this.pictureBoxEdit.Click += new System.EventHandler(this.pictureBoxEdit_Click);
        this.pictureBoxEdit.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
        this.pictureBoxEdit.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
        //
        // pictureBoxSave
        //
        this.pictureBoxSave.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        this.pictureBoxSave.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_save;
        this.pictureBoxSave.Location = new System.Drawing.Point(407, 0);
        this.pictureBoxSave.Name = "pictureBoxSave";
        this.pictureBoxSave.Size = new System.Drawing.Size(25, 25);
        this.pictureBoxSave.TabStop = false;
        this.pictureBoxSave.Visible = false;
        this.pictureBoxSave.Click += new System.EventHandler(this.pictureBoxSave_Click);
        this.pictureBoxSave.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
        this.pictureBoxSave.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
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
        this.smartLabel1.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Sender address");
        //
        // textBoxSender
        //
        this.textBoxSender.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxSender.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSender.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxSender.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.textBoxSender.Name = "textBoxSender";
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
        this.smartLabel2.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Recipient address");
        //
        // textBoxRecipient
        //
        this.textBoxRecipient.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxRecipient.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxRecipient.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxRecipient.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.textBoxRecipient.Name = "textBoxRecipient";
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
        this.smartLabel3.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SMTP server");
        //
        // textBoxSmtpServer
        //
        this.textBoxSmtpServer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxSmtpServer.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSmtpServer.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxSmtpServer.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.textBoxSmtpServer.Name = "textBoxSmtpServer";
        //
        // smartLabel8
        //
        this.smartLabel8.AutoSize = true;
        this.smartLabel8.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel8.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel8.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel8.Name = "smartLabel8";
        this.smartLabel8.Selected = false;
        this.smartLabel8.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel8.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SMTP/SSL Port");
        //
        // flowSmtpPort
        //
        this.flowSmtpPort.AutoSize = true;
        this.flowSmtpPort.Controls.Add(this.textBoxSmtpPort);
        this.flowSmtpPort.Controls.Add(this.errSmtpPort);
        this.flowSmtpPort.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.flowSmtpPort.Name = "flowSmtpPort";
        this.flowSmtpPort.WrapContents = false;
        //
        // textBoxSmtpPort
        //
        this.textBoxSmtpPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxSmtpPort.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxSmtpPort.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.textBoxSmtpPort.Name = "textBoxSmtpPort";
        this.textBoxSmtpPort.Size = new System.Drawing.Size(150, 23);
        //
        // errSmtpPort
        //
        this.errSmtpPort.AutoSize = true;
        this.errSmtpPort.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.errSmtpPort.ForeColor = System.Drawing.Color.FromArgb(225, 50, 50);
        this.errSmtpPort.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.errSmtpPort.Name = "errSmtpPort";
        this.errSmtpPort.Selected = false;
        this.errSmtpPort.SelectedColor = System.Drawing.Color.Empty;
        this.errSmtpPort.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("value must be a number");
        this.errSmtpPort.Visible = false;
        //
        // checkBoxUseSSL
        //
        this.checkBoxUseSSL.AutoSize = true;
        this.checkBoxUseSSL.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxUseSSL.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.checkBoxUseSSL.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.checkBoxUseSSL.Name = "checkBoxUseSSL";
        this.checkBoxUseSSL.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Use SSL for communication");
        this.checkBoxUseSSL.UseVisualStyleBackColor = true;
        //
        // checkBoxAuthentication
        //
        this.checkBoxAuthentication.AutoSize = true;
        this.checkBoxAuthentication.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkBoxAuthentication.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.checkBoxAuthentication.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.checkBoxAuthentication.Name = "checkBoxAuthentication";
        this.checkBoxAuthentication.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("This server requires authentication");
        this.checkBoxAuthentication.UseVisualStyleBackColor = true;
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
        this.smartLabel4.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Username");
        //
        // textBoxUsername
        //
        this.textBoxUsername.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxUsername.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxUsername.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxUsername.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.textBoxUsername.Name = "textBoxUsername";
        //
        // smartLabel6
        //
        this.smartLabel6.AutoSize = true;
        this.smartLabel6.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabel6.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.smartLabel6.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.smartLabel6.Name = "smartLabel6";
        this.smartLabel6.Selected = false;
        this.smartLabel6.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabel6.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Password");
        //
        // textBoxPassword
        //
        this.textBoxPassword.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.textBoxPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.textBoxPassword.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.textBoxPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.textBoxPassword.Name = "textBoxPassword";
        this.textBoxPassword.PasswordChar = '*';
        //
        // flowTest
        //
        this.flowTest.AutoSize = true;
        this.flowTest.Controls.Add(this.buttonTestSmtpSettings);
        this.flowTest.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
        this.flowTest.Name = "flowTest";
        this.flowTest.WrapContents = false;
        //
        // buttonTestSmtpSettings
        //
        this.buttonTestSmtpSettings.AutoSize = true;
        this.buttonTestSmtpSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.buttonTestSmtpSettings.BackColor = System.Drawing.Color.White;
        this.buttonTestSmtpSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonTestSmtpSettings.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.buttonTestSmtpSettings.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.buttonTestSmtpSettings.Margin = new System.Windows.Forms.Padding(0);
        this.buttonTestSmtpSettings.MinimumSize = new System.Drawing.Size(100, 28);
        this.buttonTestSmtpSettings.Name = "buttonTestSmtpSettings";
        this.buttonTestSmtpSettings.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Test");
        this.buttonTestSmtpSettings.UseVisualStyleBackColor = false;
        this.buttonTestSmtpSettings.Click += new System.EventHandler(this.buttonTestSmtpSettings_Click);
        //
        // smartLabelTestError
        //
        this.smartLabelTestError.AutoSize = true;
        this.smartLabelTestError.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.smartLabelTestError.ForeColor = System.Drawing.Color.FromArgb(225, 50, 50);
        this.smartLabelTestError.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.smartLabelTestError.MaximumSize = new System.Drawing.Size(420, 0);
        this.smartLabelTestError.Name = "smartLabelTestError";
        this.smartLabelTestError.Selected = false;
        this.smartLabelTestError.SelectedColor = System.Drawing.Color.Empty;
        this.smartLabelTestError.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Smtp test error");
        this.smartLabelTestError.Visible = false;
        //
        // buttonSave
        //
        this.buttonSave.BackColor = System.Drawing.Color.FromArgb(19, 184, 166);
        this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.buttonSave.ForeColor = System.Drawing.Color.White;
        this.buttonSave.Margin = new System.Windows.Forms.Padding(0);
        this.buttonSave.Name = "buttonSave";
        this.buttonSave.Size = new System.Drawing.Size(120, 32);
        this.buttonSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        this.buttonSave.UseVisualStyleBackColor = false;
        this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
        //
        // PanelSmtpSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelSmtpSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.headerPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEdit)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSave)).EndInit();
        this.flowSmtpPort.ResumeLayout(false);
        this.flowSmtpPort.PerformLayout();
        this.flowTest.ResumeLayout(false);
        this.flowTest.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Panel headerPanel;
    private SmartLabel smartLabel5;
    private System.Windows.Forms.PictureBox pictureBoxEdit;
    private System.Windows.Forms.PictureBox pictureBoxSave;
    private SmartLabel smartLabel1;
    private System.Windows.Forms.TextBox textBoxSender;
    private SmartLabel smartLabel2;
    private System.Windows.Forms.TextBox textBoxRecipient;
    private SmartLabel smartLabel3;
    private System.Windows.Forms.TextBox textBoxSmtpServer;
    private SmartLabel smartLabel8;
    private System.Windows.Forms.FlowLayoutPanel flowSmtpPort;
    private System.Windows.Forms.TextBox textBoxSmtpPort;
    private SmartLabel errSmtpPort;
    private System.Windows.Forms.CheckBox checkBoxUseSSL;
    private System.Windows.Forms.CheckBox checkBoxAuthentication;
    private SmartLabel smartLabel4;
    private System.Windows.Forms.TextBox textBoxUsername;
    private SmartLabel smartLabel6;
    private System.Windows.Forms.TextBox textBoxPassword;
    private System.Windows.Forms.FlowLayoutPanel flowTest;
    private System.Windows.Forms.Button buttonTestSmtpSettings;
    private SmartLabel smartLabelTestError;
    private System.Windows.Forms.Button buttonSave;
}
