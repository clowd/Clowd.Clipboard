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
    public enum BitmapGdiParserFlags
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

    public class BitmapGdi
    {
        public static Bitmap Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static Bitmap Read(Stream stream, BitmapGdiParserFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static Bitmap Read(byte[] data) => Read(data, BitmapGdiParserFlags.None);

        public unsafe static Bitmap Read(byte[] data, BitmapGdiParserFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, pFlags);
        }

        public unsafe static Bitmap Read(byte* data, int dataLength, BitmapGdiParserFlags pFlags)
        {
            var preserveAlpha = (pFlags & BitmapGdiParserFlags.PreserveInvalidAlphaChannel) > 0;
            var strictFormat = (pFlags & BitmapGdiParserFlags.StrictPreserveOriginalFormat) > 0;
            var formatbgra32 = (pFlags & BitmapGdiParserFlags.ConvertToBGRA32) > 0;

            if (strictFormat && formatbgra32)
                throw new ArgumentException("Both ConvertToBGRA32 and StrictPreserveOriginalFormat options were set. These are incompatible options.");

            BITMAP_READ_DETAILS info;
            BitmapCore.ReadHeader(data, dataLength, out info);

            // we do this parsing here since BitmapCore has no references to System.Drawing
            var size = info.imgDataSize != 0 ? info.imgDataSize : (uint)dataLength;
            if (info.compression == BitmapCompressionMode.BI_PNG || info.compression == BitmapCompressionMode.BI_JPEG)
                return new Bitmap(new PointerStream(data, size));

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
            var dlock = bitmap.LockBits(new Rectangle(0, 0, info.imgWidth, info.imgHeight), System.Drawing.Imaging.ImageLockMode.ReadWrite, gdiFmt);

            var buf = (byte*)dlock.Scan0;

            BitmapCore.ReadPixels(ref info, coreFmt, data + info.imgDataOffset, buf, preserveAlpha);

            bitmap.UnlockBits(dlock);
            return bitmap;
        }

        private struct PxMap
        {
            public PixelFormat gdiFmt;
            public BitmapCorePixelFormat2 coreFmt;

            public PxMap(PixelFormat gdi, BitmapCorePixelFormat2 core)
            {
                gdiFmt = gdi;
                coreFmt = core;
            }
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
        };
    }
}
