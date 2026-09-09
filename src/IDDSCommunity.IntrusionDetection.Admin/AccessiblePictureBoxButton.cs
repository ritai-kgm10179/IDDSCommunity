using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 可透過鍵盤操作、具備可見焦點框與 <see cref="AccessibleRole.PushButton"/> 角色的 <see cref="PictureBox"/>。
/// 用來取代整個專案裡原本純滑鼠、無法用 Tab 到達、無鍵盤等效操作的圖示按鈕（儲存、編輯、新增、刪除等），
/// 修正 WCAG 2.1.1（鍵盤可操作）與 2.4.7/2.4.11（焦點可見）缺失。焦點與鍵盤處理手法沿用本專案中
/// <see cref="IDDSCommunitySettingsNavigationItem"/> 已驗證可行的相同模式，維持視覺與行為一致。
/// </summary>
public class AccessiblePictureBoxButton : PictureBox
{
    /// <summary>
    /// 初始化 <see cref="AccessiblePictureBoxButton"/> 類別的新執行個體。
    /// </summary>
    public AccessiblePictureBoxButton()
    {
        SetStyle(ControlStyles.Selectable | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
    }

    /// <summary>
    /// 處理 got focus 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <summary>
    /// 處理 lost focus 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    /// <summary>
    /// 處理 on paint 事件：先繪製底層圖示，再疊畫鍵盤焦點框線（僅在 <see cref="Control.Focused"/> 時）。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Focused)
        {
            using Pen focusPen = new(Color.FromArgb(15, 118, 110), 1F) { DashStyle = DashStyle.Dot };
            Rectangle focusRect = new(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            e.Graphics.DrawRectangle(focusPen, focusRect);
        }
    }

    /// <summary>
    /// 處理鍵盤按鍵事件（Enter 或 Space 觸發既有的 Click 事件，等效於滑鼠點擊）。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Enabled && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space))
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }
}
