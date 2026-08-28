using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 代表設定功能導覽項目之單元控制項。
/// </summary>
public partial class IDDSCommunitySettingsNavigationItem : UserControl
{

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
    }

    /// <summary>
    /// 取得或設定 IsSelected。
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// 取得或設定 SelectedIcon。
    /// </summary>
    public Image? SelectedIcon { get; set; }

    /// <summary>
    /// 取得或設定 UnselectedIcon。
    /// </summary>
    public Image? UnselectedIcon { get; set; }

    /// <summary>
    /// 取得或設定 本地化顯示名稱。
    /// </summary>
    public string DisplayName
    {
        get => smartLabelAgentName.Text;
        set
        {
            smartLabelAgentName.Text = value;
            AccessibleName = value;
            AccessibleDescription = value;
        }
    }
    /// <summary>
    /// 處理 on paint 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (IsSelected)
        {
            BackColor = Color.FromArgb(4, 46, 100);
            smartLabelAgentName.ForeColor = Color.White;
            pictureBoxNavigationIcon.Image = SelectedIcon;
        }
        else
        {
            BackColor = Color.White;
            smartLabelAgentName.ForeColor = Color.FromArgb(0x666666);
            pictureBoxNavigationIcon.Image = UnselectedIcon;
        }
        base.OnPaint(e);
        if (Focused)
        {
            using Pen focusPen = new(Color.FromArgb(19, 184, 166), 1.5F) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            Rectangle focusRect = new(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            e.Graphics.DrawRectangle(focusPen, focusRect);
        }
    }


    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void IDDSCommunitySettingsNavigationItem_MouseDown(object sender, MouseEventArgs e)
    {
        pictureBoxNavigationIcon.Location = new Point(pictureBoxNavigationIcon.Location.X + 1, pictureBoxNavigationIcon.Location.Y + 1);
        smartLabelAgentName.Location = new Point(smartLabelAgentName.Location.X + 1, smartLabelAgentName.Location.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void IDDSCommunitySettingsNavigationItem_MouseUp(object sender, MouseEventArgs e)
    {
        pictureBoxNavigationIcon.Location = new Point(pictureBoxNavigationIcon.Location.X - 1, pictureBoxNavigationIcon.Location.Y - 1);
        smartLabelAgentName.Location = new Point(smartLabelAgentName.Location.X - 1, smartLabelAgentName.Location.Y - 1);
    }


    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void IDDSCommunitySettingsNavigationItem_Click(object sender, EventArgs e) => OnNavigationClicked();
    /// <summary>
    /// Processes the navigation clicked notification.
    /// </summary>
    private void OnNavigationClicked() => NavigationClicked?.Invoke(this, EventArgs.Empty);



}
