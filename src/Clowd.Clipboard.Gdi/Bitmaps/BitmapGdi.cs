using System.Drawing;
using System.Drawing.Imaging;

namespace Clowd.Clipboard.Bitmaps;

/// <summary>
/// Provides a GDI+ implementation of Bitmap reader and writer. This bitmap library can read almost any kind of bitmap and 
/// tries to do a better job than Gdi+ does in terms of coverage and it also tries to handle some nuances of how other native 
/// applications write bitmaps especially when reading from or writing to the clipboard.
/// </summary>
[SupportedOSPlatform("windows")]
public unsafe class BitmapGdi : BitmapConverterStaticBase<BitmapGdi, Bitmap>
{
    /// <inheritdoc/>
    public override byte[] GetBytes(Bitmap bitmap, BitmapWriterFlags wFlags)
    {
        // default - this will cause GDI to convert the pixel format to bgra32 if we don't know the format directly
        var gdiFmt = PixelFormat.Format32bppArgb;
        var coreFmt = BitmapCorePixelFormat.Bgra32;

        var pxarr = Formats.Where(f => f.gdiFmt == bitmap.PixelFormat).ToArray();
        if (pxarr.Length > 0)
        {
            var px = pxarr.First();
            gdiFmt = px.gdiFmt;
            coreFmt = px.coreFmt;
        }

        var colorTable = bitmap.Palette.Entries.Select(e => new RGBQUAD { rgbBlue = e.B, rgbGreen = e.G, rgbRed = e.R }).ToArray();

        var dlock = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, gdiFmt);
        var buf = (byte*)dlock.Scan0;

        BITMAP_WRITE_REQUEST req = new BITMAP_WRITE_REQUEST
        {
            dpiX = 0,
            dpiY = 0,
            imgWidth = bitmap.Width,
            imgHeight = bitmap.Height,
            imgStride = (uint)dlock.Stride,
            imgTopDown = true,
            imgColorTable = colorTable,
        };

        var bytes = BitmapCore.WriteToBMP(ref req, buf, coreFmt, (uint)wFlags);
        bitmap.UnlockBits(dlock);
        return bytes;
    }

    /// <inheritdoc/>
    public override unsafe Bitmap Read(byte* data, int dataLength, BitmapReaderFlags rFlags)
    {
        uint bcrFlags = (uint)rFlags;

        BITMAP_READ_DETAILS info;
        BitmapCore.ReadHeader(data, dataLength, out info, bcrFlags);

        byte* pixels = data + info.imgDataOffset;

        // we do this parsing here since BitmapCore has no references to System.Drawing
        if (info.compression == BitmapCompressionMode.BI_PNG || info.compression == BitmapCompressionMode.BI_JPEG)
            return new Bitmap(new PointerStream(pixels, info.imgDataSize));

        // defaults
        PixelFormat gdiFmt = PixelFormat.Format32bppArgb;
        BitmapCorePixelFormat coreFmt = BitmapCorePixelFormat.Bgra32;

        var formatbgra32 = (rFlags & BitmapReaderFlags.ForceFormatBGRA32) > 0;
        if (!formatbgra32 && info.imgSourceFmt != null)
        {
            var origFmt = info.imgSourceFmt;
            if (origFmt == BitmapCorePixelFormat.Rgb24)
            {
                // we need BitmapCore to reverse the pixel order for GDI
                coreFmt = BitmapCorePixelFormat.Bgr24;
                gdiFmt = PixelFormat.Format24bppRgb;
            }
            else
            {
                var pxarr = Formats.Where(f => f.coreFmt == origFmt).ToArray();
                if (pxarr.Length > 0)
                {
                    var px = pxarr.First();
                    gdiFmt = px.gdiFmt;
                    coreFmt = px.coreFmt;
                }
            }
        }

        Bitmap bitmap = new Bitmap(info.imgWidth, info.imgHeight, gdiFmt);
        var dlock = bitmap.LockBits(new Rectangle(0, 0, info.imgWidth, info.imgHeight), ImageLockMode.ReadWrite, gdiFmt);
        var buf = (byte*)dlock.Scan0;
        BitmapCore.ReadPixels(ref info, coreFmt, pixels, buf, bcrFlags);
        bitmap.UnlockBits(dlock);

        // update bitmap color palette
        var gdipPalette = bitmap.Palette;
        if (info.imgColorTable != null && gdipPalette?.Entries != null && gdipPalette.Entries.Length > 0)
        {
            for (int i = 0; i < info.imgColorTable.Length && i < gdipPalette.Entries.Length; i++)
            {
                var quad = info.imgColorTable[i];
                gdipPalette.Entries[i] = Color.FromArgb(0xFF, quad.rgbRed, quad.rgbGreen, quad.rgbBlue);
            }
            bitmap.Palette = gdipPalette;
        }

        return bitmap;
    }

    private struct PxMap
    {
        public PixelFormat gdiFmt;
        public BitmapCorePixelFormat coreFmt;

        public PxMap(PixelFormat gdi, BitmapCorePixelFormat core)
        {
            gdiFmt = gdi;
            coreFmt = core;
        }
    }

    private static PxMap[] Formats = new PxMap[]
    {
        new PxMap(PixelFormat.Format32bppArgb, BitmapCorePixelFormat.Bgra32),
        new PxMap(PixelFormat.Format24bppRgb, BitmapCorePixelFormat.Bgr24),
        new PxMap(PixelFormat.Format16bppArgb1555, BitmapCorePixelFormat.Bgr5551),
        new PxMap(PixelFormat.Format16bppRgb555, BitmapCorePixelFormat.Bgr555X),
        new PxMap(PixelFormat.Format16bppRgb565, BitmapCorePixelFormat.Bgr565),
        new PxMap(PixelFormat.Format8bppIndexed, BitmapCorePixelFormat.Indexed8),
        new PxMap(PixelFormat.Format4bppIndexed, BitmapCorePixelFormat.Indexed4),
        new PxMap(PixelFormat.Format1bppIndexed, BitmapCorePixelFormat.Indexed1),
    };
}
