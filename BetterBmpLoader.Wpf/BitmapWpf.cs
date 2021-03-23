using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader.Wpf
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

        /// <summary>
        /// Not recommended: Attempts to parse an embedded color profile to a WPF color context. This does not support calibrated bitmaps (calibration will be ignored).
        /// Also, WPF ignores color profiles when rendering a BitmapSource, so this should only be used if the intention is to immediately encode the returned BitmapFrame
        /// into another image type, using an encoder such as <see cref="PngBitmapEncoder"/>.
        /// </summary>
        PreserveColorProfile = 2,

        /// <summary>
        /// This will attempt to convert any embedded profile or calibration to sRGB with lcms2.dll, and will throw if an error occurs.
        /// </summary>
        FlattenTo_sRGB = 3,
    }
#endif

    [Flags]
    public enum BitmapWpfReaderFlags
    {
        /// <summary>
        /// No special parsing flags
        /// </summary>
        None = 0,

        /// <summary>
        /// In windows, many applications create 16 or 32 bpp Bitmaps that indicate they have no transparency, but actually do have transparency.
        /// For example, in a 32bpp RGB encoded bitmap, you'd have the following R8, G8, B8, and the remaining 8 bits are to be ignored / zero.
        /// Sometimes, these bits are not zero, and with this flag set, we will use heuristics to determine if that unused channel contains 
        /// transparency data, and if so, parse it as such.
        /// </summary>
        PreserveInvalidAlphaChannel = 1,

        /// <summary>
        /// Will cause an exeption if the original pixel format can not be preserved. This could be the case if either WPF or BitmapCore does not 
        /// support this format natively.
        /// </summary>
        StrictPreserveOriginalFormat = 2,

        /// <summary>
        /// Will force the bitmap pixel data to be converted to BGRA32 no matter what the source format is. Not valid if combined with <see cref="StrictPreserveOriginalFormat"/>.
        /// </summary>
        ConvertToBGRA32 = 4
    }

    [Flags]
    public enum BitmapWpfWriterFlags
    {
        None = 0,
        ForceV5Header = 1,
        ForceInfoHeader = 2,
        OmitFileHeader = 4,
    }

    /// <summary>
    /// Provides a WPF implementation of BetterBmpLoaded Bitmap parser and writer. This bitmap library can read almost any kind of bitmap and 
    /// tries to do a better job than WPF does in terms of coverage and it also tries to handle some nuances of how other native applications write bitmaps.
    /// </summary>
    public sealed class BitmapWpf
    {

#if EXPERIMENTAL_CMM
        public static BitmapFrame Read(Stream stream) => Read(stream, CalibrationOptions.Ignore);

        public static BitmapFrame Read(Stream stream, CalibrationOptions colorOptions) => Read(stream, colorOptions, BitmapWpfParserFlags.None);

        public static BitmapFrame Read(Stream stream, CalibrationOptions colorOptions, BitmapWpfParserFlags pFlags) => Read(StructUtil.ReadBytes(stream), colorOptions, pFlags);

        public static BitmapFrame Read(byte[] data) => Read(data, CalibrationOptions.Ignore);

        public static BitmapFrame Read(byte[] data, CalibrationOptions colorOptions) => Read(data, colorOptions, BitmapWpfParserFlags.None);

        public unsafe static BitmapFrame Read(byte[] data, CalibrationOptions colorOptions, BitmapWpfParserFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, colorOptions, pFlags);
        }

        public unsafe static BitmapFrame Read(byte* data, int dataLength, CalibrationOptions colorOptions, BitmapWpfParserFlags pFlags)
#else
        public static BitmapFrame Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static BitmapFrame Read(Stream stream, BitmapWpfReaderFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static BitmapFrame Read(byte[] data) => Read(data, BitmapWpfReaderFlags.None);

        public unsafe static BitmapFrame Read(byte[] data, BitmapWpfReaderFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, pFlags);
        }

        public unsafe static BitmapFrame Read(byte* data, int dataLength, BitmapWpfReaderFlags pFlags)
#endif
        {
            BITMAP_READ_DETAILS info;
            BitmapCore.ReadHeader(data, dataLength, out info);
            var preserveAlpha = (pFlags & BitmapWpfReaderFlags.PreserveInvalidAlphaChannel) > 0;
            var preserveFormat = (pFlags & BitmapWpfReaderFlags.StrictPreserveOriginalFormat) > 0;
            var bgra32 = (pFlags & BitmapWpfReaderFlags.ConvertToBGRA32) > 0;

            if (preserveFormat && bgra32)
                throw new ArgumentException("Both ConvertToBGRA32 and StrictPreserveOriginalFormat options were set. These are incompatible options.");

            return BitmapWpfInternal.Read(ref info, data, dataLength, preserveAlpha, preserveFormat, bgra32);
        }

        public static byte[] GetBytes(BitmapFrame bitmap) => GetBytes(bitmap, BitmapWpfWriterFlags.None);

        public static byte[] GetBytes(BitmapFrame bitmap, BitmapWpfWriterFlags wFlags)
        {
            var forceV5 = (wFlags & BitmapWpfWriterFlags.ForceV5Header) > 0;
            var forceInfo = (wFlags & BitmapWpfWriterFlags.ForceInfoHeader) > 0;
            var omitFileHeader = (wFlags & BitmapWpfWriterFlags.OmitFileHeader) > 0;
            return BitmapWpfInternal.GetBytes(bitmap, !omitFileHeader, forceV5, forceInfo);
        }
    }
}
