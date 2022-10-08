using Clowd.Clipboard.Formats;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clowd.Clipboard;

/// <summary>
/// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandleGdi"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class ClipboardGdi : ClipboardStaticBase<ClipboardHandleGdi, Bitmap>
{
    private ClipboardGdi() { }
}

/// <inheritdoc />
[SupportedOSPlatform("windows")]
public abstract class ClipboardHandleGdiBase : ClipboardHandlePlatformBase<Bitmap>
{
    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetDibConverter() => new ImageGdiDib();

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetDibV5Converter() => new ImageGdiDibV5();

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetGdiHandleConverter() => new ImageGdiHandle();

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetGifConverter() => new ImageGdiBitmap(ImageFormat.Gif);

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetJpegConverter() => new ImageGdiBitmap(ImageFormat.Jpeg);

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetPngConverter() => new ImageGdiBitmap(ImageFormat.Png);

    /// <inheritdoc/>
    protected override IDataConverter<Bitmap> GetTiffConverter() => new ImageGdiBitmap(ImageFormat.Tiff);

    /// <inheritdoc/>
    protected override Bitmap LoadFromFile(string filePath) => Image.FromFile(filePath) as Bitmap;
}

/// <inheritdoc />
[SupportedOSPlatform("windows")]
public class ClipboardHandleGdi : ClipboardHandleGdiBase, IClipboardHandlePlatform<Bitmap>
{
    /// <inheritdoc/>
    public virtual Bitmap GetImage() => GetImageImpl();

    /// <inheritdoc/>
    public virtual void SetImage(Bitmap bitmap) => SetImageImpl(bitmap);
}
