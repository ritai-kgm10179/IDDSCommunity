using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供設定功能導覽樹狀選單之使用者控制項。
/// </summary>
public partial class IDDSCommunitySettingsNavigation : UserControl
{
    /// <summary>
    /// 初始化 <see cref="IDDSCommunitySettingsNavigation"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunitySettingsNavigation()
    {
        InitializeComponent();
        flowLayoutPanelNavigationItems.AutoScroll = true;
        flowLayoutPanelNavigationItems.WrapContents = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Enabled = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Visible = false;
        UpdateNavigationListLayout();
    }

    /// <summary>
    /// 當 NavigationChanged 時引發之事件。
    /// </summary>
    public event EventHandler? NavigationChanged;

    /// <summary>
    /// 取得或設定 SeparatorColor。
    /// </summary>
    public Color SeparatorColor { get; set; }

    /// <summary>
    /// 取得或設定 ShowSeparator。
    /// </summary>
    public bool ShowSeparator { get; set; }
    /// <summary>
    /// 處理 on paint 事件。僅負責繪製，不得在此變更子控制項版面——否則會觸發
    /// Invalidate → Layout → Paint 的重入迴圈（表現為閃爍或版面計算失敗）。
    /// 子控制項的定位改由 <see cref="OnLayout"/> 處理。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ShowSeparator)
        {
            using Pen separatorPen = new(SeparatorColor, 1);
            int separatorX = Math.Max(0, ClientSize.Width - 1);
            e.Graphics.DrawLine(separatorPen, separatorX, 0, separatorX, ClientSize.Height);
        }
    }

    /// <summary>
    /// 重新定位導覽項目清單。
    /// </summary>
    private void UpdateNavigationListLayout()
    {
        // flowLayoutPanelNavigationItems 設有 Anchor = Top|Bottom|Left|Right，理論上應會隨父容器
        // （本控制項）縮放自動調整寬度。但 AutoScaleMode.Font 在放大本控制項的 Size 時，並未經過
        // 會觸發 Anchor 重新計算的一般 Resize 流程，導致寬度殘留放大前的舊值。在此手動同步寬度，
        // 不再單靠 Anchor 機制。
        int separatorGutterWidth = ShowSeparator ? LogicalToDeviceUnits(5) : 0;
        flowLayoutPanelNavigationItems.Width = Math.Max(0, ClientSize.Width - separatorGutterWidth);
        flowLayoutPanelNavigationItems.Top = 0;
        flowLayoutPanelNavigationItems.Height = Height - 1;
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void iddscommunitySettingsNavigationItem_Click(object? sender, EventArgs e)
    {
        if (sender is IDDSCommunitySettingsNavigationItem item && !item.IsSelected)
        {
            UnselectAll();
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    private List<IDDSCommunitySettingsNavigationItem>? _navigationItems;
    private List<IDDSCommunitySettingsNavigationItem> NavigationItems
    {
        get
        {
            _navigationItems ??= [];
            return _navigationItems;
        }
    }
    /// <summary>
    /// Adds navigation item.
    /// </summary>
    /// <param name="item">item 的值。</param>
    public void AddNavigationItem(IDDSCommunitySettingsNavigationItem item) => NavigationItems.Add(item);
    /// <summary>
    /// Adds navigation item.
    /// </summary>
    /// <param name="name">name 的值。</param>
    /// <param name="selectedIcon">selected icon 的值。</param>
    /// <param name="unselectedIcon">unselected icon 的值。</param>
    public void AddNavigationItem(string name, Image? selectedIcon, Image? unselectedIcon)
    {
        int clientW = flowLayoutPanelNavigationItems.ClientSize.Width;
        int targetWidth = Math.Max(200, clientW > 10 ? clientW - 8 : 330);
        IDDSCommunitySettingsNavigationItem item = new()
        {
            SelectedIcon = selectedIcon,
            DisplayName = name,
            UnselectedIcon = unselectedIcon,
            Width = targetWidth
        };
        flowLayoutPanelNavigationItems.Controls.Add(item);
        item.NavigationClicked += new EventHandler(iddscommunitySettingsNavigationItem_Click);
        UpdateItemWidths();
        if (flowLayoutPanelNavigationItems.Controls.Count == 1)
        {
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateNavigationListLayout();
        UpdateItemWidths();
    }

    /// <inheritdoc/>
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        UpdateNavigationListLayout();
        UpdateItemWidths();
    }

    /// <summary>
    /// 依據導覽容器實際可視 Client 寬度自動調整所有項目寬度，消除水平捲軸。
    /// </summary>
    private void UpdateItemWidths()
    {
        if (flowLayoutPanelNavigationItems == null) return;
        flowLayoutPanelNavigationItems.HorizontalScroll.Enabled = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Visible = false;

        int clientW = flowLayoutPanelNavigationItems.ClientSize.Width;
        if (clientW <= 0) return;

        int targetWidth = Math.Max(200, clientW - 8);
        flowLayoutPanelNavigationItems.SuspendLayout();
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item && item.Width != targetWidth)
            {
                item.Width = targetWidth;
            }
        }
        flowLayoutPanelNavigationItems.ResumeLayout(true);
    }
    /// <summary>
    /// Clears requested operation.
    /// </summary>
    public void Clear()
    {
        NavigationItems.Clear();
        while (flowLayoutPanelNavigationItems.Controls.Count > 0)
        {
            Control child = flowLayoutPanelNavigationItems.Controls[0];
            flowLayoutPanelNavigationItems.Controls.RemoveAt(0);
            child.Dispose();
        }
    }


        /// <summary>
    /// 取得或設定 SelectedItem。
    /// </summary>
public IDDSCommunitySettingsNavigationItem? SelectedItem
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is IDDSCommunitySettingsNavigationItem item && item.IsSelected) return item;
            }
            return null;
        }
    }
    /// <summary>
    /// Sets selected item.
    /// </summary>
    /// <param name="name">name 的值。</param>
    public void SetSelectedItem(string name)
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item && item.DisplayName.Equals(name, StringComparison.Ordinal))
            {
                UnselectAll();
                item.IsSelected = true;
                flowLayoutPanelNavigationItems.ScrollControlIntoView(item);
                flowLayoutPanelNavigationItems.AutoScrollPosition = new Point(0, flowLayoutPanelNavigationItems.VerticalScroll.Value);
                OnNavigationChanged(item);
            }
        }
    }
    /// <summary>
    /// Processes the navigation changed notification.
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    private void OnNavigationChanged(object sender) => NavigationChanged?.Invoke(sender, EventArgs.Empty);

        /// <summary>
    /// 取得或設定 SelectedName。
    /// </summary>
public string SelectedName
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is IDDSCommunitySettingsNavigationItem item && item.IsSelected) return item.DisplayName;
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// 執行 unselect all 作業。
    /// </summary>
    public void UnselectAll()
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    c.Invalidate();
                }
            }
        }
    }
}
