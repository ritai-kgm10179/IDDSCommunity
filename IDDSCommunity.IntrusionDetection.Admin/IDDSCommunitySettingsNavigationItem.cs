using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunitySettingsNavigationItem : UserControl
{

    public event EventHandler? NavigationClicked;

    /// <summary>
    /// Initializes a new instance of the <see cref="IDDSCommunitySettingsNavigationItem"/> class.
    /// </summary>

    public IDDSCommunitySettingsNavigationItem() => InitializeComponent();

    public bool IsSelected { get; set; }

    public Image? SelectedIcon { get; set; }

    public Image? UnselectedIcon { get; set; }

    public string DisplayName
    {
        get => smartLabelAgentName.Text; set => smartLabelAgentName.Text = value;
    }

    /// <summary>
    /// Handles the on paint event.
    /// </summary>
    /// <param name="e">The event data.</param>

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
    }



    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IDDSCommunitySettingsNavigationItem_MouseDown(object sender, MouseEventArgs e)
    {
        pictureBoxNavigationIcon.Location = new Point(pictureBoxNavigationIcon.Location.X + 1, pictureBoxNavigationIcon.Location.Y + 1);
        smartLabelAgentName.Location = new Point(smartLabelAgentName.Location.X + 1, smartLabelAgentName.Location.Y + 1);
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IDDSCommunitySettingsNavigationItem_MouseUp(object sender, MouseEventArgs e)
    {
        pictureBoxNavigationIcon.Location = new Point(pictureBoxNavigationIcon.Location.X - 1, pictureBoxNavigationIcon.Location.Y - 1);
        smartLabelAgentName.Location = new Point(smartLabelAgentName.Location.X - 1, smartLabelAgentName.Location.Y - 1);
    }



    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void IDDSCommunitySettingsNavigationItem_Click(object sender, EventArgs e) => OnNavigationClicked();

    /// <summary>
    /// Processes the navigation clicked notification.
    /// </summary>

    private void OnNavigationClicked() => NavigationClicked?.Invoke(this, EventArgs.Empty);



}
