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
        using Pen pen = new(Selected ? SelectedColor : BackColor);
        int bottom = System.Math.Max(0, Height - 1);
        e.Graphics.DrawLines(pen, [ new(0, bottom),
            new(0, 0),
            new(Width-1, 0),
            new(Width-1, bottom) ]);
        base.OnPaint(e);

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
