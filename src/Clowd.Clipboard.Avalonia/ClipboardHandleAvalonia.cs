using System.Drawing;
using System.Drawing.Imaging;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Clowd.Clipboard;

/// <summary>
/// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandleAvalonia"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class ClipboardAvalonia : ClipboardStaticBase<ClipboardHandleAvalonia, AvaBitmap>
{
    private ClipboardAvalonia() { }
}

/// <inheritdoc/>
[SupportedOSPlatform("windows")]
public class ClipboardHandleAvalonia : ClipboardHandleGdiBase, IClipboardHandlePlatform<AvaBitmap>
{
    /// <inheritdoc/>
    public virtual AvaBitmap GetImage()
    {
        using var gdi = GetImageImpl();

        if (gdi == null)
            return null;

        var bitmapData = gdi.LockBits(
            new Rectangle(0, 0, gdi.Width, gdi.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

        var bmp = new AvaBitmap(
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul,
            bitmapData.Scan0,
            new Avalonia.PixelSize(bitmapData.Width, bitmapData.Height),
            new Avalonia.Vector(gdi.HorizontalResolution, gdi.VerticalResolution),
            bitmapData.Stride);

        gdi.UnlockBits(bitmapData);

        return bmp;
    }

    /// <inheritdoc/>
    public virtual void SetImage(AvaBitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms);

        using var gdi = new Bitmap(ms);

        SetImageImpl(gdi);
    }
}
