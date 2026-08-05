using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SmartForm : Form
{

    readonly Color buttonHighlight = Color.FromArgb(205, 230, 247);
    readonly Color buttonPress = Color.FromArgb(105, 130, 147);
    readonly Color buttonNormal = Color.FromKnownColor(KnownColor.Window);

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartForm"/> class.
    /// </summary>

    public SmartForm()
    {
        InitializeComponent();
        Text = Name;
        labelFormText.Text = Text;

    }


    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxCloseButton_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseDown(object sender, MouseEventArgs e)
    {
        IsMoving = true;
        MoveStartPoint = new Point(e.X, e.Y);
    }

    public bool IsMoving { get; set; }
    public Point MoveStartPoint { get; set; }
    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseUp(object sender, MouseEventArgs e) => IsMoving = false;

    /// <summary>
    /// Handles the mouse move event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelWindowGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsMoving)
        {
            Location = new Point(Location.X + e.X - MoveStartPoint.X, Location.Y + e.Y - MoveStartPoint.Y);

        }
    }


    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBox1_Click(object sender, EventArgs e) => pictureBox1.ContextMenuStrip?.Show(PointToScreen(pictureBox1.Location));

    public event EventHandler? HelpClicked;

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxHelpButon_Click(object sender, EventArgs e) => HelpClicked?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxMinimizeButton_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxMaximizeButton_Click(object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Maximized;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_scale;
        }
        else
        {
            WindowState = FormWindowState.Normal;
            pictureBoxMaximizeButton.Image = Properties.Resources.icon_maximize;
        }
    }


    /// <summary>
    /// Handles the mouse enter event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseEnter(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonHighlight; }

    /// <summary>
    /// Handles the mouse leave event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseLeave(object sender, EventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseDown(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonPress; }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxButton_MouseUp(object sender, MouseEventArgs e) { if (sender is Control control) control.BackColor = buttonNormal; }

    /// <summary>
    /// Executes the resize form operation.
    /// </summary>
    /// <param name="mouseLocation">The mouse location value.</param>

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
        None,
        Top,
        Right,
        Bottom,
        Left
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelContent_MouseDown(object sender, MouseEventArgs e)
    {
        isResizing = (resizeDirection == ResizeDirection.None) ? false : true;
        if (isResizing) resizeStartLocation = e.Location;
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelContent_MouseUp(object sender, MouseEventArgs e) => isResizing = false;

    /// <summary>
    /// Handles the mouse leave event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void panelContent_MouseLeave(object sender, EventArgs e)
    {
        if (!isResizing) resizeDirection = ResizeDirection.None;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderN_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Top;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderS_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderNE_MouseDown(object sender, MouseEventArgs e) => resizeDirection = ResizeDirection.Right | ResizeDirection.Top;

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderSE_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Right | ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderSW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Bottom;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void borderNW_MouseDown(object sender, MouseEventArgs e)
    {
        resizeDirection = ResizeDirection.Left | ResizeDirection.Top;
        resizeStartLocation = e.Location;
        isResizing = true;
    }

    /// <summary>
    /// Handles the mouse move event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void border_MouseMove(object sender, MouseEventArgs e)
    {
        if (isResizing)
        {
            resizeForm(e.Location);
        }
    }




}
