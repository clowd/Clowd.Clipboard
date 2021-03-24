using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Clowd.BmpLib.Wpf
{
    [Flags]
    public enum BitmapWpfReaderFlags : uint
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
        PreserveInvalidAlphaChannel = BitmapCore.BC_READ_PRESERVE_INVALID_ALPHA,

        /// <summary>
        /// Will cause an exeption if the original pixel format can not be preserved. This could be the case if BitmapCore or the target framework 
        /// does not support this format natively.
        /// </summary>
        StrictPreserveOriginalFormat = BitmapCore.BC_READ_STRICT_PRESERVE_FORMAT,

        /// <summary>
        /// Will force the bitmap pixel data to be converted to BGRA32 no matter what the source format is. Not valid if combined with <see cref="StrictPreserveOriginalFormat"/>.
        /// </summary>
        ForceFormatBGRA32 = BitmapCore.BC_READ_FORCE_BGRA32,

        /// <summary>
        /// Skips and ignores any embedded calibration or ICC profile data.
        /// </summary>
        IgnoreColorProfile = BitmapCore.BC_READ_IGNORE_COLOR_PROFILE,
    }

    [Flags]
    public enum BitmapWpfWriterFlags : uint
    {
        /// <summary>
        /// No special writer flags
        /// </summary>
        None = 0,

        /// <summary>
        /// This specifies that the bitmap must be created with a BITMAPV5HEADER. This is desirable if storing the image to the cliboard at CF_DIBV5 for example.
        /// </summary>
        ForceV5Header = BitmapCore.BC_WRITE_V5,

        /// <summary>
        /// This specifies that the bitmap must be created with a BITMAPINFOHEADER. This is required when storing the image to the clipboard at CF_DIB, or possibly
        /// for interoping with other applications that do not support newer bitmap files. This option is not advisable unless absolutely required - as not all bitmaps
        /// can be accurately represented. For example, no transparency data can be stored - and the images will appear fully opaque.
        /// </summary>
        ForceInfoHeader = BitmapCore.BC_WRITE_VINFO,

        /// <summary>
        /// This option requests that the bitmap be created without a BITMAPFILEHEADER (ie, in Packed DIB format). This is used when storing the file to the clipboard.
        /// </summary>
        SkipFileHeader = BitmapCore.BC_WRITE_SKIP_FH,
    }

    /// <summary>
    /// Provides a WPF implementation of BetterBmpLoaded Bitmap reader and writer. This bitmap library can read almost any kind of bitmap and 
    /// tries to do a better job than WPF does in terms of coverage and it also tries to handle some nuances of how other native applications write bitmaps, 
    /// especially when reading from or writing to the clipboard.
    /// </summary>
    public sealed class BitmapWpf
    {
        public static BitmapSource Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

        public static BitmapSource Read(Stream stream, BitmapWpfReaderFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

        public static BitmapSource Read(byte[] data) => Read(data, BitmapWpfReaderFlags.None);

        public unsafe static BitmapSource Read(byte[] data, BitmapWpfReaderFlags rFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, rFlags);
        }

        public unsafe static BitmapSource Read(byte* data, int dataLength, BitmapWpfReaderFlags rFlags)
        {
            BITMAP_READ_DETAILS info;
            uint bcrFlags = (uint)rFlags;
            BitmapCore.ReadHeader(data, dataLength, out info, bcrFlags);
            return BitmapWpfInternal.Read(ref info, data + info.imgDataOffset, bcrFlags);
        }

        public static byte[] GetBytes(BitmapSource bitmap) => GetBytes(bitmap, BitmapWpfWriterFlags.None);

        public static byte[] GetBytes(BitmapSource bitmap, BitmapWpfWriterFlags wFlags)
        {
            return BitmapWpfInternal.GetBytes(bitmap, (uint)wFlags);
        }
    }
}
