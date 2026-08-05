using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public class SmartPanel : Panel
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartPanel"/> class.
    /// </summary>

    public SmartPanel() => BorderColor = ForeColor;

    public Color BorderColor { get; set; }
    public bool PaintBorder { get; set; }

    /// <summary>
    /// Handles the on paint event.
    /// </summary>
    /// <param name="e">The event data.</param>

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (PaintBorder)
        {
            e.Graphics.DrawRectangle(new Pen(BorderColor), new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}
