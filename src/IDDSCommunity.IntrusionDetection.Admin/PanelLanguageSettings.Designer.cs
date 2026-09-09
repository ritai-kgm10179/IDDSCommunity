namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelLanguageSettings
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
        this.pageTitle = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.fieldLabel = new System.Windows.Forms.Label();
        this.languageSelector = new System.Windows.Forms.ComboBox();
        this.restartNotice = new System.Windows.Forms.Label();
        this.buttonSave = new System.Windows.Forms.Button();
        this.tableLayoutMain.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.pageTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.fieldLabel, 0, 1);
        this.tableLayoutMain.Controls.Add(this.languageSelector, 0, 2);
        this.tableLayoutMain.Controls.Add(this.restartNotice, 0, 3);
        this.tableLayoutMain.Controls.Add(this.buttonSave, 0, 4);
        this.tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Top;
        this.tableLayoutMain.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutMain.Name = "tableLayoutMain";
        this.tableLayoutMain.Padding = new System.Windows.Forms.Padding(15);
        this.tableLayoutMain.RowCount = 5;
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        //
        // pageTitle
        //
        this.pageTitle.AutoSize = true;
        this.pageTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.pageTitle.ForeColor = PanelLanguageSettings.AccentColor;
        this.pageTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.pageTitle.Name = "pageTitle";
        this.pageTitle.Selected = false;
        this.pageTitle.SelectedColor = System.Drawing.Color.Empty;
        this.pageTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Language settings");
        //
        // fieldLabel
        //
        this.fieldLabel.AutoSize = true;
        this.fieldLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.fieldLabel.ForeColor = PanelLanguageSettings.BodyTextColor;
        this.fieldLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.fieldLabel.Name = "fieldLabel";
        this.fieldLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Display language");
        //
        // languageSelector
        //
        this.languageSelector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
        this.languageSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.languageSelector.DropDownWidth = 240;
        this.languageSelector.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.languageSelector.ForeColor = PanelLanguageSettings.BodyTextColor;
        this.languageSelector.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.languageSelector.Name = "languageSelector";
        this.languageSelector.Size = new System.Drawing.Size(240, 23);
        //
        // restartNotice
        //
        this.restartNotice.AutoSize = true;
        this.restartNotice.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.restartNotice.ForeColor = PanelLanguageSettings.BodyTextColor;
        this.restartNotice.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
        this.restartNotice.MaximumSize = new System.Drawing.Size(420, 0);
        this.restartNotice.Name = "restartNotice";
        this.restartNotice.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Restart the application after saving to apply the language to every open window.");
        //
        // buttonSave
        //
        this.buttonSave.BackColor = PanelLanguageSettings.AccentColor;
        this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.buttonSave.ForeColor = System.Drawing.Color.White;
        this.buttonSave.Margin = new System.Windows.Forms.Padding(0);
        this.buttonSave.Name = "buttonSave";
        this.buttonSave.Size = new System.Drawing.Size(120, 32);
        this.buttonSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
        //
        // PanelLanguageSettings
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelLanguageSettings";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private SmartLabel pageTitle;
    private System.Windows.Forms.Label fieldLabel;
    private System.Windows.Forms.ComboBox languageSelector;
    private System.Windows.Forms.Label restartNotice;
    private System.Windows.Forms.Button buttonSave;
}
