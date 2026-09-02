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
    /// 截圖並檢驗所有 Agent 進階設定面板與所有系統設定面板。
    /// </summary>
    [STATestMethod]
    public void CaptureAllUiSnapshots()
    {
        Directory.CreateDirectory(OutputDir);
        string tempDir = Path.Combine(Path.GetTempPath(), "idds-ui-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Database.Instance.Configure(tempDir);

        try
        {
            List<SecurityAgent> allAgents = GetTestAgents();

            using IddsAdmin admin = new();
            admin.StartPosition = FormStartPosition.Manual;
            admin.Show();
            admin.InitAdmin();
            Application.DoEvents();

            // 1. 切換至「代理程式」主分頁並載入所有 Agent
            var panelOnlineServices = typeof(IddsAdmin).GetField("panelOnlineServices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as Panel;
            var agentsMenu = typeof(IddsAdmin).GetField("labelMenuAgents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin) as SmartLabel;
            if (agentsMenu != null)
            {
                var showMethod = typeof(IddsAdmin).GetMethod("ShowMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                showMethod?.Invoke(admin, [agentsMenu]);
                admin.Dashboard.Hide();
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
                    bmp.Save(Path.Combine(OutputDir, $"agent_{safeName}.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            // 2. 切換至「設定」主分頁並截圖所有設定面板項目
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
                var showMethod = typeof(IddsAdmin).GetMethod("ShowMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                showMethod?.Invoke(admin, [settingsMenu]);
                admin.Dashboard.Hide();
                admin.PanelAgentConfiguration.Hide();
                panelOnlineServices?.Hide();
                admin.PanelApplicationSettings.Show();
                admin.PanelApplicationSettings.BringToFront();

                var nav = typeof(IDDSCommunityApplicationSettings).GetField("iddscommunitySettingsNavigation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(admin.PanelApplicationSettings) as IDDSCommunitySettingsNavigation;

                int index = 1;
                foreach (string itemKey in settingsItems)
                {
                    nav?.SetSelectedItem(Strings.Get(itemKey));
                    Application.DoEvents();

                    using Bitmap bmp = CaptureForm(admin);
                    string safeItemName = itemKey.Replace(" ", "_").Replace("&", "and");
                    bmp.Save(Path.Combine(OutputDir, $"settings_{index:D2}_{safeItemName}.png"), System.Drawing.Imaging.ImageFormat.Png);
                    index++;
                }
            }

            admin.Close();
        }
        finally
        {
            Database.Instance.Close();
            try { Directory.Delete(tempDir, true); } catch { }
        }
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
        if (parent.GetType().Name == "PluginSettingEditor")
        {
            foreach (Control c in parent.Controls)
            {
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

        foreach (Control child in parent.Controls)
        {
            DrawCustomControlsRecursive(child, form, g);
        }
    }

    private static List<SecurityAgent> GetTestAgents() =>
    [
        CreateAgent("Technitium DNS", "IDDSCommunity.Agents.TechnitiumDns.TechnitiumDnsSecurityAgent", new()
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

        CreateAgent("Windows DNS", "IDDSCommunity.Agents.WindowsDns.WindowsDnsSecurityAgent", new()
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

        CreateAgent("Honeypot", "IDDSCommunity.Agents.Honeypot.HoneypotSecurityAgent", new()
        {
            ["DecoyPortsString"] = "21,23,25,8080"
        }, new()
        {
            ["DecoyPortsString"] = typeof(string).FullName!
        }),

        CreateAgent("OpenSSH", "IDDSCommunity.Agents.OpenSsh.OpenSshSecurityAgent", new()
        {
            ["LogFilePath"] = @"C:\ProgramData\ssh\logs\sshd.log",
            ["ReadEventLog"] = "true"
        }, new()
        {
            ["LogFilePath"] = typeof(string).FullName!,
            ["ReadEventLog"] = typeof(bool).FullName!
        }),

        CreateAgent("FTP Server", "IDDSCommunity.Agents.FtpServer.FtpAgent", new()
        {
            ["Port"] = "21"
        }, new()
        {
            ["Port"] = typeof(int).FullName!
        }),

        CreateAgent("Terminal Server (TLS/SSL)", "IDDSCommunity.Agents.TerminalServer.TlsSslAgent", new()
        {
            ["RdpPort"] = "3389"
        }, new()
        {
            ["RdpPort"] = typeof(int).FullName!
        }),

        CreateAgent("RD Gateway", "IDDSCommunity.Agents.RemoteDesktopGateway.RdGatewaySecurityAgent", new()
        {
            ["LogFilePath"] = @"C:\Windows\System32\LogFiles\HTTPERR\httperr1.log"
        }, new()
        {
            ["LogFilePath"] = typeof(string).FullName!
        }),

        CreateAgent("RADIUS", "IDDSCommunity.Agents.Radius.RadiusSecurityAgent", new()
        {
            ["LogDirectoryPath"] = @"C:\Windows\System32\LogFiles",
            ["LogSearchPattern"] = "IN*.log"
        }, new()
        {
            ["LogDirectoryPath"] = typeof(string).FullName!,
            ["LogSearchPattern"] = typeof(string).FullName!
        }),

        CreateAgent("PostgreSQL", "IDDSCommunity.Agents.PostgreSql.PostgreSqlSecurityAgent", new()
        {
            ["LogDirectoryPath"] = @"C:\Program Files\PostgreSQL\16\data\log",
            ["LogSearchPattern"] = "postgresql-*.log"
        }, new()
        {
            ["LogDirectoryPath"] = typeof(string).FullName!,
            ["LogSearchPattern"] = typeof(string).FullName!
        }),

        CreateAgent("FileZilla", "IDDSCommunity.Agents.FileZilla.FileZillaSecurityAgent", new()
        {
            ["LogFilePath"] = @"C:\Program Files\FileZilla Server\Logs\filezilla-server.log"
        }, new()
        {
            ["LogFilePath"] = typeof(string).FullName!
        }),

        CreateAgent("FileMaker", "IDDSCommunity.Agents.FileMaker.FileMakerSecurityAgent", [], []),
        CreateAgent("SQL Server", "IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher", [], []),
        CreateAgent("MySQL", "IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher", [], []),

        CreateAgent("IMAP", "IDDSCommunity.Agents.MailServer.ImapAgent", new()
        {
            ["Port"] = "143"
        }, new()
        {
            ["Port"] = typeof(int).FullName!
        }),

        CreateAgent("POP3", "IDDSCommunity.Agents.MailServer.Pop3Agent", new()
        {
            ["Port"] = "110"
        }, new()
        {
            ["Port"] = typeof(int).FullName!
        }),

        CreateAgent("SMTP", "IDDSCommunity.Agents.MailServer.SmtpAgent", new()
        {
            ["Port"] = "25"
        }, new()
        {
            ["Port"] = typeof(int).FullName!
        }),

        CreateAgent("WinRM", "IDDSCommunity.Agents.WinRm.WinRmSecurityAgent", new()
        {
            ["Port"] = "5985"
        }, new()
        {
            ["Port"] = typeof(int).FullName!
        }),

        CreateAgent("Active Directory", "IDDSCommunity.Agents.ActiveDirectory.ActiveDirectorySecurityAgent", new()
        {
            ["FailureThreshold"] = "5",
            ["WindowSeconds"] = "300"
        }, new()
        {
            ["FailureThreshold"] = typeof(int).FullName!,
            ["WindowSeconds"] = typeof(int).FullName!
        }),

        CreateAgent("Windows Network Logon", "IDDSCommunity.Agents.WindowsNetworkLogon.WindowsNetworkLogonSecurityAgent", new()
        {
            ["FailureThreshold"] = "5",
            ["WindowSeconds"] = "300"
        }, new()
        {
            ["FailureThreshold"] = typeof(int).FullName!,
            ["WindowSeconds"] = typeof(int).FullName!
        }),

        CreateAgent("Web Security", "IDDSCommunity.Agents.WebSecurity.WebSecurityAgent", new()
        {
            ["ProtectedPaths"] = "/admin,/login,/wp-login.php"
        }, new()
        {
            ["ProtectedPaths"] = typeof(string).FullName!
        }),

        CreateAgent("IIS Authentication", "IDDSCommunity.Agents.IisAuthentication.IisAuthenticationSecurityAgent", new()
        {
            ["LogDirectoryPath"] = @"C:\inetpub\logs\LogFiles\W3SVC1",
            ["LogSearchPattern"] = "u_ex*.log"
        }, new()
        {
            ["LogDirectoryPath"] = typeof(string).FullName!,
            ["LogSearchPattern"] = typeof(string).FullName!
        }),

        CreateAgent("Windows Authentication", "IDDSCommunity.Agents.WindowsAuthentication.WindowsAuthenticationSecurityAgent", [], [])
    ];

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
