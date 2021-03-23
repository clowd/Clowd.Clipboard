using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader.Wpf
{
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
    /// Provides a WPF implementation of BetterBmpLoaded Bitmap reader and writer. This bitmap library can read almost any kind of bitmap and 
    /// tries to do a better job than WPF does in terms of coverage and it also tries to handle some nuances of how other native applications write bitmaps, 
    /// especially when reading from or writing to the clipboard.
    /// </summary>
    public sealed class BitmapWpf
    {
        public static BitmapFrame Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static BitmapFrame Read(Stream stream, BitmapWpfReaderFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static BitmapFrame Read(byte[] data) => Read(data, BitmapWpfReaderFlags.None);

        public unsafe static BitmapFrame Read(byte[] data, BitmapWpfReaderFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, pFlags);
        }

        public unsafe static BitmapFrame Read(byte* data, int dataLength, BitmapWpfReaderFlags pFlags)
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
