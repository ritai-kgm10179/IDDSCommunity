using System;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class PanelLockoutConfiguration : UserControl
{

    public event EventHandler LockoutConfigurationChanged;

    public PanelLockoutConfiguration()
    {
        InitializeComponent();
        BackColor = Color.White;
        LoadData();
    }

    private void pictureBoxEdit_MouseDown(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X + 1, pictureBoxEdit.Location.Y + 1);

    private void pictureBoxEdit_MouseUp(object sender, MouseEventArgs e) => pictureBoxEdit.Location = new Point(pictureBoxEdit.Location.X - 1, pictureBoxEdit.Location.Y - 1);

    public bool IsInEditMode { get; set; }

    private void LoadData()
    {
        textBoxHardLocks.Text = IddsConfig.Instance.HardLockAttempts.ToString();
        textBoxHardLockDuration.Text = IddsConfig.Instance.HardLockTimeHours.ToString();
        textBoxSoftLockDuration.Text = IddsConfig.Instance.SoftLockTimeMinutes.ToString();
        textBoxSoftLocks.Text = IddsConfig.Instance.SoftLockAttempts.ToString();
        checkBoxLockForever.Checked = IddsConfig.Instance.LockForever;
        SetEditMode(false);
    }

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        //if (IsInEditMode) LoadData();
        //ToggleEditMode();
        //ClearErrors();
    }

    private static void ToggleEditMode()
    {
        //if (!IsInEditMode) {
        //    pictureBoxEdit.Image = global::Cyberarms.IntrusionDetection.Admin.Properties.Resources.button25px_delete;
        //    IsInEditMode = true;
        //} else {
        //    pictureBoxEdit.Image = global::Cyberarms.IntrusionDetection.Admin.Properties.Resources.button25px_edit;
        //    IsInEditMode = false;
        //}
        //pictureBoxSave.Visible = IsInEditMode;
        //textBoxHardLockDuration.Enabled = IsInEditMode;
        //textBoxHardLocks.Enabled = IsInEditMode;
        //textBoxSoftLockDuration.Enabled = IsInEditMode;
        //textBoxSoftLocks.Enabled = IsInEditMode;
        //checkBoxLockForever.Enabled = IsInEditMode;
    }

    private void ClearErrors()
    {
        errHardLockDuration.Visible = false;
        errHardLocks.Visible = false;
        errSoftLockDuration.Visible = false;
        errSoftLocks.Visible = false;
    }

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

    private void OnLockoutConfigurationChanged() => LockoutConfigurationChanged?.Invoke(this, EventArgs.Empty);

    private void buttonDiscard_Click(object sender, EventArgs e)
    {
        LoadData();
        SetEditMode(false);
    }

    private void textBoxSoftLocks_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);

    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }

    private void checkBoxLockForever_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);
}
