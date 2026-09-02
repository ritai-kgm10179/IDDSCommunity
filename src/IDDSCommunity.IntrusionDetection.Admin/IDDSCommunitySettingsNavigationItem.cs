using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 代表設定功能導覽項目之單元控制項。
/// </summary>
public partial class IDDSCommunitySettingsNavigationItem : UserControl
{
    private string _displayName = string.Empty;
    private bool _isSelected;
    private Image? _selectedIcon;
    private Image? _unselectedIcon;
    private bool _isPressed;

    /// <summary>
    /// 當 NavigationClicked 時引發之事件。
    /// </summary>
    public event EventHandler? NavigationClicked;

    /// <summary>
    /// 初始化 <see cref="IDDSCommunitySettingsNavigationItem"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunitySettingsNavigationItem()
    {
        InitializeComponent();
        AccessibleRole = AccessibleRole.PushButton;
        DoubleBuffered = true;
        SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Margin = new Padding(3, 1, 3, 1);
        Font = new Font("Segoe UI", 9F);
        Cursor = Cursors.Hand;
    }

    /// <summary>
    /// 取得或設定 IsSelected。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 取得或設定 SelectedIcon。
    /// </summary>
    public Image? SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            _selectedIcon = value;
            if (_isSelected) Invalidate();
        }
    }

    /// <summary>
    /// 取得或設定 UnselectedIcon。
    /// </summary>
    public Image? UnselectedIcon
    {
        get => _unselectedIcon;
        set
        {
            _unselectedIcon = value;
            if (!_isSelected) Invalidate();
        }
    }

    /// <summary>
    /// 取得或設定 本地化顯示名稱。
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value ?? string.Empty;
            AccessibleName = _displayName;
            AccessibleDescription = _displayName;
            Invalidate();
        }
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
    /// 處理 on paint 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // 1. 繪製背景色
        Color backColor = IsSelected ? Color.FromArgb(4, 46, 100) : Color.White;
        using (SolidBrush bgBrush = new(backColor))
        {
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);
        }

        int offset = _isPressed ? 1 : 0;

        // 2. 繪製圖示 (置中於左側 6px)
        Image? icon = IsSelected ? SelectedIcon : UnselectedIcon;
        if (icon != null)
        {
            int iconX = 6 + offset;
            int iconY = Math.Max(0, (Height - icon.Height) / 2) + offset;
            e.Graphics.DrawImage(icon, iconX, iconY, icon.Width, icon.Height);
        }

        // 3. 繪製文字
        Color textColor = IsSelected ? Color.White : Color.FromArgb(0x66, 0x66, 0x66);
        Rectangle textRect = new(32 + offset, offset, Math.Max(0, Width - 36), Height);
        TextRenderer.DrawText(
            e.Graphics,
            DisplayName,
            Font,
            textRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // 4. 繪製鍵盤焦點框線 (完整包覆無任何子控制項覆蓋)
        if (Focused)
        {
            using Pen focusPen = new(Color.FromArgb(19, 184, 166), 1F) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            Rectangle focusRect = new(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            e.Graphics.DrawRectangle(focusPen, focusRect);
        }
    }

    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Focus();
            Invalidate();
        }
    }

    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isPressed)
        {
            _isPressed = false;
            Invalidate();
        }
    }

    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        OnNavigationClicked();
    }

    /// <summary>
    /// 處理鍵盤按鍵事件 (Enter 或 Space 觸發選取)。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
        {
            OnNavigationClicked();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Processes the navigation clicked notification.
    /// </summary>
    private void OnNavigationClicked() => NavigationClicked?.Invoke(this, EventArgs.Empty);
}
