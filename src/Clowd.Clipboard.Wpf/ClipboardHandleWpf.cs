using Clowd.Clipboard.Formats;
using System.Windows.Media.Imaging;

namespace Clowd.Clipboard;

/// <summary>
/// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandleWpf"/>.
/// </summary>
public class ClipboardWpf : ClipboardStaticBase<ClipboardHandleWpf, BitmapSource>
{
    private ClipboardWpf() { }
}

/// <inheritdoc/>
public class ClipboardHandleWpf : ClipboardHandlePlatformBase<BitmapSource>, IClipboardHandlePlatform<BitmapSource>
{
    /// <inheritdoc/>
    public void SetImage(BitmapSource bitmap) => SetImageImpl(bitmap);

    /// <inheritdoc/>
    public BitmapSource GetImage() => GetImageImpl();

    /// <inheritdoc/>
    protected override BitmapSource LoadFromFile(string filePath)
    {
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.UriSource = new Uri(filePath);
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.EndInit();
        return bi;
    }

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetJpegConverter() => new ImageWpfBasicEncoderJpeg();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetTiffConverter() => new ImageWpfBasicEncoderTiff();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetGifConverter() => new ImageWpfBasicEncoderGif();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetPngConverter() => new ImageWpfBasicEncoderPng();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetGdiHandleConverter() => new ImageBitmap();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetDibConverter() => new ImageWpfDib();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetDibV5Converter() => new ImageWpfDibV5();
}
