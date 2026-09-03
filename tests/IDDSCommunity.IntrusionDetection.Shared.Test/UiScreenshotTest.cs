using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Admin;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 產生 UI 畫面截圖以驗證所有代理程式與設定面板之排版與控制項邊界。
/// </summary>
[TestClass]
public sealed class UiScreenshotTest
{
    private static readonly string OutputDir =
        Environment.GetEnvironmentVariable("IDDS_SCREENSHOT_DIR")
        ?? (Directory.Exists(@"C:\Users\user\.gemini\antigravity\brain\7946b439-3249-4c3e-ae4a-68d24cffc07d")
            ? @"C:\Users\user\.gemini\antigravity\brain\7946b439-3249-4c3e-ae4a-68d24cffc07d"
            : Path.Combine(Path.GetTempPath(), "idds-ui-screenshots"));

    /// <summary>
    /// <summary>
    /// 截圖並檢驗所有 Agent 進階設定面板與所有系統設定面板（包含繁體中文與英文雙語系）。
    /// </summary>
    [STATestMethod]
    public void CaptureAllUiSnapshots()
    {
        Environment.SetEnvironmentVariable("IDDS_TEST_MODE", "1");
        Directory.CreateDirectory(OutputDir);
        string tempDir = Path.Combine(Path.GetTempPath(), "idds-ui-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Database.Instance.Configure(tempDir);

        try
        {
            // 1. 擷取正體中文 (zh-TW) 介面
            CaptureLanguageSession("zh-TW", "zh");

            // 2. 擷取英文 (en-US) 介面
            CaptureLanguageSession("en-US", "en");
        }
        finally
        {
            LanguageManager.Instance.Initialize("auto");
            Database.Instance.Close();
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static void CaptureLanguageSession(string cultureCode, string prefix)
    {
        LanguageManager.Instance.Initialize(cultureCode);
        List<SecurityAgent> allAgents = GetTestAgents(cultureCode);

        using IddsAdmin admin = new();
        admin.StartPosition = FormStartPosition.Manual;
        admin.Show();
        admin.InitAdmin();
        var logReader = typeof(IddsAdmin).GetField("logReader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as Timer;
        logReader?.Stop();
        var timerRefreshServiceStatus = typeof(IddsAdmin).GetField("timerRefreshServiceStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as Timer;
        timerRefreshServiceStatus?.Stop();
        Application.DoEvents();

        // 1. 首頁 (Dashboard) 截圖
        using (Bitmap bmpDash = CaptureForm(admin))
        {
            bmpDash.Save(Path.Combine(OutputDir, $"{prefix}_dashboard.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // 2. 安全性記錄 (Security Log) 截圖
        var showMenuMethod = typeof(IddsAdmin).GetMethod("ShowMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var securityLogMenu = typeof(IddsAdmin).GetField("labelMenuSecurityLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as SmartLabel;
        var panelOnlineServices = typeof(IddsAdmin).GetField("panelOnlineServices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as Panel;
        if (securityLogMenu != null)
        {
            showMenuMethod?.Invoke(admin, [securityLogMenu]);
            admin.Dashboard.Hide();
            admin.PanelAgentConfiguration.Hide();
            admin.PanelApplicationSettings.Hide();
            admin.PanelSecurityLog.Show();
            admin.PanelSecurityLog.BringToFront();
            panelOnlineServices?.Hide();
            Application.DoEvents();
            using Bitmap bmpSec = CaptureForm(admin);
            bmpSec.Save(Path.Combine(OutputDir, $"{prefix}_security_log.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // 3. 切換至「代理程式」主分頁並載入所有 Agent
        var agentsMenu = typeof(IddsAdmin).GetField("labelMenuAgents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as SmartLabel;
        if (agentsMenu != null)
        {
            showMenuMethod?.Invoke(admin, [agentsMenu]);
            admin.Dashboard.Hide();
            admin.PanelSecurityLog.Hide();
            admin.PanelApplicationSettings.Hide();
            panelOnlineServices?.Hide();
            admin.PanelAgentConfiguration.Show();
            admin.PanelAgentConfiguration.BringToFront();

            admin.PanelAgentConfiguration.ClearSecurityAgents();
            foreach (SecurityAgent agent in allAgents)
            {
                admin.PanelAgentConfiguration.LoadSecurityAgent(agent);
            }

            foreach (SecurityAgent agent in allAgents)
            {
                admin.PanelAgentConfiguration.ShowAgentConfig(agent);
                Application.DoEvents();

                string safeName = agent.Name.Substring(agent.Name.LastIndexOf('.') + 1);
                using Bitmap bmp = CaptureForm(admin);
                bmp.Save(Path.Combine(OutputDir, $"{prefix}_agent_{safeName}.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 4. 切換至「設定」主分頁並截圖所有設定面板項目
        string[] settingsItems =
        [
            IDDSCommunityApplicationSettings.MENU_LOCK_OUT_CONFIGURATION,
            IDDSCommunityApplicationSettings.MENU_SAFE_NETWORKS,
            IDDSCommunityApplicationSettings.MENU_THREAT_INTELLIGENCE,
            IDDSCommunityApplicationSettings.MENU_CLOUD_PERIMETER,
            IDDSCommunityApplicationSettings.MENU_SELF_SERVICE,
            IDDSCommunityApplicationSettings.MENU_DECEPTION_AND_API,
            IDDSCommunityApplicationSettings.MENU_NOTIFICATION_SETTINGS,
            IDDSCommunityApplicationSettings.MENU_SMTP_SETTINGS,
            IDDSCommunityApplicationSettings.MENU_COMPLIANCE_AND_FORENSICS,
            IDDSCommunityApplicationSettings.MENU_LANGUAGE_SETTINGS,
            IDDSCommunityApplicationSettings.MENU_DATABASE_MAINTENANCE,
            IDDSCommunityApplicationSettings.MENU_CONFIGURATION_TRANSFER,
            IDDSCommunityApplicationSettings.MENU_REPORT_EXPORT
        ];

        var settingsMenu = typeof(IddsAdmin).GetField("labelMenuSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as SmartLabel;
        if (settingsMenu != null)
        {
            showMenuMethod?.Invoke(admin, [settingsMenu]);
            admin.Dashboard.Hide();
            admin.PanelAgentConfiguration.Hide();
            admin.PanelSecurityLog.Hide();
            panelOnlineServices?.Hide();
            admin.PanelApplicationSettings.Show();
            admin.PanelApplicationSettings.BringToFront();

            var nav = typeof(IDDSCommunityApplicationSettings).GetField("iddscommunitySettingsNavigation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin.PanelApplicationSettings) as IDDSCommunitySettingsNavigation;

            int index = 1;
            foreach (string itemKey in settingsItems)
            {
                nav?.SetSelectedItem(Strings.Get(itemKey));

                // 針對 CIS 面板執行掃描以產生項目以供 Auto Width 驗證
                if (itemKey == IDDSCommunityApplicationSettings.MENU_COMPLIANCE_AND_FORENSICS)
                {
                    admin.PanelApplicationSettings.PanelComplianceAndForensics.RunCisScan();
                }

                Application.DoEvents();

                using Bitmap bmp = CaptureForm(admin);
                string safeItemName = itemKey.Replace(" ", "_").Replace("&", "and");
                bmp.Save(Path.Combine(OutputDir, $"{prefix}_settings_{index:D2}_{safeItemName}.png"), System.Drawing.Imaging.ImageFormat.Png);
                index++;
            }
        }

        admin.Close();
    }

    private static Bitmap CaptureForm(Form form)
    {
        Bitmap bmp = new(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.Size));
        using (Graphics g = Graphics.FromImage(bmp))
        {
            DrawCustomControlsRecursive(form, form, g);
        }
        return bmp;
    }

    private static Point GetControlFormLocation(Control ctrl, Form form)
    {
        int x = 0;
        int y = 0;
        Control? current = ctrl;
        while (current != null && current != form)
        {
            x += current.Left;
            y += current.Top;
            current = current.Parent;
        }
        return new Point(x, y);
    }

    private static void DrawCustomControlsRecursive(Control parent, Form form, Graphics g)
    {
        if (!parent.Visible) return;

        if (parent.GetType().Name == "PluginSettingEditor")
        {
            foreach (Control c in parent.Controls)
            {
                if (!c.Visible) continue;
                Point formPos = GetControlFormLocation(c, form);
                int width = c.Width > 0 ? c.Width : 200;
                int height = c.Height > 0 ? c.Height : 23;
                Color textColor = Color.FromArgb(102, 102, 102);
                Font font = new("Segoe UI", 9F);

                if (c is TextBox tb)
                {
                    Rectangle rect = new(formPos, new Size(width, height));
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawRectangle(Pens.DarkGray, new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
                    TextRenderer.DrawText(g, tb.Text, font, new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 8, rect.Height - 4), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                else if (c is NumericUpDown nud)
                {
                    Rectangle rect = new(formPos, new Size(width, height));
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawRectangle(Pens.DarkGray, new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
                    TextRenderer.DrawText(g, nud.Value.ToString(), font, new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 24, rect.Height - 4), textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                else if (c is Button btn)
                {
                    Rectangle rect = new(formPos, new Size(width, height));
                    using Brush btnBrush = new SolidBrush(Color.White);
                    g.FillRectangle(btnBrush, rect);
                    g.DrawRectangle(Pens.DarkGray, new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
                    TextRenderer.DrawText(g, btn.Text, font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        if (parent is ComboBox cb)
        {
            if (cb.Visible)
            {
                Point formPos = GetControlFormLocation(cb, form);
                int width = cb.Width > 0 ? cb.Width : 200;
                int height = cb.Height > 0 ? cb.Height : 23;
                Rectangle rect = new(formPos, new Size(width, height));
                g.FillRectangle(Brushes.White, rect);
                g.DrawRectangle(Pens.DarkGray, new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1));
                string? text = cb.GetItemText(cb.SelectedItem);
                if (string.IsNullOrEmpty(text)) text = cb.Text;
                TextRenderer.DrawText(g, text, cb.Font, new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 20, rect.Height - 4), Color.FromArgb(102, 102, 102), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        if (parent is ListView lv && lv.View == View.Details)
        {
            if (lv.Visible)
            {
                Point lvPos = GetControlFormLocation(lv, form);
                int curX = lvPos.X;
                Font headerFont = new("Segoe UI", 9F, FontStyle.Bold);
                Font cellFont = new("Segoe UI", 8.5F);
                foreach (ColumnHeader col in lv.Columns)
                {
                    Rectangle headerRect = new(curX, lvPos.Y, col.Width, 24);
                    g.FillRectangle(Brushes.WhiteSmoke, headerRect);
                    g.DrawRectangle(Pens.DarkGray, headerRect);
                    TextRenderer.DrawText(g, col.Text, headerFont, headerRect, Color.FromArgb(51, 51, 51), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    curX += col.Width;
                }
                int curY = lvPos.Y + 24;
                foreach (ListViewItem item in lv.Items)
                {
                    curX = lvPos.X;
                    for (int colIdx = 0; colIdx < lv.Columns.Count && colIdx < item.SubItems.Count; colIdx++)
                    {
                        int colWidth = lv.Columns[colIdx].Width;
                        Rectangle cellRect = new(curX, curY, colWidth, 20);
                        g.FillRectangle(Brushes.White, cellRect);
                        g.DrawRectangle(Pens.LightGray, cellRect);
                        Color itemColor = item.ForeColor;
                        TextRenderer.DrawText(g, item.SubItems[colIdx].Text, cellFont, cellRect, itemColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        curX += colWidth;
                    }
                    curY += 20;
                    if (curY > lvPos.Y + lv.Height) break;
                }
            }
        }

        foreach (Control child in parent.Controls)
        {
            DrawCustomControlsRecursive(child, form, g);
        }
    }

    private static List<SecurityAgent> GetTestAgents(string cultureCode)
    {
        bool isZh = cultureCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        return
        [
            CreateAgent(isZh ? "Technitium DNS 安全性代理程式" : "Technitium DNS Security Agent", "IDDSCommunity.Agents.TechnitiumDns.TechnitiumDnsSecurityAgent", new()
            {
                ["LogDirectoryPath"] = @"C:\Program Files\Technitium DNS Server\logs",
                ["LogSearchPattern"] = "dns-*.log",
                ["WindowSeconds"] = "300"
            }, new()
            {
                ["LogDirectoryPath"] = typeof(string).FullName!,
                ["LogSearchPattern"] = typeof(string).FullName!,
                ["WindowSeconds"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "Windows DNS 安全性代理程式" : "Windows DNS Security Agent", "IDDSCommunity.Agents.WindowsDns.WindowsDnsSecurityAgent", new()
            {
                ["LogDirectoryPath"] = @"C:\Windows\System32\dns",
                ["LogSearchPattern"] = "dns.log",
                ["WindowSeconds"] = "60",
                ["FailureThreshold"] = "50"
            }, new()
            {
                ["LogDirectoryPath"] = typeof(string).FullName!,
                ["LogSearchPattern"] = typeof(string).FullName!,
                ["WindowSeconds"] = typeof(int).FullName!,
                ["FailureThreshold"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "誘餌蜜罐主動防禦代理程式" : "Honeypot Decoy Security Agent", "IDDSCommunity.Agents.Honeypot.HoneypotSecurityAgent", new()
            {
                ["DecoyPortsString"] = "21,23,25,8080"
            }, new()
            {
                ["DecoyPortsString"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "Windows OpenSSH 安全性代理程式" : "Windows OpenSSH Security Agent", "IDDSCommunity.Agents.OpenSsh.OpenSshSecurityAgent", new()
            {
                ["LogFilePath"] = @"C:\ProgramData\ssh\logs\sshd.log",
                ["ReadEventLog"] = "true"
            }, new()
            {
                ["LogFilePath"] = typeof(string).FullName!,
                ["ReadEventLog"] = typeof(bool).FullName!
            }),

            CreateAgent(isZh ? "FTP 安全性代理程式" : "FTP Security Agent", "IDDSCommunity.Agents.FtpServer.FtpAgent", new()
            {
                ["Port"] = "21"
            }, new()
            {
                ["Port"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "遠端桌面安全性代理程式" : "Terminal Server (TLS/SSL) Security Agent", "IDDSCommunity.Agents.TerminalServer.TlsSslAgent", new()
            {
                ["RdpPort"] = "3389"
            }, new()
            {
                ["RdpPort"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "遠端桌面閘道安全性代理程式" : "Remote Desktop Gateway Security Agent", "IDDSCommunity.Agents.RemoteDesktopGateway.RdGatewaySecurityAgent", new()
            {
                ["LogFilePath"] = @"C:\Windows\System32\LogFiles\HTTPERR\httperr1.log"
            }, new()
            {
                ["LogFilePath"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "NPS RADIUS 安全性代理程式" : "NPS RADIUS Security Agent", "IDDSCommunity.Agents.Radius.RadiusSecurityAgent", new()
            {
                ["LogDirectoryPath"] = @"C:\Windows\System32\LogFiles",
                ["LogSearchPattern"] = "IN*.log"
            }, new()
            {
                ["LogDirectoryPath"] = typeof(string).FullName!,
                ["LogSearchPattern"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "PostgreSQL 安全性代理程式" : "PostgreSQL Security Agent", "IDDSCommunity.Agents.PostgreSql.PostgreSqlSecurityAgent", new()
            {
                ["LogDirectoryPath"] = @"C:\Program Files\PostgreSQL\16\data\log",
                ["LogSearchPattern"] = "postgresql-*.log"
            }, new()
            {
                ["LogDirectoryPath"] = typeof(string).FullName!,
                ["LogSearchPattern"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "FileZilla 安全性代理程式" : "FileZilla Security Agent", "IDDSCommunity.Agents.FileZilla.FileZillaSecurityAgent", new()
            {
                ["LogFilePath"] = @"C:\Program Files\FileZilla Server\Logs\filezilla-server.log"
            }, new()
            {
                ["LogFilePath"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "FileMaker 安全性代理程式" : "FileMaker Security Agent", "IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent", [], []),
            CreateAgent(isZh ? "SQL Server 安全性代理程式" : "SQL Server Security Agent", "IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher", [], []),
            CreateAgent(isZh ? "MySQL／MariaDB 安全性代理程式" : "MySQL and MariaDB Security Agent", "IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher", [], []),

            CreateAgent(isZh ? "IMAP 安全性代理程式" : "IMAP Security Agent", "IDDSCommunity.Agents.MailServer.ImapAgent", new()
            {
                ["Port"] = "143"
            }, new()
            {
                ["Port"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "POP3 安全性代理程式" : "POP3 Security Agent", "IDDSCommunity.Agents.MailServer.Pop3Agent", new()
            {
                ["Port"] = "110"
            }, new()
            {
                ["Port"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "郵件伺服器 SMTP 安全性代理程式" : "SMTP Security Agent", "IDDSCommunity.Agents.MailServer.SmtpAgent", new()
            {
                ["Port"] = "25"
            }, new()
            {
                ["Port"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "Windows 遠端管理（WinRM / WAC）安全性代理程式" : "Windows Remote Management (WinRM / WAC) Security Agent", "IDDSCommunity.Agents.WinRm.WinRmSecurityAgent", new()
            {
                ["Port"] = "5985"
            }, new()
            {
                ["Port"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "Active Directory 與 Kerberos 安全性代理程式" : "Active Directory & Kerberos Security Agent", "IDDSCommunity.Agents.ActiveDirectory.ActiveDirectorySecurityAgent", new()
            {
                ["FailureThreshold"] = "5",
                ["WindowSeconds"] = "300"
            }, new()
            {
                ["FailureThreshold"] = typeof(int).FullName!,
                ["WindowSeconds"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "Windows 網路登入安全性代理程式" : "Windows Network Logon Security Agent", "IDDSCommunity.Agents.WindowsNetworkLogon.WindowsNetworkLogonSecurityAgent", new()
            {
                ["FailureThreshold"] = "5",
                ["WindowSeconds"] = "300"
            }, new()
            {
                ["FailureThreshold"] = typeof(int).FullName!,
                ["WindowSeconds"] = typeof(int).FullName!
            }),

            CreateAgent(isZh ? "Web 安全性代理程式" : "Web Security Agent", "IDDSCommunity.Agents.WebSecurity.WebSecurityAgent", new()
            {
                ["ProtectedPaths"] = "/admin,/login,/wp-login.php"
            }, new()
            {
                ["ProtectedPaths"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "IIS 驗證安全性代理程式" : "IIS Authentication Security Agent", "IDDSCommunity.Agents.IisAuthentication.IisAuthenticationSecurityAgent", new()
            {
                ["LogDirectoryPath"] = @"C:\inetpub\logs\LogFiles\W3SVC1",
                ["LogSearchPattern"] = "u_ex*.log"
            }, new()
            {
                ["LogDirectoryPath"] = typeof(string).FullName!,
                ["LogSearchPattern"] = typeof(string).FullName!
            }),

            CreateAgent(isZh ? "Windows 基礎安全性代理程式" : "Windows Base Security Agent", "IDDSCommunity.Agents.WindowsAuthentication.WindowsAuthenticationSecurityAgent", [], [])
        ];
    }

    private static SecurityAgent CreateAgent(
        string displayName,
        string typeName,
        Dictionary<string, string> customConfig,
        Dictionary<string, string> customTypes) => new()
    {
        DisplayName = displayName,
        Name = typeName,
        Enabled = true,
        OverrideConfig = false,
        LockForever = false,
        HardLockAttempts = 20,
        HardLockTimeHours = 1,
        SoftLockAttempts = 10,
        SoftLockTimeMinutes = 1,
        CustomConfiguration = customConfig,
        CustomConfigurationTypes = customTypes
    };
}
