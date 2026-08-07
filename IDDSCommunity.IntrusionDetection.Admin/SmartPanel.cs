using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public class SmartPanel : Panel
{
    /// <summary>
    /// 初始化 <see cref="SmartPanel"/> 類別的新執行個體。
    /// </summary>
    public SmartPanel() => BorderColor = ForeColor;

    public Color BorderColor { get; set; }
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
