using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace BetterBmpLoader.Gdi
{
    [Flags]
    public enum BitmapGdiReaderFlags
    {
        /// <summary>
        /// No special parsing flags
        /// </summary>
        None = 0,

        /// <summary>
        /// In windows, many applications create 16 or 32 bpp Bitmaps that indicate they have no transparency, but actually use the unused channel for transparency data.
        /// For example, in a 32bpp RGB encoded bitmap, you'd have the following R8, G8, B8, and the remaining bits are to be ignored. With this flag set, we will use 
        /// heuristics to determine if that unused channel contains transparency data, and if so, parse it as such.
        /// </summary>
        PreserveInvalidAlphaChannel = 1,

        /// <summary>
        /// Will cause an exeption if the original pixel format can not be preserved. This could be the case if either System.Drawing or BitmapCore does not 
        /// support this format natively.
        /// </summary>
        StrictPreserveOriginalFormat = 2,

        /// <summary>
        /// Will force the bitmap pixel data to be converted to BGRA32 no matter what the source format is. Not valid if combined with <see cref="StrictPreserveOriginalFormat"/>.
        /// </summary>
        ConvertToBGRA32 = 4
    }

    [Flags]
    public enum BitmapGdiWriterFlags
    {
        /// <summary>
        /// No special writer flags
        /// </summary>
        None = 0,

        /// <summary>
        /// This specifies that the bitmap must be created with a BITMAPV5HEADER. This is desirable if storing the image to the cliboard at CF_DIBV5 for example.
        /// </summary>
        ForceV5Header = 1,

        /// <summary>
        /// This specifies that the bitmap must be created with a BITMAPINFOHEADER. This is required when storing the image to the clipboard at CF_DIB, or possibly
        /// for interoping with other applications that do not support newer bitmap files. This option is not advisable unless absolutely required - as not all bitmaps
        /// can be accurately represented. For example, no transparency data can be stored - and the images will appear fully opaque.
        /// </summary>
        ForceInfoHeader = 2,

        /// <summary>
        /// This option requests that the bitmap be created without a BITMAPFILEHEADER (ie, in Packed DIB format). This is used when storing the file to the clipboard.
        /// </summary>
        OmitFileHeader = 4,
    }

    /// <summary>
    /// Provides a Gdi+ implementation of BetterBmpLoaded Bitmap reader and writer. This bitmap library can read almost any kind of bitmap and 
    /// tries to do a better job than Gdi+ does in terms of coverage and it also tries to handle some nuances of how other native applications write bitmaps especially when
    /// reading from or writing to the clipboard.
    /// </summary>
    public class BitmapGdi
    {
        public static Bitmap Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static Bitmap Read(Stream stream, BitmapGdiReaderFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static Bitmap Read(byte[] data) => Read(data, BitmapGdiReaderFlags.None);

        public unsafe static Bitmap Read(byte[] data, BitmapGdiReaderFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, pFlags);
        }

        public unsafe static Bitmap Read(byte* data, int dataLength, BitmapGdiReaderFlags pFlags)
        {
            var preserveAlpha = (pFlags & BitmapGdiReaderFlags.PreserveInvalidAlphaChannel) > 0;
            var strictFormat = (pFlags & BitmapGdiReaderFlags.StrictPreserveOriginalFormat) > 0;
            var formatbgra32 = (pFlags & BitmapGdiReaderFlags.ConvertToBGRA32) > 0;

            if (strictFormat && formatbgra32)
                throw new ArgumentException("Both ConvertToBGRA32 and StrictPreserveOriginalFormat options were set. These are incompatible options.");

            BITMAP_READ_DETAILS info;
            BitmapCore.ReadHeader(data, dataLength, out info);

            byte* pixels = data + info.imgDataOffset;

            // we do this parsing here since BitmapCore has no references to System.Drawing
            if (info.compression == BitmapCompressionMode.BI_PNG || info.compression == BitmapCompressionMode.BI_JPEG)
                return new Bitmap(new PointerStream(pixels, info.imgDataSize));

            // defaults
            PixelFormat gdiFmt = PixelFormat.Format32bppArgb;
            BitmapCorePixelFormat coreFmt = BitmapCorePixelFormat.Bgra32;

            if (!formatbgra32 && info.imgSourceFmt != null)
            {
                bool sourceMatched = false;
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
                        sourceMatched = true;
                    }
                }

                if (!sourceMatched && strictFormat)
                    throw new NotSupportedException("The pixel format of this bitmap is not supported, and StrictPreserveOriginalFormat is set.");
            }

            Bitmap bitmap = new Bitmap(info.imgWidth, info.imgHeight, gdiFmt);
            var dlock = bitmap.LockBits(new Rectangle(0, 0, info.imgWidth, info.imgHeight), ImageLockMode.ReadWrite, gdiFmt);
            var buf = (byte*)dlock.Scan0;
            BitmapCore.ReadPixels(ref info, coreFmt, pixels, buf, preserveAlpha);
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

        public static byte[] GetBytes(Bitmap bitmap) => GetBytes(bitmap, BitmapGdiWriterFlags.None);

        public static unsafe byte[] GetBytes(Bitmap bitmap, BitmapGdiWriterFlags wFlags)
        {
            var forceV5 = (wFlags & BitmapGdiWriterFlags.ForceV5Header) > 0;
            var forceInfo = (wFlags & BitmapGdiWriterFlags.ForceInfoHeader) > 0;
            var omitFileHeader = (wFlags & BitmapGdiWriterFlags.OmitFileHeader) > 0;

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

            var htype = BitmapCoreHeaderType.BestFit;
            if (forceV5) htype = BitmapCoreHeaderType.ForceV5;
            else if (forceInfo) htype = BitmapCoreHeaderType.ForceVINFO;

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
                headerIncludeFile = !omitFileHeader,
                iccEmbed = false,
                iccProfileData = null,
                headerType = htype,
            };

            var bytes = BitmapCore.WriteToBMP(ref req, buf, coreFmt);
            bitmap.UnlockBits(dlock);
            return bytes;
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
}
