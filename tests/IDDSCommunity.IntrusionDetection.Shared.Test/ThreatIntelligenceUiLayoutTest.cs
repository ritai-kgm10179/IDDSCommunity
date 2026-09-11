using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證威脅情報與叢集聯防設定面板於中英文語系下之 UI 佈局健全性，杜絕跑版與重疊。
/// </summary>
[TestClass]
public sealed class ThreatIntelligenceUiLayoutTest
{
    private string _tempDir = null!;

    /// <summary>
    /// 初始化測試環境與獨立暫存資料庫，避免測試受到正式環境金鑰權限干擾。
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        Environment.SetEnvironmentVariable("IDDS_TEST_MODE", "1");
        _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "idds-ti-test-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_tempDir);
        Database.Instance.Configure(_tempDir);
    }

    /// <summary>
    /// 清理測試資料庫連線與暫存檔案目錄。
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        Database.Instance.Close();
        try
        {
            if (System.IO.Directory.Exists(_tempDir))
            {
                System.IO.Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // 忽略暫存目錄清理失敗
        }
    }
    /// <summary>
    /// 驗證在中英文語系下，「立即更新威脅情資」按鈕與相鄰控制項絕不重疊且文字完整容納。
    /// </summary>
    /// <param name="cultureName">待測試之文化特性識別代碼（zh-TW 或 en-US）。</param>
    [STATestMethod]
    [DataRow("zh-TW")]
    [DataRow("en-US")]
    public void PanelThreatIntelligenceSettings_UpdateThreatFeedsButton_LayoutValidInBothCultures(string cultureName)
    {
        string originalCulture = LanguageManager.Instance.CurrentCulture.Name;
        try
        {
            LanguageManager.Instance.Initialize(cultureName);

            using Panel viewport = new() { ClientSize = new Size(600, 800) };
            using PanelThreatIntelligenceSettings panel = new() { Size = new Size(600, 800) };
            viewport.Controls.Add(panel);
            viewport.PerformLayout();
            panel.PerformLayout();

            Button updateButton = panel.btnUpdateThreatFeedsNow;
            Label statusLabel = panel.lblThreatFeedStatus;

            Assert.IsNotNull(updateButton, "btnUpdateThreatFeedsNow 控制項必須存在。");
            Assert.IsNotNull(statusLabel, "lblThreatFeedStatus 控制項必須存在。");

            // 驗證按鈕文字非空白
            Assert.IsFalse(string.IsNullOrWhiteSpace(updateButton.Text), "更新按鈕文字不得為空白。");

            // 驗證文字在不同語系下的正確性
            if (cultureName == "zh-TW")
            {
                Assert.AreEqual("立即更新威脅情資", updateButton.Text);
            }
            else
            {
                Assert.AreEqual("Update Threat Feeds Now", updateButton.Text);
            }

            // 驗證按鈕邊界有效
            Assert.IsTrue(updateButton.Width >= 200, $"更新按鈕寬度 ({updateButton.Width}) 必須至少具備 200px 以完整呈現雙語標籤。");
            Assert.IsTrue(updateButton.Height >= 28, $"更新按鈕高度 ({updateButton.Height}) 必須至少具備 28px 以符合點擊規範。");

            // 驗證按鈕與狀態標籤垂直有序排列且不重疊
            Assert.IsTrue(statusLabel.Top >= updateButton.Bottom,
                $"狀態標籤頂部 ({statusLabel.Top}) 必須位於更新按鈕底部 ({updateButton.Bottom}) 之後或齊平。");
            Assert.IsFalse(updateButton.Bounds.IntersectsWith(statusLabel.Bounds),
                "更新按鈕與狀態標籤之邊界不得重疊。");

            string? screenshotDir = Environment.GetEnvironmentVariable("IDDS_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(screenshotDir) && System.IO.Directory.Exists(@"C:\Users\user\.gemini\antigravity\brain\77306ac9-4872-4667-aa78-107fcaf7c9b2"))
            {
                screenshotDir = @"C:\Users\user\.gemini\antigravity\brain\77306ac9-4872-4667-aa78-107fcaf7c9b2";
            }

            if (!string.IsNullOrEmpty(screenshotDir) && System.IO.Directory.Exists(screenshotDir))
            {
                using Form form = new() { ClientSize = new Size(640, 600), StartPosition = FormStartPosition.Manual };
                viewport.Controls.Remove(panel);
                panel.Dock = DockStyle.Fill;
                form.Controls.Add(panel);
                form.Show();
                panel.ScrollControlIntoView(updateButton);
                Application.DoEvents();

                using Bitmap bmp = new(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.Size));
                bmp.Save(System.IO.Path.Combine(screenshotDir, $"{cultureName}_threat_feeds_scrolled.png"), System.Drawing.Imaging.ImageFormat.Png);
                form.Close();
            }
        }
        finally
        {
            LanguageManager.Instance.Initialize(originalCulture);
        }
    }

    /// <summary>
    /// 驗證當節點角色切換為邊緣節點（EdgeNode）時，更新按鈕自動連動停用。
    /// </summary>
    [STATestMethod]
    public void PanelThreatIntelligenceSettings_EdgeNodeRole_DisablesUpdateThreatFeedsButton()
    {
        using PanelThreatIntelligenceSettings panel = new();
        panel.LoadData();

        ComboBox roleCombo = Assert.IsInstanceOfType<ComboBox>(panel.Controls.Find("comboClusterRole", true)[0]);
        roleCombo.SelectedIndex = (int)ThreatHubRole.EdgeNode;

        Assert.IsFalse(panel.btnUpdateThreatFeedsNow.Enabled, "邊緣節點模式下，外部威脅情資更新按鈕必須被停用。");

        roleCombo.SelectedIndex = (int)ThreatHubRole.Standalone;
        CheckBox enableFeeds = Assert.IsInstanceOfType<CheckBox>(panel.Controls.Find("chkEnableFeeds", true)[0]);
        enableFeeds.Checked = true;
        Assert.IsTrue(panel.btnUpdateThreatFeedsNow.Enabled, "獨立模式且啟用情資訂閱時，更新按鈕必須處於啟用狀態。");
    }
}