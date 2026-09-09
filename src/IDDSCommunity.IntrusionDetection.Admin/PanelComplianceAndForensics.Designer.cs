namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelComplianceAndForensics
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
        this.topPanel = new System.Windows.Forms.Panel();
        this.topPanelStack = new System.Windows.Forms.FlowLayoutPanel();
        this.title = new System.Windows.Forms.Label();
        this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
        this.btnRunScan = new System.Windows.Forms.Button();
        this.btnExportReport = new System.Windows.Forms.Button();
        this.lblScore = new System.Windows.Forms.Label();
        this.listChecks = new System.Windows.Forms.ListView();
        this.topPanel.SuspendLayout();
        this.topPanelStack.SuspendLayout();
        this.flowButtons.SuspendLayout();
        this.SuspendLayout();
        //
        // topPanel
        //
        this.topPanel.Controls.Add(this.topPanelStack);
        this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.topPanel.Height = 130;
        this.topPanel.Name = "topPanel";
        this.topPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 10);
        //
        // topPanelStack
        //
        this.topPanelStack.AutoSize = true;
        this.topPanelStack.Controls.Add(this.title);
        this.topPanelStack.Controls.Add(this.flowButtons);
        this.topPanelStack.Controls.Add(this.lblScore);
        this.topPanelStack.Dock = System.Windows.Forms.DockStyle.Fill;
        this.topPanelStack.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
        this.topPanelStack.Name = "topPanelStack";
        this.topPanelStack.WrapContents = false;
        //
        // title
        //
        this.title.AutoSize = true;
        this.title.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.title.ForeColor = PanelComplianceAndForensics.AccentColor;
        this.title.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.title.Name = "title";
        this.title.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("CIS Windows Server Benchmark & Forensics");
        //
        // flowButtons
        //
        this.flowButtons.AutoSize = true;
        this.flowButtons.Controls.Add(this.btnRunScan);
        this.flowButtons.Controls.Add(this.btnExportReport);
        this.flowButtons.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
        this.flowButtons.Name = "flowButtons";
        this.flowButtons.WrapContents = false;
        //
        // btnRunScan
        //
        this.btnRunScan.BackColor = PanelComplianceAndForensics.AccentColor;
        this.btnRunScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRunScan.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnRunScan.ForeColor = System.Drawing.Color.White;
        this.btnRunScan.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
        this.btnRunScan.Name = "btnRunScan";
        this.btnRunScan.Size = new System.Drawing.Size(160, 32);
        this.btnRunScan.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Run CIS Benchmark Scan");
        //
        // btnExportReport
        //
        this.btnExportReport.BackColor = System.Drawing.Color.White;
        this.btnExportReport.Enabled = false;
        this.btnExportReport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnExportReport.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnExportReport.ForeColor = PanelComplianceAndForensics.BodyTextColor;
        this.btnExportReport.Margin = new System.Windows.Forms.Padding(0);
        this.btnExportReport.Name = "btnExportReport";
        this.btnExportReport.Size = new System.Drawing.Size(160, 32);
        this.btnExportReport.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Export Report");
        //
        // lblScore
        //
        this.lblScore.AutoSize = true;
        this.lblScore.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblScore.ForeColor = PanelComplianceAndForensics.BodyTextColor;
        this.lblScore.Name = "lblScore";
        this.lblScore.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Scan not executed");
        //
        // listChecks
        //
        this.listChecks.Dock = System.Windows.Forms.DockStyle.Fill;
        this.listChecks.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.listChecks.FullRowSelect = true;
        this.listChecks.GridLines = true;
        this.listChecks.Name = "listChecks";
        this.listChecks.UseCompatibleStateImageBehavior = false;
        this.listChecks.View = System.Windows.Forms.View.Details;
        //
        // PanelComplianceAndForensics
        //
        this.BackColor = System.Drawing.Color.White;
        this.Controls.Add(this.listChecks);
        this.Controls.Add(this.topPanel);
        this.Dock = System.Windows.Forms.DockStyle.Fill;
        this.Name = "PanelComplianceAndForensics";
        this.topPanel.ResumeLayout(false);
        this.topPanel.PerformLayout();
        this.topPanelStack.ResumeLayout(false);
        this.topPanelStack.PerformLayout();
        this.flowButtons.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel topPanel;
    private System.Windows.Forms.FlowLayoutPanel topPanelStack;
    private System.Windows.Forms.Label title;
    private System.Windows.Forms.FlowLayoutPanel flowButtons;
    private System.Windows.Forms.Button btnRunScan;
    private System.Windows.Forms.Button btnExportReport;
    private System.Windows.Forms.Label lblScore;
    private System.Windows.Forms.ListView listChecks;
}
