using System;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
using MailKit.Security;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class PanelSmtpSettings : UserControl
{

    public event EventHandler? SmtpSettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PanelSmtpSettings"/> class.
    /// </summary>

    public PanelSmtpSettings()
    {
        InitializeComponent();
        BackColor = Color.White;
        Load += new EventHandler(PanelSmtpSettings_Load);
    }

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void PanelSmtpSettings_Load(object? sender, EventArgs e) => LoadData();

    public bool IsInEditMode { get; set; }


    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData();
        ToggleEditMode();
        ClearErrors();
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
        textBoxSender.Enabled = IsInEditMode;
        textBoxRecipient.Enabled = IsInEditMode;
        textBoxSmtpServer.Enabled = IsInEditMode;
        textBoxSmtpPort.Enabled = IsInEditMode;
        checkBoxUseSSL.Enabled = IsInEditMode;
        checkBoxAuthentication.Enabled = IsInEditMode;
        textBoxUsername.Enabled = IsInEditMode;
        textBoxPassword.Enabled = IsInEditMode;
    }

    /// <summary>
    /// Loads data.
    /// </summary>

    private void LoadData()
    {
        textBoxSender.Text = IddsConfig.Instance.SenderEmailAddress;
        textBoxRecipient.Text = IddsConfig.Instance.NotificationEmailAddress;
        textBoxSmtpServer.Text = IddsConfig.Instance.SmtpServer;
        textBoxSmtpPort.Text = IddsConfig.Instance.SmtpPort.ToString();
        checkBoxUseSSL.Checked = IddsConfig.Instance.SmtpSslRequired;
        checkBoxAuthentication.Checked = IddsConfig.Instance.SmtpRequiresAuthentication;
        textBoxUsername.Text = IddsConfig.Instance.SmtpUsername;
        textBoxPassword.Text = IddsConfig.Instance.GetSmtpPassword();
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxSave_Click(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Processes the smtp settings changed notification.
    /// </summary>

    private void OnSmtpSettingsChanged() => SmtpSettingsChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Executes the check form data operation.
    /// </summary>
    /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>

    private bool CheckFormData()
    {
        bool hasError = false;
        ClearErrors();
        if (!int.TryParse(textBoxSmtpPort.Text, out int smtpPort))
        {
            errSmtpPort.Visible = true;
            hasError = true;
        }
        return !hasError;
    }



    /// <summary>
    /// Clears errors.
    /// </summary>

    private void ClearErrors() => errSmtpPort.Visible = false;

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonTestSmtpSettings_Click(object sender, EventArgs e)
    {
        smartLabelTestError.Visible = false;

        if (CheckFormData())
        {
            try
            {
                var mimeMessage = new MimeKit.MimeMessage();
                mimeMessage.From.Add(MimeKit.MailboxAddress.Parse(textBoxSender.Text));
                mimeMessage.To.Add(MimeKit.MailboxAddress.Parse(textBoxRecipient.Text));
                mimeMessage.Subject = "Intrusion Detection Testmail";
                mimeMessage.Body = new MimeKit.TextPart("plain") { Text = "This is a test message from your Cyberarms Intrusion Detection administration tool." };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                int port = int.Parse(textBoxSmtpPort.Text);
                SecureSocketOptions secureOption = checkBoxUseSSL.Checked ? MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable : MailKit.Security.SecureSocketOptions.Auto;
                client.Connect(textBoxSmtpServer.Text, port, secureOption);

                if (checkBoxAuthentication.Checked)
                {
                    client.Authenticate(textBoxUsername.Text, textBoxPassword.Text);
                }

                client.Send(mimeMessage);
                client.Disconnect(true);

                MessageBox.Show("Mail was sent successfully.");
            }
            catch (Exception ex)
            {
                smartLabelTestError.Text = string.Format("{0}\n{1}", ex.Message, ex.InnerException == null ? "" : ex.InnerException.Message);
                smartLabelTestError.Visible = true;
            }
        }
    }

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
        bool isOk = CheckFormData();
        if (isOk)
        {
            IddsConfig.Instance.SenderEmailAddress = textBoxSender.Text;
            IddsConfig.Instance.NotificationEmailAddress = textBoxRecipient.Text;
            IddsConfig.Instance.SmtpServer = textBoxSmtpServer.Text;
            IddsConfig.Instance.SmtpPort = int.Parse(textBoxSmtpPort.Text);
            IddsConfig.Instance.SmtpSslRequired = checkBoxUseSSL.Checked;
            IddsConfig.Instance.SmtpRequiresAuthentication = checkBoxAuthentication.Checked;
            IddsConfig.Instance.SmtpUsername = textBoxUsername.Text;
            IddsConfig.Instance.SetSmtpPassword(textBoxPassword.Text);
            IddsConfig.Instance.Save();

            OnSmtpSettingsChanged();
        }
        SetEditMode(false);
    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();

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
