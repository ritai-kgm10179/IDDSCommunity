using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SmartLabel : Label
{
    /// <summary>
    /// 初始化 <see cref="SmartLabel"/> 類別的新執行個體。
    /// </summary>

    public SmartLabel() => InitializeComponent();
    /// <summary>
    /// 處理 on paint 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        base.OnPaint(e);
        if (Selected && !SelectedColor.IsEmpty)
        {
            using Pen pen = new(Color.FromArgb(19, 184, 166), 3F);
            int bottom = System.Math.Max(1, Height - 2);
            e.Graphics.DrawLine(pen, 8, bottom, System.Math.Max(8, Width - 9), bottom);
        }
    }

    bool _selected;
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                Invalidate();
            }
        }
    }
    public Color SelectedColor { get; set; }




}
