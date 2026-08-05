using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SmartLabel : Label
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SmartLabel"/> class.
    /// </summary>

    public SmartLabel() => InitializeComponent();

    /// <summary>
    /// Handles the on paint event.
    /// </summary>
    /// <param name="e">The event data.</param>

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
