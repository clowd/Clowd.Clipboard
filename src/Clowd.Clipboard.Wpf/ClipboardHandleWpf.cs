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
    protected override IDataConverter<BitmapSource> GetJpegConverter() => new BytesToWicBitmapConverter(BytesToWicBitmapConverter.Format_Jpeg);

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetTiffConverter() => new BytesToWicBitmapConverter(BytesToWicBitmapConverter.Format_Tiff);

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetGifConverter() => new BytesToWicBitmapConverter(BytesToWicBitmapConverter.Format_Gif);

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetPngConverter() => new BytesToWicBitmapConverter(BytesToWicBitmapConverter.Format_Png);

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetGdiHandleConverter() => new GdiHandleToWicBitmapConverter();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetDibConverter() => new DibToWicBitmapConverter();

    /// <inheritdoc/>
    protected override IDataConverter<BitmapSource> GetDibV5Converter() => new DibV5ToWicBitmapConverter();
}
