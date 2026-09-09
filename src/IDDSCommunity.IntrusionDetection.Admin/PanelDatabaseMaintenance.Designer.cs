namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelDatabaseMaintenance
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
        this.statusLabel = new System.Windows.Forms.Label();
        this.flowActionButtons = new System.Windows.Forms.FlowLayoutPanel();
        this.checkButton = new System.Windows.Forms.Button();
        this.backupButton = new System.Windows.Forms.Button();
        this.optimizeButton = new System.Windows.Forms.Button();
        this.purgeButton = new System.Windows.Forms.Button();
        this.restoreButton = new System.Windows.Forms.Button();
        this.compactButton = new System.Windows.Forms.Button();
        this.verifyButton = new System.Windows.Forms.Button();
        this.backupListLabel = new System.Windows.Forms.Label();
        this.backupList = new System.Windows.Forms.ListBox();
        this.historyListLabel = new System.Windows.Forms.Label();
        this.historyList = new System.Windows.Forms.ListBox();
        this.tableLayoutMain.SuspendLayout();
        this.flowActionButtons.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.pageTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.description, 0, 1);
        this.tableLayoutMain.Controls.Add(this.statusLabel, 0, 2);
        this.tableLayoutMain.Controls.Add(this.flowActionButtons, 0, 3);
        this.tableLayoutMain.Controls.Add(this.backupListLabel, 0, 4);
        this.tableLayoutMain.Controls.Add(this.backupList, 0, 5);
        this.tableLayoutMain.Controls.Add(this.historyListLabel, 0, 6);
        this.tableLayoutMain.Controls.Add(this.historyList, 0, 7);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 8;
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85F));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85F));
        //
        // pageTitle
        //
        this.pageTitle.AutoSize = true;
        this.pageTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.pageTitle.ForeColor = System.Drawing.Color.FromArgb(15, 118, 110);
        this.pageTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.pageTitle.Name = "pageTitle";
        this.pageTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database maintenance");
        //
        // description
        //
        this.description.AutoSize = true;
        this.description.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.description.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.description.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.description.MaximumSize = new System.Drawing.Size(620, 0);
        this.description.Name = "description";
        this.description.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("SQLite database health, verified backups, retention cleanup, and safe optimization.");
        //
        // statusLabel
        //
        this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.statusLabel.AutoSize = false;
        this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.statusLabel.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.statusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new System.Drawing.Size(620, 73);
        this.statusLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Reading database status...");
        //
        // flowActionButtons
        //
        this.flowActionButtons.AutoSize = true;
        this.flowActionButtons.Controls.Add(this.checkButton);
        this.flowActionButtons.Controls.Add(this.backupButton);
        this.flowActionButtons.Controls.Add(this.optimizeButton);
        this.flowActionButtons.Controls.Add(this.purgeButton);
        this.flowActionButtons.Controls.Add(this.restoreButton);
        this.flowActionButtons.Controls.Add(this.compactButton);
        this.flowActionButtons.Controls.Add(this.verifyButton);
        this.flowActionButtons.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.flowActionButtons.Name = "flowActionButtons";
        this.flowActionButtons.WrapContents = true;
        //
        // checkButton
        //
        this.checkButton.AutoSize = true;
        this.checkButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.checkButton.BackColor = System.Drawing.Color.White;
        this.checkButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.checkButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.checkButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.checkButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.checkButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.checkButton.Name = "checkButton";
        this.checkButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Run integrity check");
        this.checkButton.UseVisualStyleBackColor = false;
        //
        // backupButton
        //
        this.backupButton.AutoSize = true;
        this.backupButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.backupButton.BackColor = System.Drawing.Color.White;
        this.backupButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.backupButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.backupButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.backupButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.backupButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.backupButton.Name = "backupButton";
        this.backupButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Create verified backup");
        this.backupButton.UseVisualStyleBackColor = false;
        //
        // optimizeButton
        //
        this.optimizeButton.AutoSize = true;
        this.optimizeButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.optimizeButton.BackColor = System.Drawing.Color.White;
        this.optimizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.optimizeButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.optimizeButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.optimizeButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.optimizeButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.optimizeButton.Name = "optimizeButton";
        this.optimizeButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Optimize database");
        this.optimizeButton.UseVisualStyleBackColor = false;
        //
        // purgeButton
        //
        this.purgeButton.AutoSize = true;
        this.purgeButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.purgeButton.BackColor = System.Drawing.Color.White;
        this.purgeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.purgeButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.purgeButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.purgeButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.purgeButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.purgeButton.Name = "purgeButton";
        this.purgeButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Clean expired data");
        this.purgeButton.UseVisualStyleBackColor = false;
        //
        // restoreButton
        //
        this.restoreButton.AutoSize = true;
        this.restoreButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.restoreButton.BackColor = System.Drawing.Color.White;
        this.restoreButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.restoreButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.restoreButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.restoreButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.restoreButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.restoreButton.Name = "restoreButton";
        this.restoreButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Restore backup");
        this.restoreButton.UseVisualStyleBackColor = false;
        //
        // compactButton
        //
        this.compactButton.AutoSize = true;
        this.compactButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.compactButton.BackColor = System.Drawing.Color.White;
        this.compactButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.compactButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.compactButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.compactButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.compactButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.compactButton.Name = "compactButton";
        this.compactButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Reclaim database space");
        this.compactButton.UseVisualStyleBackColor = false;
        //
        // verifyButton
        //
        this.verifyButton.AutoSize = true;
        this.verifyButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.verifyButton.BackColor = System.Drawing.Color.White;
        this.verifyButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.verifyButton.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.verifyButton.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.verifyButton.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.verifyButton.MinimumSize = new System.Drawing.Size(170, 28);
        this.verifyButton.Name = "verifyButton";
        this.verifyButton.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Verify selected backup");
        this.verifyButton.UseVisualStyleBackColor = false;
        //
        // backupListLabel
        //
        this.backupListLabel.AutoSize = true;
        this.backupListLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.backupListLabel.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.backupListLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.backupListLabel.Name = "backupListLabel";
        this.backupListLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Verified backups");
        //
        // backupList
        //
        this.backupList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.backupList.Dock = System.Windows.Forms.DockStyle.Fill;
        this.backupList.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.backupList.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.backupList.Name = "backupList";
        //
        // historyListLabel
        //
        this.historyListLabel.AutoSize = true;
        this.historyListLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.historyListLabel.ForeColor = PanelDatabaseMaintenance.BodyTextColor;
        this.historyListLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.historyListLabel.Name = "historyListLabel";
        this.historyListLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Maintenance history");
        //
        // historyList
        //
        this.historyList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.historyList.Dock = System.Windows.Forms.DockStyle.Fill;
        this.historyList.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.historyList.Margin = new System.Windows.Forms.Padding(0);
        this.historyList.Name = "historyList";
        //
        // PanelDatabaseMaintenance
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelDatabaseMaintenance";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowActionButtons.ResumeLayout(false);
        this.flowActionButtons.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label pageTitle;
    private System.Windows.Forms.Label description;
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.FlowLayoutPanel flowActionButtons;
    private System.Windows.Forms.Button checkButton;
    private System.Windows.Forms.Button backupButton;
    private System.Windows.Forms.Button optimizeButton;
    private System.Windows.Forms.Button purgeButton;
    private System.Windows.Forms.Button restoreButton;
    private System.Windows.Forms.Button compactButton;
    private System.Windows.Forms.Button verifyButton;
    private System.Windows.Forms.Label backupListLabel;
    private System.Windows.Forms.ListBox backupList;
    private System.Windows.Forms.Label historyListLabel;
    private System.Windows.Forms.ListBox historyList;
}
