using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義代理程式主題圖示類別。
/// </summary>
public enum AgentThemeCategory
{
    /// <summary>
    /// 資料庫伺服器類別代理程式。
    /// </summary>
    Database,

    /// <summary>
    /// 電子郵件伺服器類別代理程式。
    /// </summary>
    Mail,

    /// <summary>
    /// Web 站台與 HTTP 服務類別代理程式。
    /// </summary>
    Web,

    /// <summary>
    /// 系統與遠端桌面存取類別代理程式。
    /// </summary>
    Terminal,

    /// <summary>
    /// 檔案傳輸 (FTP / FileZilla) 類別代理程式。
    /// </summary>
    FileTransfer,

    /// <summary>
    /// 網域名稱系統 (DNS) 類別代理程式。
    /// </summary>
    Dns,

    /// <summary>
    /// 主動誘餌與蜜罐防禦類別代理程式。
    /// </summary>
    Honeypot,

    /// <summary>
    /// 驗證與系統安全類別代理程式。
    /// </summary>
    AuthAndShield
}

/// <summary>
/// 產生高品質 16x16 向量主題圖示的代理程式圖示工廠。
/// </summary>
public static class AgentThemeIconFactory
{
    private static readonly Color DarkIconColor = Color.FromArgb(0, 150, 136); // Teal 主題強調色

    /// <summary>
    /// 依指定主題類別與選取狀態建立 16x16 向量圖示點陣圖。
    /// </summary>
    /// <param name="category">主題分類。</param>
    /// <param name="selected">是否處於選取狀態。</param>
    /// <param name="customAccent">自訂強調色。</param>
    /// <returns>16x16 點陣圖執行個體。</returns>
    public static Bitmap Create(AgentThemeCategory category, bool selected, Color? customAccent = null)
    {
        Bitmap bitmap = new(16, 16);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        Color mainColor = selected ? Color.White : (customAccent ?? DarkIconColor);
        using SolidBrush brush = new(mainColor);

        switch (category)
        {
            case AgentThemeCategory.Database:
                // 3D 圓柱資料庫堆疊
                g.FillEllipse(brush, 2F, 1.5F, 12F, 3.5F);
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawLine(pen, 2F, 3.2F, 2F, 12F);
                    g.DrawLine(pen, 14F, 3.2F, 14F, 12F);
                    g.DrawArc(pen, 2F, 5F, 12F, 3.5F, 0, 180);
                    g.DrawArc(pen, 2F, 8.5F, 12F, 3.5F, 0, 180);
                    g.DrawArc(pen, 2F, 11.5F, 12F, 3.2F, 0, 180);
                }
                break;

            case AgentThemeCategory.Mail:
                // 郵件信封
                using (Pen pen = new(mainColor, 1.3F))
                {
                    g.DrawRectangle(pen, 1.5F, 3F, 13F, 10F);
                    g.DrawLine(pen, 1.5F, 3F, 8F, 8.5F);
                    g.DrawLine(pen, 14.5F, 3F, 8F, 8.5F);
                }
                break;

            case AgentThemeCategory.Web:
                // Web 地球儀
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawEllipse(pen, 1.5F, 1.5F, 13F, 13F);
                    g.DrawLine(pen, 1.5F, 8F, 14.5F, 8F);
                    g.DrawEllipse(pen, 5F, 1.5F, 6F, 13F);
                }
                break;

            case AgentThemeCategory.Terminal:
                // 終端機視窗提示符號 (>_)
                using (Pen pen = new(mainColor, 1.3F))
                {
                    g.DrawRectangle(pen, 1.5F, 2F, 13F, 12F);
                    g.DrawLines(pen, [new PointF(4F, 5F), new PointF(7F, 7.5F), new PointF(4F, 10F)]);
                    g.DrawLine(pen, 8.5F, 10F, 11.5F, 10F);
                }
                break;

            case AgentThemeCategory.FileTransfer:
                // 檔案夾與傳輸箭頭
                using (Pen pen = new(mainColor, 1.2F))
                {
                    PointF[] folder = [new(1.5F, 4F), new(5.5F, 4F), new(7F, 5.5F), new(14.5F, 5.5F), new(14.5F, 13.5F), new(1.5F, 13.5F)];
                    g.DrawPolygon(pen, folder);
                    g.DrawLine(pen, 8F, 8F, 8F, 11.5F);
                    g.DrawLines(pen, [new PointF(6.5F, 10F), new PointF(8F, 12F), new PointF(9.5F, 10F)]);
                }
                break;

            case AgentThemeCategory.Dns:
                // DNS 網路層級分支節點
                using (Pen pen = new(mainColor, 1.2F))
                {
                    g.DrawEllipse(pen, 6.5F, 1.5F, 3F, 3F);
                    g.DrawEllipse(pen, 2F, 11.5F, 3F, 3F);
                    g.DrawEllipse(pen, 11F, 11.5F, 3F, 3F);
                    g.DrawLine(pen, 8F, 4.5F, 3.5F, 11.5F);
                    g.DrawLine(pen, 8F, 4.5F, 12.5F, 11.5F);
                }
                break;

            case AgentThemeCategory.Honeypot:
                // 主動蜜罐與誘餌防禦盾牌 (含蜂巢核心)
                PointF[] honeyShield = [new(8F, 1.5F), new(13.5F, 3.5F), new(12.5F, 10.5F), new(8F, 14.5F), new(3.5F, 10.5F), new(2.5F, 3.5F)];
                g.FillPolygon(brush, honeyShield);
                using (Pen cutoutPen = new(selected ? DarkIconColor : Color.White, 1.3F))
                {
                    PointF[] hex = [new(8F, 5F), new(10.5F, 6.5F), new(10.5F, 9.5F), new(8F, 11F), new(5.5F, 9.5F), new(5.5F, 6.5F)];
                    g.DrawPolygon(cutoutPen, hex);
                }
                break;

            case AgentThemeCategory.AuthAndShield:
            default:
                // 系統安全防禦盾牌 (含鎖孔)
                PointF[] shield = [new(8F, 1.5F), new(13.5F, 3.5F), new(12.5F, 10.5F), new(8F, 14.5F), new(3.5F, 10.5F), new(2.5F, 3.5F)];
                g.FillPolygon(brush, shield);
                using (Pen cutout = new(selected ? DarkIconColor : Color.White, 1.4F))
                {
                    g.DrawEllipse(cutout, 6.7F, 5F, 2.6F, 2.6F);
                    g.DrawLine(cutout, 8F, 7.6F, 8F, 11F);
                }
                break;
        }

        return bitmap;
    }

    /// <summary>
    /// 依代理程式名稱或型別名稱自動偵測對應之 AgentThemeCategory。
    /// </summary>
    /// <param name="agentIdentifier">代理程式識別字串。</param>
    /// <returns>符合的主題類別列舉值。</returns>
    public static AgentThemeCategory DetectCategory(string agentIdentifier)
    {
        if (string.IsNullOrEmpty(agentIdentifier)) return AgentThemeCategory.AuthAndShield;

        string key = agentIdentifier.ToLowerInvariant();
        if (key.Contains("honeypot") || key.Contains("蜜罐") || key.Contains("誘餌") || key.Contains("decoy"))
            return AgentThemeCategory.Honeypot;
        if (key.Contains("mysql") || key.Contains("sql") || key.Contains("postgres") || key.Contains("filemaker") || key.Contains("database") || key.Contains("資料庫"))
            return AgentThemeCategory.Database;
        if (key.Contains("mail") || key.Contains("pop3") || key.Contains("imap") || key.Contains("smtp") || key.Contains("郵件"))
            return AgentThemeCategory.Mail;
        if (key.Contains("web") || key.Contains("iis") || key.Contains("http"))
            return AgentThemeCategory.Web;
        if (key.Contains("technitium") || key.Contains("dns") || key.Contains("domain") || key.Contains("網域"))
            return AgentThemeCategory.Dns;
        if (key.Contains("ssh") || key.Contains("terminal") || key.Contains("rdp") || key.Contains("winrm") || key.Contains("remote") || key.Contains("遠端") || key.Contains("gateway"))
            return AgentThemeCategory.Terminal;
        if (key.Contains("ftp") || key.Contains("filezilla") || key.Contains("檔案傳輸"))
            return AgentThemeCategory.FileTransfer;

        return AgentThemeCategory.AuthAndShield;
    }
}
