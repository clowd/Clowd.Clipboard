using System.Drawing;
using Clowd.Clipboard.Bitmaps;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Converts a CF_DIB to/from a WPF BitmapSource.
/// </summary>
public unsafe class ImageGdiDib : BytesDataConverterBase<Bitmap>
{
    /// <inheritdoc/>
    public override Bitmap ReadFromBytes(byte[] data)
    {
        return BitmapGdi.FromBytes(data, BitmapReaderFlags.PreserveInvalidAlphaChannel);
    }

    /// <inheritdoc/>
    public override byte[] WriteToBytes(Bitmap obj)
    {
        return BitmapGdi.ToBytes(obj, BitmapWriterFlags.ForceInfoHeader | BitmapWriterFlags.SkipFileHeader);
    }
}

/// <summary>
/// Converts a CF_DIBV5 to/from a WPF BitmapSource.
/// </summary>
public unsafe class ImageGdiDibV5 : ImageGdiDib
{
    /// <inheritdoc/>
    public override byte[] WriteToBytes(Bitmap obj)
    {
        return BitmapGdi.ToBytes(obj, BitmapWriterFlags.ForceV5Header | BitmapWriterFlags.SkipFileHeader);
    }
}
