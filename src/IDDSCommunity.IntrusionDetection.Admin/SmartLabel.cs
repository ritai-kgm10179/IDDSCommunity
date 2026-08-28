using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供平滑抗鋸齒文字呈現與高對比焦點框之自訂標籤控制項。
/// </summary>
public partial class SmartLabel : Label
{
    /// <summary>
    /// 初始化 <see cref="SmartLabel"/> 類別的新執行個體。
    /// </summary>
    public SmartLabel()
    {
        InitializeComponent();
        AccessibleRole = AccessibleRole.StaticText;
    }

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
            int bottom = Math.Max(1, Height - 2);
            e.Graphics.DrawLine(pen, 8, bottom, Math.Max(8, Width - 9), bottom);
        }
        if (Focused && TabStop)
        {
            using Pen focusPen = new(Color.FromArgb(19, 184, 166), 1F) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            Rectangle focusRect = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            e.Graphics.DrawRectangle(focusPen, focusRect);
        }
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    bool _selected;
    /// <summary>
    /// 取得或設定 是否為已選取狀態。
    /// </summary>
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
    /// <summary>
    /// 取得或設定 選取狀態之醒目提示色彩。
    /// </summary>
    public Color SelectedColor { get; set; }
}
