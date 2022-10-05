using System.Windows.Media.Imaging;

namespace Clowd.Clipboard.Bitmaps;

/// <summary>
/// Provides a WPF implementation of Bitmap reader and writer. This bitmap library can read almost any kind of bitmap and 
/// tries to do a better job than WPF does in terms of coverage and it also tries to handle some nuances of how other native applications write bitmaps, 
/// especially when reading from or writing to the clipboard.
/// </summary>
public sealed class BitmapWpf : BitmapConverterStaticBase<BitmapWpf, BitmapSource>
{
    /// <inheritdoc/>
    public unsafe override BitmapSource Read(byte* data, int dataLength, BitmapReaderFlags rFlags)
    {
        BITMAP_READ_DETAILS info;
        uint bcrFlags = (uint)rFlags;
        BitmapCore.ReadHeader(data, dataLength, out info, bcrFlags);
        return BitmapWpfInternal.Read(ref info, data + info.imgDataOffset, bcrFlags);
    }

    /// <inheritdoc/>
    public override byte[] GetBytes(BitmapSource bitmap, BitmapWriterFlags wFlags)
    {
        return BitmapWpfInternal.GetBytes(bitmap, (uint)wFlags);
    }
}
