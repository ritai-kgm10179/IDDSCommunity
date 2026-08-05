using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDDSCommunity.Agents.Authentication.Common;

internal static class AgentIconFactory
{
    internal static Bitmap Create(Color accent, bool selected)
    {
        Bitmap bitmap = new(15, 15);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        Color color = selected ? Color.White : accent;
        using SolidBrush brush = new(color);
        PointF[] shield = [new(7.5F, 1), new(12.5F, 3), new(11.5F, 9), new(7.5F, 13.5F), new(3.5F, 9), new(2.5F, 3)];
        graphics.FillPolygon(brush, shield);
        using Pen cutout = new(Color.Transparent, 1.6F);
        graphics.DrawLine(cutout, 5.5F, 7.5F, 9.5F, 7.5F);
        return bitmap;
    }
}
