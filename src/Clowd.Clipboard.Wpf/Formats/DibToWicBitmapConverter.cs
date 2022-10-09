using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Clowd.Clipboard.Bitmaps;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Converts a CF_DIB to/from a WPF BitmapSource.
/// </summary>
public unsafe class DibToWicBitmapConverter : BytesDataConverterBase<BitmapSource>
{
    /// <inheritdoc/>
    public override BitmapSource ReadFromBytes(byte[] data)
    {
        fixed (byte* dataptr = data)
        {
            uint bcrFlags = BitmapCore.BC_READ_PRESERVE_INVALID_ALPHA;
            BitmapCore.ReadHeader(dataptr, data.Length, out var info, bcrFlags);
            return BitmapWpfInternal.Read(ref info, (dataptr + info.imgDataOffset), bcrFlags);
        }
    }

    /// <inheritdoc/>
    public override byte[] WriteToBytes(BitmapSource bmp)
    {
        // use WIC because the integrated method is less flexible.
        // for example, it can't output pre-multiplied alpha which seems to produce better results.
        //return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_VINFO);

        FormatConvertedBitmap formatted = new FormatConvertedBitmap(bmp, PixelFormats.Pbgra32, null, 0);
        int imgHeight = formatted.PixelHeight;
        int imgWidth = formatted.PixelWidth;
        int imgStride = imgWidth * 4;
        int imgSize = imgStride * imgHeight;
        var imgBytes = new byte[imgSize];
        formatted.CopyPixels(imgBytes, imgStride, 0);

        BITMAPINFOHEADER info = new BITMAPINFOHEADER()
        {
            bV5Size = 40,
            bV5BitCount = 32,
            bV5Compression = BitmapCompressionMode.BI_RGB,
            bV5Height = imgHeight,
            bV5Width = imgWidth,
            bV5Planes = 1,
            bV5SizeImage = (uint)imgSize,
        };

        var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
        byte[] buf = new byte[headerSize + imgSize];
        uint offset = 0;
        StructUtil.SerializeTo(info, buf, ref offset);

        // the bitmap is upside down, so we need to reverse it.
        for (int y = 0; y < imgHeight; y++)
        {
            Buffer.BlockCopy(imgBytes, (imgHeight - y - 1) * imgStride, buf, headerSize + (y * imgStride), imgStride);
        }

        return buf;
    }
}

/// <summary>
/// Converts a CF_DIBV5 to/from a WPF BitmapSource.
/// </summary>
public unsafe class DibV5ToWicBitmapConverter : DibToWicBitmapConverter
{
    /// <inheritdoc/>
    public override byte[] WriteToBytes(BitmapSource obj)
    {
        return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_V5);
    }
}
