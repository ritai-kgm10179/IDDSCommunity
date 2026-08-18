using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供雙重緩衝繪圖以避免閃爍之自訂面板控制項。
/// </summary>
public class SmartPanel : Panel
{
    /// <summary>
    /// 初始化 <see cref="SmartPanel"/> 類別的新執行個體。
    /// </summary>
    public SmartPanel() => BorderColor = ForeColor;

        /// <summary>
    /// 取得或設定 外框繪製色彩。
    /// </summary>
public Color BorderColor { get; set; }
        /// <summary>
    /// 取得或設定 是否繪製自訂外框。
    /// </summary>
public bool PaintBorder { get; set; }
    /// <summary>
    /// 處理 on paint 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (PaintBorder)
        {
            e.Graphics.DrawRectangle(new Pen(BorderColor), new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}
