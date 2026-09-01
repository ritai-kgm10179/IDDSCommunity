namespace IDDSCommunity.IntrusionDetection.Admin;

partial class PanelNotificationSettings
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
        this.smartLabel5 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.smartLabel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxSoftLock = new System.Windows.Forms.CheckBox();
        this.checkBoxHardLocks = new System.Windows.Forms.CheckBox();
        this.checkBoxOnUnlock = new System.Windows.Forms.CheckBox();
        this.smartLabelSummary = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
        this.checkBoxDailySummary = new System.Windows.Forms.CheckBox();
        this.checkBoxWeeklyReport = new System.Windows.Forms.CheckBox();
        this.checkBoxMonthlyReport = new System.Windows.Forms.CheckBox();
        this.buttonSave = new System.Windows.Forms.Button();
        this.buttonDiscard = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // PanelNotificationSettings
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.AutoScroll = true;
        this.BackColor = System.Drawing.Color.White;
        this.Name = "PanelNotificationSettings";
        this.Size = new System.Drawing.Size(480, 800);
        this.ResumeLayout(false);
    }

    #endregion

    private SmartLabel smartLabel5;
    private SmartLabel smartLabel1;
    private System.Windows.Forms.CheckBox checkBoxSoftLock;
    private System.Windows.Forms.CheckBox checkBoxHardLocks;
    private System.Windows.Forms.CheckBox checkBoxOnUnlock;
    private SmartLabel smartLabelSummary;
    private System.Windows.Forms.CheckBox checkBoxDailySummary;
    private System.Windows.Forms.CheckBox checkBoxWeeklyReport;
    private System.Windows.Forms.CheckBox checkBoxMonthlyReport;
    private System.Windows.Forms.Button buttonSave;
    private System.Windows.Forms.Button buttonDiscard;
}
