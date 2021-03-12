using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace BetterBmpLoader.Gdi
{
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
        public static Bitmap Read(Stream stream) => Read(stream, CalibrationOptions.Ignore);
        public static Bitmap Read(Stream stream, CalibrationOptions colorOptions) => Read(stream, colorOptions, ParserFlags.None);
        public static Bitmap Read(Stream stream, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            if (stream is MemoryStream mem)
            {
                return Read(mem.GetBuffer(), colorOptions, pFlags);
            }
            else
            {
                byte[] buffer = new byte[4096];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return Read(ms.GetBuffer(), colorOptions, pFlags);
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }
        public static Bitmap Read(byte[] data) => Read(data, CalibrationOptions.Ignore);
        public static Bitmap Read(byte[] data, CalibrationOptions colorOptions) => Read(data, colorOptions, ParserFlags.None);
        public unsafe static Bitmap Read(byte[] data, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, colorOptions, pFlags);
        }
        public unsafe static Bitmap Read(byte* data, int dataLength, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            BITMAP_DETAILS info;
            BitmapCore.GetInfo(data, dataLength, out info);

            // we do this parsing here since BitmapCore has no references to System.Drawing
            if (info.compression == BitmapCompressionMode.BI_PNG)
            {
                byte[] pngImg = new byte[info.imgDataSize];
                Marshal.Copy((IntPtr)(data + info.imgDataOffset), pngImg, 0, pngImg.Length);
                return new Bitmap(new MemoryStream(pngImg));
            }
            else if (info.compression == BitmapCompressionMode.BI_JPEG)
            {
                byte[] jpegImg = new byte[info.imgDataSize];
                Marshal.Copy((IntPtr)(data + info.imgDataOffset), jpegImg, 0, jpegImg.Length);
                return new Bitmap(new MemoryStream(jpegImg));
            }

            var fmt = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

            Bitmap bitmap = new Bitmap(info.imgWidth, info.imgHeight, fmt);
            var dlock = bitmap.LockBits(new Rectangle(0, 0, info.imgWidth, info.imgHeight), System.Drawing.Imaging.ImageLockMode.ReadWrite, fmt);

            var buf = (byte*)dlock.Scan0;
            var preserveAlpha = (pFlags & ParserFlags.PreserveInvalidAlphaChannel) > 0;
            BitmapCore.ReadPixelsToBGRA8(ref info, (data + info.imgDataOffset), buf, preserveAlpha);

            try
            {
                lcms.TransformBGRA8(ref info, data, buf, dlock.Stride);
            }
            catch
            {
                if (colorOptions != CalibrationOptions.TryBestEffort)
                    throw;
            }

            bitmap.UnlockBits(dlock);
            return bitmap;
        }
    }
}
