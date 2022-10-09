using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Clowd.Clipboard.Bitmaps;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Converts a CF_DIB to/from a WPF BitmapSource.
/// </summary>
[SupportedOSPlatform("windows")]
public unsafe class DibToGdiBitmapConverter : BytesDataConverterBase<Bitmap>
{
    /// <inheritdoc/>
    public override Bitmap ReadFromBytes(byte[] data)
    {
        return BitmapGdi.FromBytes(data, BitmapReaderFlags.PreserveInvalidAlphaChannel);
    }

    /// <inheritdoc/>
    public override byte[] WriteToBytes(Bitmap bmp)
    {
        // use GDI because the integrated method is less flexible.
        // for example, it can't output pre-multiplied alpha which seems to produce better results.
        // return BitmapGdi.ToBytes(obj, BitmapWriterFlags.ForceInfoHeader | BitmapWriterFlags.SkipFileHeader);

        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

        try
        {
            int imgSize = data.Stride * data.Height;
            byte[] imgBytes = new byte[imgSize];
            Marshal.Copy(data.Scan0, imgBytes, 0, imgSize);

            BITMAPINFOHEADER info = new BITMAPINFOHEADER()
            {
                bV5Size = 40,
                bV5BitCount = 32,
                bV5Compression = BitmapCompressionMode.BI_RGB,
                bV5Height = data.Height,
                bV5Width = data.Width,
                bV5Planes = 1,
                bV5SizeImage = (uint)imgSize,
            };

            var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            byte[] buf = new byte[headerSize + imgSize];
            uint offset = 0;
            StructUtil.SerializeTo(info, buf, ref offset);

            // the bitmap is upside down, so we need to reverse it.
            for (int y = 0; y < data.Height; y++)
            {
                Buffer.BlockCopy(imgBytes, (data.Height - y - 1) * data.Stride, buf, headerSize + (y * data.Stride), data.Stride);
            }

            return buf;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}

/// <summary>
/// Converts a CF_DIBV5 to/from a WPF BitmapSource.
/// </summary>
[SupportedOSPlatform("windows")]
public unsafe class DibV5ToGdiBitmapConverter : DibToGdiBitmapConverter
{
    /// <inheritdoc/>
    public override byte[] WriteToBytes(Bitmap obj)
    {
        return BitmapGdi.ToBytes(obj, BitmapWriterFlags.ForceV5Header | BitmapWriterFlags.SkipFileHeader);
    }
}