using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class PanelLockoutConfiguration : UserControl
{

    public event EventHandler? LockoutConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PanelLockoutConfiguration"/> class.
    /// </summary>

    public PanelLockoutConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        LoadData();
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X + 1, pictureBoxEdit.Location.Y + 1);

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X - 1, pictureBoxEdit.Location.Y - 1);

    public bool IsInEditMode { get; set; }

    /// <summary>
    /// Loads data.
    /// </summary>

    private void LoadData()
    {
        textBoxHardLocks.Text = IddsConfig.Instance.HardLockAttempts.ToString();
        textBoxHardLockDuration.Text = IddsConfig.Instance.HardLockTimeHours.ToString();
        textBoxSoftLockDuration.Text = IddsConfig.Instance.SoftLockTimeMinutes.ToString();
        textBoxSoftLocks.Text = IddsConfig.Instance.SoftLockAttempts.ToString();
        checkBoxLockForever.Checked = IddsConfig.Instance.LockForever;
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        //if (IsInEditMode) LoadData();
        //ToggleEditMode();
        //ClearErrors();
    }

    /// <summary>
    /// Executes the toggle edit mode operation.
    /// </summary>

    private static void ToggleEditMode()
    {
        //if (!IsInEditMode) {
        //    pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_delete;
        //    IsInEditMode = true;
        //} else {
        //    pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_edit;
        //    IsInEditMode = false;
        //}
        //pictureBoxSave.Visible = IsInEditMode;
        //textBoxHardLockDuration.Enabled = IsInEditMode;
        //textBoxHardLocks.Enabled = IsInEditMode;
        //textBoxSoftLockDuration.Enabled = IsInEditMode;
        //textBoxSoftLocks.Enabled = IsInEditMode;
        //checkBoxLockForever.Enabled = IsInEditMode;
    }

    /// <summary>
    /// Clears errors.
    /// </summary>

    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxSave_Click(object sender, EventArgs e)
    {
        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxHardLocks.Text, out int hardLocks))
        {
            errHardLocks.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxHardLockDuration.Text, out int hardLockDuration))
        {
            errHardLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLockDuration.Text, out int softLockDuration))
        {
            errSoftLockDuration.Visible = true;
            hasError = true;
        }
        if (!int.TryParse(textBoxSoftLocks.Text, out int softLocks))
        {
            errSoftLocks.Visible = true;
            hasError = true;
        }
        if (!hasError)
        {
            IddsConfig.Instance.LockForever = checkBoxLockForever.Checked;
            IddsConfig.Instance.HardLockAttempts = hardLocks;
            IddsConfig.Instance.HardLockTimeHours = hardLockDuration;
            IddsConfig.Instance.SoftLockAttempts = softLocks;
            IddsConfig.Instance.SoftLockTimeMinutes = softLockDuration;
            IddsConfig.Instance.Save();
            ToggleEditMode();
            OnLockoutConfigurationChanged();
        }
        SetEditMode(false);
    }

    /// <summary>
    /// Processes the lockout configuration changed notification.
    /// </summary>

    private void OnLockoutConfigurationChanged() => LockoutConfigurationChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonDiscard_Click(object sender, EventArgs e)
    {
        LoadData();
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the key press event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void textBoxSoftLocks_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);

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

    private void checkBoxLockForever_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);
}
