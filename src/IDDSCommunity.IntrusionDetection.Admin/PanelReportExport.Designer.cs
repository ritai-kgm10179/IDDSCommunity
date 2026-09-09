namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelReportExport
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
        this.startLabel = new System.Windows.Forms.Label();
        this.start = new System.Windows.Forms.DateTimePicker();
        this.endLabel = new System.Windows.Forms.Label();
        this.end = new System.Windows.Forms.DateTimePicker();
        this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
        this.export = new System.Windows.Forms.Button();
        this.exportIso = new System.Windows.Forms.Button();
        this.exportStix = new System.Windows.Forms.Button();
        this.status = new System.Windows.Forms.Label();
        this.tableLayoutMain.SuspendLayout();
        this.flowButtons.SuspendLayout();
        this.SuspendLayout();
        //
        // tableLayoutMain
        //
        this.tableLayoutMain.AutoSize = true;
        this.tableLayoutMain.ColumnCount = 2;
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
        this.tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutMain.Controls.Add(this.pageTitle, 0, 0);
        this.tableLayoutMain.Controls.Add(this.description, 0, 1);
        this.tableLayoutMain.Controls.Add(this.startLabel, 0, 2);
        this.tableLayoutMain.Controls.Add(this.start, 1, 2);
        this.tableLayoutMain.Controls.Add(this.endLabel, 0, 3);
        this.tableLayoutMain.Controls.Add(this.end, 1, 3);
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
        this.tableLayoutMain.SetColumnSpan(this.flowButtons, 2);
        this.tableLayoutMain.SetColumnSpan(this.status, 2);
        //
        // pageTitle
        //
        this.pageTitle.AutoSize = true;
        this.pageTitle.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.pageTitle.ForeColor = System.Drawing.Color.FromArgb(15, 118, 110);
        this.pageTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.pageTitle.Name = "pageTitle";
        this.pageTitle.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Report export");
        //
        // description
        //
        this.description.AutoSize = true;
        this.description.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.description.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.description.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.description.MaximumSize = new System.Drawing.Size(560, 0);
        this.description.Name = "description";
        this.description.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export a localized HTML security report for the selected date range.");
        //
        // startLabel
        //
        this.startLabel.AutoSize = true;
        this.startLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.startLabel.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.startLabel.Margin = new System.Windows.Forms.Padding(0, 6, 8, 8);
        this.startLabel.Name = "startLabel";
        this.startLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Start date");
        //
        // start
        //
        this.start.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.start.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.start.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
        this.start.Name = "start";
        this.start.Width = 160;
        //
        // endLabel
        //
        this.endLabel.AutoSize = true;
        this.endLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.endLabel.ForeColor = System.Drawing.Color.FromArgb(102, 102, 102);
        this.endLabel.Margin = new System.Windows.Forms.Padding(0, 6, 8, 14);
        this.endLabel.Name = "endLabel";
        this.endLabel.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("End date");
        //
        // end
        //
        this.end.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
        this.end.Format = System.Windows.Forms.DateTimePickerFormat.Short;
        this.end.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
        this.end.Name = "end";
        this.end.Width = 160;
        //
        // flowButtons
        //
        this.flowButtons.AutoSize = true;
        this.flowButtons.Controls.Add(this.export);
        this.flowButtons.Controls.Add(this.exportIso);
        this.flowButtons.Controls.Add(this.exportStix);
        this.flowButtons.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
        this.flowButtons.Name = "flowButtons";
        this.flowButtons.WrapContents = true;
        //
        // export
        //
        this.export.AutoSize = true;
        this.export.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.export.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.export.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.export.MinimumSize = new System.Drawing.Size(180, 30);
        this.export.Name = "export";
        this.export.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export HTML report");
        //
        // exportIso
        //
        this.exportIso.AutoSize = true;
        this.exportIso.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.exportIso.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.exportIso.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.exportIso.MinimumSize = new System.Drawing.Size(180, 30);
        this.exportIso.Name = "exportIso";
        this.exportIso.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export ISO 27001 report");
        //
        // exportStix
        //
        this.exportStix.AutoSize = true;
        this.exportStix.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
        this.exportStix.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.exportStix.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
        this.exportStix.MinimumSize = new System.Drawing.Size(180, 30);
        this.exportStix.Name = "exportStix";
        this.exportStix.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export STIX 2.1 bundle");
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
        // PanelReportExport
        //
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.tableLayoutMain);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelReportExport";
        this.tableLayoutMain.ResumeLayout(false);
        this.tableLayoutMain.PerformLayout();
        this.flowButtons.ResumeLayout(false);
        this.flowButtons.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
    private System.Windows.Forms.Label pageTitle;
    private System.Windows.Forms.Label description;
    private System.Windows.Forms.Label startLabel;
    private System.Windows.Forms.DateTimePicker start;
    private System.Windows.Forms.Label endLabel;
    private System.Windows.Forms.DateTimePicker end;
    private System.Windows.Forms.FlowLayoutPanel flowButtons;
    private System.Windows.Forms.Button export;
    private System.Windows.Forms.Button exportIso;
    private System.Windows.Forms.Button exportStix;
    private System.Windows.Forms.Label status;
}
