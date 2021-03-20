using System;
using System.Drawing;
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
    public enum ParserFlags
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
    }

    public class BitmapGdi
    {
        public static Bitmap Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static Bitmap Read(Stream stream, ParserFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static Bitmap Read(byte[] data) => Read(data, ParserFlags.None);

        public unsafe static Bitmap Read(byte[] data, ParserFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, pFlags);
        }

        public unsafe static Bitmap Read(byte* data, int dataLength, ParserFlags pFlags)
        {
            BITMAP_READ_DETAILS info;
            BitmapCore.ReadHeader(data, dataLength, out info);

            // we do this parsing here since BitmapCore has no references to System.Drawing
            var size = info.imgDataSize != 0 ? info.imgDataSize : (uint)dataLength;
            if (info.compression == BitmapCompressionMode.BI_PNG || info.compression == BitmapCompressionMode.BI_JPEG)
                return new Bitmap(new PointerStream(data, size));


            var fmt = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

            Bitmap bitmap = new Bitmap(info.imgWidth, info.imgHeight, fmt);
            var dlock = bitmap.LockBits(new Rectangle(0, 0, info.imgWidth, info.imgHeight), System.Drawing.Imaging.ImageLockMode.ReadWrite, fmt);

            var buf = (byte*)dlock.Scan0;
            var preserveAlpha = (pFlags & ParserFlags.PreserveInvalidAlphaChannel) > 0;

            BitmapCore.ReadPixels(ref info, )

            BitmapCore.ReadPixelsToBGRA8(ref info, (data + info.imgDataOffset), buf, preserveAlpha);

            bitmap.UnlockBits(dlock);
            return bitmap;
        }
    }
}
