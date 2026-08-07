using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;
/// <summary>
/// Provides every window and branded surface with images from the executable's canonical multi-size icon.
/// </summary>
internal static class BrandingIcons
{
    private const string IconResourceName = "IDDSCommunity.Branding.idds-community.ico";
    private static readonly MemoryStream ApplicationIconStream = LoadApplicationIconStream();
    private static readonly Icon ApplicationIcon = new(ApplicationIconStream);

    internal static Icon CreateIcon() => (Icon)ApplicationIcon.Clone();

    internal static Bitmap CreateBitmap(int size)
    {
        using Icon scaledIcon = new(ApplicationIcon, new Size(size, size));
        using Bitmap source = scaledIcon.ToBitmap();
        Rectangle content = FindVisibleBounds(source);
        Bitmap result = new(size, size);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.Clear(Color.Transparent);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        int padding = Math.Max(1, size / 16);
        graphics.DrawImage(source, new Rectangle(padding, padding, size - padding * 2, size - padding * 2), content, GraphicsUnit.Pixel);
        return result;
    }

    internal static void ApplyTo(PictureBox pictureBox)
    {
        int size = Math.Max(1, Math.Min(pictureBox.ClientSize.Width, pictureBox.ClientSize.Height));
        Image? previous = pictureBox.Image;
        pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
        pictureBox.Image = CreateBitmap(size);
        previous?.Dispose();
    }

    private static MemoryStream LoadApplicationIconStream()
    {
        using Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"Embedded branding resource '{IconResourceName}' was not found.");
        MemoryStream copy = new();
        source.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    private static Rectangle FindVisibleBounds(Bitmap bitmap)
    {
        int left = bitmap.Width;
        int top = bitmap.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
        return right < left ? new Rectangle(0, 0, bitmap.Width, bitmap.Height) : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
