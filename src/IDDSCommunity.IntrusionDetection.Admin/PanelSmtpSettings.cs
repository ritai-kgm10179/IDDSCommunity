using System;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using MailKit.Security;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供 SMTP 郵件伺服器連線與認證設定之面板控制項。
/// </summary>
public partial class PanelSmtpSettings : UserControl
{
    private long editGeneration;

        /// <summary>
    /// 當 SmtpSettingsChanged 時引發之事件。
    /// </summary>
public event EventHandler? SmtpSettingsChanged;
    /// <summary>
    /// 初始化 <see cref="PanelSmtpSettings"/> 類別的新執行個體。
    /// </summary>
    public PanelSmtpSettings()
    {
        InitializeComponent();
        foreach (Control control in new Control[] { textBoxSender, textBoxRecipient, textBoxSmtpServer, textBoxSmtpPort, textBoxUsername, textBoxPassword })
            control.TextChanged += (_, _) => editGeneration++;
        checkBoxUseSSL.CheckedChanged += (_, _) => editGeneration++;
        checkBoxAuthentication.CheckedChanged += (_, _) => editGeneration++;
        Load += new EventHandler(PanelSmtpSettings_Load);
        SettingsResetButtonFactory.AddTo(this, ResetDefaults_Click, container: headerPanel);
    }
    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void PanelSmtpSettings_Load(object? sender, EventArgs e) => LoadData();

        /// <summary>
    /// 取得或設定 IsInEditMode。
    /// </summary>
public bool IsInEditMode { get; set; }

    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData();
        ToggleEditMode();
        ClearErrors();
    }
    /// <summary>
    /// 執行 toggle edit mode 作業。
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
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxSave_Click(object sender, EventArgs e)
    {

    }
    /// <summary>
    /// Processes the smtp settings changed notification.
    /// </summary>
    private void OnSmtpSettingsChanged() => SmtpSettingsChanged?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 執行 check form data 作業。
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
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void buttonTestSmtpSettings_Click(object sender, EventArgs e)
    {
        smartLabelTestError.Visible = false;

        if (CheckFormData())
        {
            try
            {
                var mimeMessage = new MimeKit.MimeMessage();
                mimeMessage.From.Add(MimeKit.MailboxAddress.Parse(textBoxSender.Text));
                mimeMessage.To.Add(MimeKit.MailboxAddress.Parse(textBoxRecipient.Text));
                mimeMessage.Subject = Strings.Get("Intrusion detection test email");
                mimeMessage.Body = new MimeKit.TextPart("plain") { Text = Strings.Get("This is a test message from IDDS Community.") };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                using System.Threading.CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
                int port = int.Parse(textBoxSmtpPort.Text);
                SecureSocketOptions secureOption = checkBoxUseSSL.Checked ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(textBoxSmtpServer.Text, port, secureOption, timeout.Token);

                if (checkBoxAuthentication.Checked)
                {
                    await client.AuthenticateAsync(textBoxUsername.Text, textBoxPassword.Text, timeout.Token);
                }

                await client.SendAsync(mimeMessage, timeout.Token);
                await client.DisconnectAsync(true, timeout.Token);

                MessageBox.Show(Strings.Get("Mail was sent successfully."));
            }
            catch (Exception ex)
            {
                smartLabelTestError.Text = string.Format("{0}\n{1}", ex.Message, ex.InnerException == null ? "" : ex.InnerException.Message);
                smartLabelTestError.Visible = true;
            }
        }
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X + 1, loc.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X - 1, loc.Y - 1);
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void buttonSave_Click(object sender, EventArgs e)
    {
        bool isOk = CheckFormData();
        if (isOk)
        {
            long generation = editGeneration;
            SmtpSnapshot snapshot = new(textBoxSender.Text, textBoxRecipient.Text, textBoxSmtpServer.Text,
                int.Parse(textBoxSmtpPort.Text), checkBoxUseSSL.Checked, checkBoxAuthentication.Checked,
                textBoxUsername.Text, textBoxPassword.Text);
            buttonSave.Enabled = false;
            try
            {
                await Task.Run(() => Persist(snapshot));
                if (generation == editGeneration)
                    MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnSmtpSettingsChanged();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!IsDisposed) buttonSave.Enabled = true; }
        }
    }

    private static void Persist(SmtpSnapshot value)
    {
        IddsConfig config = IddsConfig.Instance;
        config.SenderEmailAddress = value.Sender;
        config.NotificationEmailAddress = value.Recipient;
        config.SmtpServer = value.Server;
        config.SmtpPort = value.Port;
        config.SmtpSslRequired = value.UseSsl;
        config.SmtpRequiresAuthentication = value.Authentication;
        config.SmtpUsername = value.Username;
        config.SetSmtpPassword(value.Password);
        config.Save();
    }

    private sealed record SmtpSnapshot(string Sender, string Recipient, string Server, int Port, bool UseSsl,
        bool Authentication, string Username, string Password);
    private void ResetDefaults_Click(object? sender, EventArgs e)
    {
        IddsConfig defaults = IddsConfig.GetDefaultConfiguration();
        textBoxSender.Text = defaults.SenderEmailAddress;
        textBoxRecipient.Clear();
        textBoxSmtpServer.Clear();
        textBoxSmtpPort.Text = defaults.SmtpPort.ToString();
        checkBoxUseSSL.Checked = false;
        checkBoxAuthentication.Checked = false;
        textBoxUsername.Clear();
        textBoxPassword.Clear();
    }
}
