using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// Defines categories for Agent theme icons.
/// </summary>
public enum AgentThemeCategory
{
    Database,      // MySQL, SQL Server, PostgreSQL, FileMaker
    Mail,          // POP3, IMAP, SMTP
    Web,           // Web Security, IIS Authentication
    Terminal,      // OpenSSH, TerminalServer (RDP)
    FileTransfer,  // FTP Server
    Dns,           // Windows DNS
    AuthAndShield  // Windows Network Logon, RADIUS, AD, Kerberos, RRAS
}

/// <summary>
/// Factory for generating high-quality 15x15 vector theme icons for security agents.
/// </summary>
public static class AgentThemeIconFactory
{
    private static readonly Color DarkIconColor = Color.FromArgb(0, 150, 136); // Teal theme accent

    /// <summary>
    /// Creates a 15x15 vector icon bitmap corresponding to the specified theme category and selection state.
    /// </summary>
    public static Bitmap Create(AgentThemeCategory category, bool selected, Color? customAccent = null)
    {
        Bitmap bitmap = new(15, 15);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Color mainColor = selected ? Color.White : (customAccent ?? DarkIconColor);
        using SolidBrush brush = new(mainColor);

        switch (category)
        {
            case AgentThemeCategory.Database:
                // 3D Cylinder Stack
                g.FillEllipse(brush, 2F, 1.5F, 11F, 3.5F);
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawLine(pen, 2F, 3.2F, 2F, 11.5F);
                    g.DrawLine(pen, 13F, 3.2F, 13F, 11.5F);
                    g.DrawArc(pen, 2F, 4.5F, 11F, 3.5F, 0, 180);
                    g.DrawArc(pen, 2F, 7.5F, 11F, 3.5F, 0, 180);
                    g.DrawArc(pen, 2F, 10.5F, 11F, 3F, 0, 180);
                }
                break;

            case AgentThemeCategory.Mail:
                // Envelope
                using (Pen pen = new(mainColor, 1.3F))
                {
                    g.DrawRectangle(pen, 1.5F, 3F, 12F, 9F);
                    g.DrawLine(pen, 1.5F, 3F, 7.5F, 8F);
                    g.DrawLine(pen, 13.5F, 3F, 7.5F, 8F);
                }
                break;

            case AgentThemeCategory.Web:
                // Globe / Web Browser
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawEllipse(pen, 1.5F, 1.5F, 12F, 12F);
                    g.DrawLine(pen, 1.5F, 7.5F, 13.5F, 7.5F);
                    g.DrawEllipse(pen, 4.5F, 1.5F, 6F, 12F);
                }
                break;

            case AgentThemeCategory.Terminal:
                // Console Prompt (>_)
                using (Pen pen = new(mainColor, 1.3F))
                {
                    g.DrawRectangle(pen, 1.5F, 2F, 12F, 11F);
                    g.DrawLines(pen, [new PointF(4F, 4.5F), new PointF(6.5F, 6.5F), new PointF(4F, 8.5F)]);
                    g.DrawLine(pen, 7.5F, 8.5F, 10.5F, 8.5F);
                }
                break;

            case AgentThemeCategory.FileTransfer:
                // Folder with download arrow
                using (Pen pen = new(mainColor, 1.2F))
                {
                    PointF[] folder = [new(1.5F, 4F), new(5F, 4F), new(6.5F, 5.5F), new(13.5F, 5.5F), new(13.5F, 12.5F), new(1.5F, 12.5F)];
                    g.DrawPolygon(pen, folder);
                    g.DrawLine(pen, 7.5F, 7.5F, 7.5F, 10.5F);
                    g.DrawLines(pen, [new PointF(6F, 9.5F), new PointF(7.5F, 11F), new PointF(9F, 9.5F)]);
                }
                break;

            case AgentThemeCategory.Dns:
                // Hierarchy / Network Tree Nodes
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawEllipse(pen, 6F, 1.5F, 3F, 3F);
                    g.DrawEllipse(pen, 2F, 10.5F, 3F, 3F);
                    g.DrawEllipse(pen, 10F, 10.5F, 3F, 3F);
                    g.DrawLine(pen, 7.5F, 4.5F, 3.5F, 10.5F);
                    g.DrawLine(pen, 7.5F, 4.5F, 11.5F, 10.5F);
                }
                break;

            case AgentThemeCategory.AuthAndShield:
            default:
                // Security Shield with keyhole
                PointF[] shield = [new(7.5F, 1.5F), new(12.5F, 3.5F), new(11.5F, 9.5F), new(7.5F, 13.5F), new(3.5F, 9.5F), new(2.5F, 3.5F)];
                g.FillPolygon(brush, shield);
                using (Pen cutout = new(selected ? DarkIconColor : Color.White, 1.4F))
                {
                    g.DrawEllipse(cutout, 6.2F, 4.5F, 2.6F, 2.6F);
                    g.DrawLine(cutout, 7.5F, 7.1F, 7.5F, 10.5F);
                }
                break;
        }

        return bitmap;
    }

    /// <summary>
    /// Detects the AgentThemeCategory based on agent name or class type.
    /// </summary>
    public static AgentThemeCategory DetectCategory(string agentIdentifier)
    {
        if (string.IsNullOrEmpty(agentIdentifier)) return AgentThemeCategory.AuthAndShield;

        string key = agentIdentifier.ToLowerInvariant();
        if (key.Contains("mysql") || key.Contains("sql") || key.Contains("postgres") || key.Contains("filemaker") || key.Contains("database"))
            return AgentThemeCategory.Database;
        if (key.Contains("mail") || key.Contains("pop3") || key.Contains("imap") || key.Contains("smtp"))
            return AgentThemeCategory.Mail;
        if (key.Contains("web") || key.Contains("iis") || key.Contains("http"))
            return AgentThemeCategory.Web;
        if (key.Contains("ssh") || key.Contains("terminal") || key.Contains("rdp") || key.Contains("remote"))
            return AgentThemeCategory.Terminal;
        if (key.Contains("ftp") || key.Contains("file"))
            return AgentThemeCategory.FileTransfer;
        if (key.Contains("dns") || key.Contains("domain"))
            return AgentThemeCategory.Dns;

        return AgentThemeCategory.AuthAndShield;
    }
}
