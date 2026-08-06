using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// Draws original, semantic interface icons without reusing the application brand mark.
/// </summary>
internal static class InterfaceIcons
{
    private static readonly Color Teal = Color.FromArgb(19, 154, 166);
    private static readonly Color Navy = Color.FromArgb(13, 56, 80);

    internal static Bitmap CreateLock(int size, bool unlocked = false)
    {
        Bitmap bitmap = Canvas(size, out Graphics graphics);
        using (graphics)
        using (Pen shackle = new(Navy, System.Math.Max(2F, size * 0.09F)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (SolidBrush body = new(Teal))
        using (SolidBrush keyhole = new(Navy))
        {
            float x = size * 0.2F;
            float y = size * 0.43F;
            float width = size * 0.6F;
            float height = size * 0.43F;
            graphics.FillRoundedRectangle(body, new RectangleF(x, y, width, height), size * 0.09F);

            RectangleF shackleBounds = unlocked
                ? new RectangleF(size * 0.42F, size * 0.13F, size * 0.42F, size * 0.48F)
                : new RectangleF(size * 0.31F, size * 0.13F, size * 0.38F, size * 0.48F);
            graphics.DrawArc(shackle, shackleBounds, 180F, -180F);
            if (!unlocked)
            {
                graphics.DrawLine(shackle, shackleBounds.Left, shackleBounds.Top + shackleBounds.Height / 2F, shackleBounds.Left, y);
                graphics.DrawLine(shackle, shackleBounds.Right, shackleBounds.Top + shackleBounds.Height / 2F, shackleBounds.Right, y);
            }
            else
            {
                graphics.DrawLine(shackle, shackleBounds.Right, shackleBounds.Top + shackleBounds.Height / 2F, shackleBounds.Right, y);
            }

            graphics.FillEllipse(keyhole, size * 0.45F, size * 0.57F, size * 0.1F, size * 0.1F);
        }
        return bitmap;
    }

    internal static Bitmap CreateSecurityLog(int size)
    {
        Bitmap bitmap = Canvas(size, out Graphics graphics);
        using (graphics)
        using (Pen outline = new(Navy, System.Math.Max(2F, size * 0.07F)) { LineJoin = LineJoin.Round })
        using (Pen line = new(Teal, System.Math.Max(2F, size * 0.07F)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            graphics.DrawRectangle(outline, size * 0.2F, size * 0.12F, size * 0.6F, size * 0.76F);
            for (int index = 0; index < 3; index++)
            {
                float y = size * (0.35F + index * 0.17F);
                graphics.DrawLine(line, size * 0.33F, y, size * 0.67F, y);
            }
        }
        return bitmap;
    }

    internal static Bitmap CreateAgentStatus(int size, bool enabled)
    {
        Bitmap bitmap = Canvas(size, out Graphics graphics);
        using (graphics)
        using (Pen outline = new(enabled ? Teal : Color.FromArgb(128, 128, 128), System.Math.Max(2F, size * 0.1F))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        })
        {
            graphics.DrawEllipse(outline, size * 0.15F, size * 0.15F, size * 0.7F, size * 0.7F);
            if (enabled)
            {
                graphics.DrawLines(outline,
                [
                    new PointF(size * 0.31F, size * 0.52F),
                    new PointF(size * 0.45F, size * 0.66F),
                    new PointF(size * 0.72F, size * 0.36F)
                ]);
            }
            else
            {
                graphics.DrawLine(outline, size * 0.3F, size * 0.3F, size * 0.7F, size * 0.7F);
            }
        }
        return bitmap;
    }

    private static Bitmap Canvas(int size, out Graphics graphics)
    {
        Bitmap bitmap = new(size, size);
        graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        return bitmap;
    }

    private static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
    {
        using GraphicsPath path = new();
        float diameter = radius * 2F;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180F, 90F);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270F, 90F);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0F, 90F);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90F, 90F);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
