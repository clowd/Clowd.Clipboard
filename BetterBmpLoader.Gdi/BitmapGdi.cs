using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BetterBmpLoader.Gdi
{
#if EXPERIMENTAL_CMM

    public enum CalibrationOptions
    {
        /// <summary>
        /// Any embedded color profile or calibration will be ignored completely.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Recommended: If an embedded color profile or calibration is found, we will try first to convert it to sRGB with lcms2.dll. 
        /// In the event this library can not be found or an error ocurrs, we will attempt to create and return a BitmapFrame with an embedded color profile instead.
        /// If embedding a WPF color profile also fails, we will return a bitmap without any color profile - equivalent to <see cref="CalibrationOptions.Ignore"/>.
        /// </summary>
        TryBestEffort = 1,

        // Not available for GDI
        // PreserveColorProfile = 2,

        /// <summary>
        /// This will attempt to convert any embedded profile or calibration to sRGB with lcms2.dll, and will throw if an error occurs.
        /// </summary>
        FlattenTo_sRGB = 3,
    }
#endif

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
        None = 0,
        ForceV5Header = 1,
        ForceInfoHeader = 2,
        OmitFileHeader = 4,
    }

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
            BitmapCorePixelFormat2 coreFmt = BitmapCorePixelFormat2.Bgra32;

            if (!formatbgra32)
            {
                bool sourceMatched = false;
                var origFmt = info.imgFmt;
                if (origFmt != null)
                {
                    if (origFmt == BitmapCorePixelFormat2.Rgb24)
                    {
                        // we need BitmapCore to reverse the pixel order for GDI
                        coreFmt = BitmapCorePixelFormat2.Bgr24;
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
            var coreFmt = BitmapCorePixelFormat2.Bgra32;

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

            //int stride = (coreFmt.BitsPerPixel * bitmap.Width + 31) / 32 * 4;
            //byte[] buffer = new byte[stride * bitmap.Height];

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
            public BitmapCorePixelFormat2 coreFmt;
            //public ushort bbp;
            //public BITMASKS masks;

            public PxMap(PixelFormat gdi, BitmapCorePixelFormat2 core)
            {
                gdiFmt = gdi;
                coreFmt = core;
                //bpp = bits;
                //masks = mks;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct LOGPALETTE0
        {
            public ushort palVersion;
            public ushort palNumEntries;
        }

        private static PxMap[] Formats = new PxMap[]
        {
            new PxMap(PixelFormat.Format32bppArgb, BitmapCorePixelFormat2.Bgra32),
            new PxMap(PixelFormat.Format24bppRgb, BitmapCorePixelFormat2.Bgr24),
            new PxMap(PixelFormat.Format16bppArgb1555, BitmapCorePixelFormat2.Bgr5551),
            new PxMap(PixelFormat.Format16bppRgb555, BitmapCorePixelFormat2.Bgr555X),
            new PxMap(PixelFormat.Format16bppRgb565, BitmapCorePixelFormat2.Bgr565),
            new PxMap(PixelFormat.Format8bppIndexed, BitmapCorePixelFormat2.Indexed8),
            new PxMap(PixelFormat.Format4bppIndexed, BitmapCorePixelFormat2.Indexed4),
            new PxMap(PixelFormat.Format1bppIndexed, BitmapCorePixelFormat2.Indexed1),

            ////new PxMap(PixelFormat.Undefined, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.DontCare, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Max, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Indexed, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Gdi, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format16bppRgb555, BitmapCorePixelFormat2.Bgr555X, 16, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format16bppRgb565, BitmapCorePixelFormat2.Bgr565, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format24bppRgb, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format32bppRgb, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format1bppIndexed, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format4bppIndexed, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format8bppIndexed, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Alpha, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format16bppArgb1555, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.PAlpha, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format32bppPArgb, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Extended, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Format16bppGrayScale, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Format48bppRgb, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Format64bppPArgb, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Canonical, null, 0, new BITMASKS(0,0,0,0)),
            //new PxMap(PixelFormat.Format32bppArgb, null, 0, new BITMASKS(0,0,0,0)),
            ////new PxMap(PixelFormat.Format64bppArgb, null, 0, new BITMASKS(0,0,0,0)),
        };
    }
}
