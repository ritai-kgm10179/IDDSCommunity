namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelConfigurationTransfer
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
        this.pageTitle = new System.Windows.Forms.Label();
        this.description = new System.Windows.Forms.Label();
        this.includeSecrets = new System.Windows.Forms.CheckBox();
        this.passphraseLabel = new System.Windows.Forms.Label();
        this.passphrase = new System.Windows.Forms.TextBox();
        this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
        this.exportButton = new System.Windows.Forms.Button();
        this.importButton = new System.Windows.Forms.Button();
        this.status = new System.Windows.Forms.Label();
        this.tableLayoutMain.SuspendLayout();
        this.flowButtons.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 2;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.pageTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.description, 0, 1);
        this.tableLayoutMain.Controls.Add(this.includeSecrets, 0, 2);
        this.tableLayoutMain.Controls.Add(this.passphraseLabel, 0, 3);
        this.tableLayoutMain.Controls.Add(this.passphrase, 1, 3);
        this.tableLayoutMain.Controls.Add(this.flowButtons, 0, 4);
        this.tableLayoutMain.Controls.Add(this.status, 0, 5);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 6;
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.SetColumnSpan(this.pageTitle, 2);
        this.tableLayoutMain.SetColumnSpan(this.description, 2);
        this.tableLayoutMain.SetColumnSpan(this.includeSecrets, 2);
        this.tableLayoutMain.SetColumnSpan(this.flowButtons, 2);
        this.tableLayoutMain.SetColumnSpan(this.status, 2);
        //
        // pageTitle
        //
        this.pageTitle.AutoSize = true;
        this.pageTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.pageTitle.ForeColor = System.Drawing.Color.FromArgb(19, 184, 166);
        this.pageTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.pageTitle.Name = "pageTitle";
        this.pageTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Configuration import and export");
        //
        // description
        //
        this.description.AutoSize = true;
        this.description.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.description.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.description.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.description.MaximumSize = new System.Drawing.Size(560, 0);
        this.description.Name = "description";
        this.description.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Transfer policies, safe networks, application settings, and Agent settings using a versioned JSON package.");
        //
        // includeSecrets
        //
        this.includeSecrets.AutoSize = true;
        this.includeSecrets.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.includeSecrets.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.includeSecrets.Name = "includeSecrets";
        this.includeSecrets.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Include encrypted SMTP password");
        //
        // passphraseLabel
        //
        this.passphraseLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.passphraseLabel.AutoSize = true;
        this.passphraseLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.passphraseLabel.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.passphraseLabel.Margin = new System.Windows.Forms.Padding(0, 6, 8, 10);
        this.passphraseLabel.Name = "passphraseLabel";
        this.passphraseLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Package passphrase");
        //
        // passphrase
        //
        this.passphrase.Dock = System.Windows.Forms.DockStyle.Fill;
        this.passphrase.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.passphrase.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
        this.passphrase.Name = "passphrase";
        this.passphrase.PasswordChar = '●';
        //
        // flowButtons
        //
        this.flowButtons.AutoSize = true;
        this.flowButtons.Controls.Add(this.exportButton);
        this.flowButtons.Controls.Add(this.importButton);
        this.flowButtons.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.flowButtons.Name = "flowButtons";
        this.flowButtons.WrapContents = false;
        //
        // exportButton
        //
        this.exportButton.BackColor = System.Drawing.Color.White;
        this.exportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.exportButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.exportButton.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.exportButton.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.exportButton.Name = "exportButton";
        this.exportButton.Size = new System.Drawing.Size(160, 30);
        this.exportButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export settings");
        this.exportButton.UseVisualStyleBackColor = false;
        //
        // importButton
        //
        this.importButton.BackColor = System.Drawing.Color.White;
        this.importButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.importButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.importButton.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.importButton.Margin = new System.Windows.Forms.Padding(0);
        this.importButton.Name = "importButton";
        this.importButton.Size = new System.Drawing.Size(160, 30);
        this.importButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Import settings");
        this.importButton.UseVisualStyleBackColor = false;
        //
        // status
        //
        this.status.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.status.AutoSize = false;
        this.status.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.status.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.status.Margin = new System.Windows.Forms.Padding(0);
        this.status.Name = "status";
        this.status.Size = new System.Drawing.Size(560, 80);
        //
        // PanelConfigurationTransfer
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelConfigurationTransfer";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowButtons.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label pageTitle;
    private System.Windows.Forms.Label description;
    private System.Windows.Forms.CheckBox includeSecrets;
    private System.Windows.Forms.Label passphraseLabel;
    private System.Windows.Forms.TextBox passphrase;
    private System.Windows.Forms.FlowLayoutPanel flowButtons;
    private System.Windows.Forms.Button exportButton;
    private System.Windows.Forms.Button importButton;
    private System.Windows.Forms.Label status;
}
