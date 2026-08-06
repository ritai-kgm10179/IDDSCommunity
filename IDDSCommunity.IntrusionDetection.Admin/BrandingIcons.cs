using System.Drawing;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// Provides every window and branded surface with images from the executable's canonical multi-size icon.
/// </summary>
internal static class BrandingIcons
{
    private static readonly Icon ApplicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

    internal static Icon CreateIcon() => (Icon)ApplicationIcon.Clone();

    internal static Bitmap CreateBitmap(int size)
    {
        using Icon scaledIcon = new(ApplicationIcon, new Size(size, size));
        return scaledIcon.ToBitmap();
    }
}
