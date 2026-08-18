using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供具備現代化無邊框設計、拖曳移動與陰影效果之自訂基礎表單。
/// </summary>
public partial class SmartForm : Form
{

    readonly Color buttonHighlight = Color.FromArgb(205, 230, 247);
    readonly Color buttonPress = Color.FromArgb(105, 130, 147);
    readonly Color buttonNormal = Color.FromKnownColor(KnownColor.Window);
    /// <summary>
    /// 初始化 <see cref="SmartForm"/> 類別的新執行個體。
    /// </summary>
    public SmartForm()
    {
        InitializeComponent();
        Icon = BrandingIcons.CreateIcon();
        BrandingIcons.ApplyTo(pictureBox1);
        Text = Name;
        labelFormText.Text = Text;

    }

    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxCloseButton_Click(object sender, EventArgs e) => Close();
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseDown(object sender, MouseEventArgs e)
    {
        IsMoving = true;
        MoveStartPoint = new Point(e.X, e.Y);
    }

        /// <summary>
    /// 取得或設定 視窗是否正在拖曳移動中。
    /// </summary>
public bool IsMoving { get; set; }
        /// <summary>
    /// 取得或設定 視窗拖曳起始座標點。
    /// </summary>
public Point MoveStartPoint { get; set; }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseUp(object sender, MouseEventArgs e) => IsMoving = false;
    /// <summary>
    /// 處理 mouse move 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelWindowGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsMoving)
        {
            Location = new Point(Location.X + e.X - MoveStartPoint.X, Location.Y + e.Y - MoveStartPoint.Y);

        }
    }

    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox1_Click(object sender, EventArgs e) => pictureBox1.ContextMenuStrip?.Show(PointToScreen(pictureBox1.Location));

        /// <summary>
    /// 當 說明按鈕點擊事件 時引發之事件。
    /// </summary>
public event EventHandler? HelpClicked;
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxHelpButon_Click(object sender, EventArgs e) => HelpClicked?.Invoke(this, EventArgs.Empty);
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxMinimizeButton_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxMaximizeButton_Click(object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Maximized)
        {
            MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
            WindowState = FormWindowState.Maximized;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_scale;
        }
        else
        {
            WindowState = FormWindowState.Normal;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_maximize;
        }
    }

    private const int WM_GETMINMAXINFO = 0x0024;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT
    {
                /// <summary>
        /// 定義 x 之數值。
        /// </summary>
public int x;
                /// <summary>
        /// 定義 y 之數值。
        /// </summary>
public int y;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
                /// <summary>
        /// 定義 ptReserved 之數值。
        /// </summary>
public POINT ptReserved;
                /// <summary>
        /// 定義 ptMaxSize 之數值。
        /// </summary>
public POINT ptMaxSize;
                /// <summary>
        /// 定義 ptMaxPosition 之數值。
        /// </summary>
public POINT ptMaxPosition;
                /// <summary>
        /// 定義 ptMinTrackSize 之數值。
        /// </summary>
public POINT ptMinTrackSize;
                /// <summary>
        /// 定義 ptMaxTrackSize 之數值。
        /// </summary>
public POINT ptMaxTrackSize;
    }

    /// <summary>
    /// 處理視窗訊息以支援自訂最大化範圍與無邊框拖曳。
    /// </summary>
        /// <param name="m">Windows 視窗訊息。</param>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_GETMINMAXINFO && m.LParam != IntPtr.Zero)
        {
            MINMAXINFO mmi = (MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO))!;
            Screen screen = Screen.FromHandle(Handle);
            Rectangle workingArea = screen.WorkingArea;
            Rectangle bounds = screen.Bounds;

            mmi.ptMaxPosition.x = Math.Abs(workingArea.Left - bounds.Left);
            mmi.ptMaxPosition.y = Math.Abs(workingArea.Top - bounds.Top);
            mmi.ptMaxSize.x = workingArea.Width;
            mmi.ptMaxSize.y = workingArea.Height;

            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, m.LParam, true);
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    /// <summary>
    /// 處理 mouse enter 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseEnter(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonHighlight; }
    /// <summary>
    /// 處理 mouse leave 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseLeave(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseDown(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonPress; }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxButton_MouseUp(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }
    /// <summary>
    /// 執行 resize form 作業。
    /// </summary>
    /// <param name="mouseLocation">mouse location 的值。</param>
    private void resizeForm(Point mouseLocation)
    {
        int deltaX = resizeStartLocation.X - mouseLocation.X;
        int deltaY = resizeStartLocation.Y - mouseLocation.Y;
        if ((resizeDirection & ResizeDirection.Left) == ResizeDirection.Left)
        {
            Left += -deltaX;
            Width += deltaX;
        }
        else if ((resizeDirection & ResizeDirection.Right) == ResizeDirection.Right)
        {
            Width -= deltaX;
            resizeStartLocation = mouseLocation;
        }

    }

    ResizeDirection resizeDirection = ResizeDirection.None;
    bool isResizing = false;
    Point resizeStartLocation = new(0, 0);

    enum ResizeDirection
    {
                /// <summary>
        /// 定義 None 列舉值。
        /// </summary>
None,
                /// <summary>
        /// 定義 Top 列舉值。
        /// </summary>
Top,
                /// <summary>
        /// 定義 Right 列舉值。
        /// </summary>
Right,
                /// <summary>
        /// 定義 Bottom 列舉值。
        /// </summary>
Bottom,
                /// <summary>
        /// 定義 Left 列舉值。
        /// </summary>
Left
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelContent_MouseDown(object sender, MouseEventArgs e)
    {
        isResizing = (resizeDirection == ResizeDirection.None) ? false : true;
        if (isResizing) resizeStartLocation = e.Location;
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelContent_MouseUp(object sender, MouseEventArgs e) => isResizing = false;
    /// <summary>
    /// 處理 mouse leave 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void panelContent_MouseLeave(object sender, EventArgs e)
    {
        if (!isResizing) resizeDirection = ResizeDirection.None;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderN_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Top;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderS_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderNE_MouseDown(object sender, MouseEventArgs e) => resizeDirection = ResizeDirection.Right | ResizeDirection.Top;
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderSE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderSW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void borderNW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Top;
        resizeStartLocation = e.Location;
        isResizing = true;
    }
    /// <summary>
    /// 處理 mouse move 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void border_MouseMove(object sender, MouseEventArgs e)
    {
        if (isResizing)
        {
            resizeForm(e.Location);
        }
    }




}
