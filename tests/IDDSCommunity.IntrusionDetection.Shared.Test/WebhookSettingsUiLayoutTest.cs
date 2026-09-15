using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class WebhookSettingsUiLayoutTest
{
    [STATestMethod]
    [DataRow("zh-TW")]
    [DataRow("en-US")]
    public void PrivateDestinationField_FitsAndCanBeEdited(string cultureName)
    {
        string previous = LanguageManager.Instance.CurrentCulture.Name;
        string tempDir = Path.Combine(Path.GetTempPath(), "idds-webhook-ui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            Environment.SetEnvironmentVariable("IDDS_TEST_MODE", "1");
            Database.Instance.Configure(tempDir);
            LanguageManager.Instance.Initialize(cultureName);
            using Form form = new() { ClientSize = new Size(640, 700) };
            using PanelNotificationSettings panel = new() { Dock = DockStyle.Fill };
            form.Controls.Add(panel);
            form.Show();
            Application.DoEvents();
            Label label = Assert.IsInstanceOfType<Label>(panel.Controls.Find("labelWebhookPrivateDestinations", true)[0]);
            string expectedLabel = cultureName == "zh-TW"
                ? "允許的內網 Webhook 目的地（HTTPS 主機及埠｜IP 或 CIDR，每行一筆）"
                : "Allowed private Webhook destinations (HTTPS origin | IP or CIDR, one per line)";
            Assert.AreEqual(expectedLabel, label.Text);
            TextBox field = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxWebhookPrivateDestinations", true)[0]);
            TextBox url = Assert.IsInstanceOfType<TextBox>(panel.Controls.Find("textBoxWebhookUrl", true)[0]);
            Assert.IsTrue(label.PreferredWidth <= panel.Width);
            Assert.IsTrue(url.Bottom <= label.Top);
            Assert.IsTrue(label.Bottom <= field.Top);
            field.Text = "https://hooks.example.com:9443|192.168.1.0/24";
            Assert.AreEqual("https://hooks.example.com:9443|192.168.1.0/24", field.Text);
            form.Close();
        }
        finally
        {
            LanguageManager.Instance.Initialize(previous);
            Database.Instance.Close();
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
