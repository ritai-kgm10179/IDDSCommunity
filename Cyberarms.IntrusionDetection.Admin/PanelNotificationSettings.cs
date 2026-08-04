using System;
using System.Drawing;
using Cyberarms.IntrusionDetection.Shared;
using System.Windows.Forms;


namespace Cyberarms.IntrusionDetection.Admin;

public partial class PanelNotificationSettings : UserControl
{
    public event EventHandler? NotificationSettingsChanged;
    /// <summary>
    /// Initializes a new instance of the <see cref="PanelNotificationSettings"/> class.
    /// </summary>

    public PanelNotificationSettings()
    {
        InitializeComponent();
        Load += new EventHandler(PanelNotificationSettings_Load);
    }

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void PanelNotificationSettings_Load(object? sender, EventArgs e) => LoadData();

    public bool IsInEditMode { get; set; }

    /// <summary>
    /// Loads data.
    /// </summary>

    public void LoadData()
    {
        checkBoxSoftLock.Checked = NotificationSettings.Instance.OnSoftLock;
        checkBoxHardLocks.Checked = NotificationSettings.Instance.OnHardLock;
        checkBoxOnUnlock.Checked = NotificationSettings.Instance.OnUnlock;
        checkBoxDailySummary.Checked = NotificationSettings.Instance.SummaryReportDaily;
        checkBoxWeeklyReport.Checked = NotificationSettings.Instance.SummaryReportWeekly;
        checkBoxMonthlyReport.Checked = NotificationSettings.Instance.SummaryReportMonthly;
        checkBoxDailySummary.Enabled = true;
        checkBoxWeeklyReport.Enabled = true;
        checkBoxMonthlyReport.Enabled = true;
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData();
        ToggleEditMode();
    }

    /// <summary>
    /// Executes the toggle edit mode operation.
    /// </summary>

    private void ToggleEditMode()
    {
        if (!IsInEditMode)
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_delete;
            IsInEditMode = true;
        }
        else
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_edit;
            IsInEditMode = false;
        }
        pictureBoxSave.Visible = IsInEditMode;
        checkBoxSoftLock.Enabled = IsInEditMode;
        checkBoxHardLocks.Enabled = IsInEditMode;
        checkBoxOnUnlock.Enabled = IsInEditMode;
        checkBoxDailySummary.Enabled = IsInEditMode;
        checkBoxWeeklyReport.Enabled = IsInEditMode;
        checkBoxMonthlyReport.Enabled = IsInEditMode;

    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxSave_Click(object sender, EventArgs e) => ToggleEditMode();


    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X + 1, loc.Y + 1);
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X - 1, loc.Y - 1);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonSave_Click(object sender, EventArgs e)
    {
        NotificationSettings.Instance.OnSoftLock = checkBoxSoftLock.Checked;
        NotificationSettings.Instance.OnHardLock = checkBoxHardLocks.Checked;
        NotificationSettings.Instance.OnUnlock = checkBoxOnUnlock.Checked;
        NotificationSettings.Instance.SummaryReportDaily = checkBoxDailySummary.Checked;
        NotificationSettings.Instance.SummaryReportWeekly = checkBoxWeeklyReport.Checked;
        NotificationSettings.Instance.SummaryReportMonthly = checkBoxMonthlyReport.Checked;
        IddsConfig.Instance.SaveAppConfig();
        OnNotificationSettingsChanged();
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();

    /// <summary>
    /// Processes the notification settings changed notification.
    /// </summary>

    private void OnNotificationSettingsChanged() => NotificationSettingsChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Handles the key press event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void textBox_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);

    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>

    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }

    /// <summary>
    /// Handles the checked changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void checkBox_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);
}
