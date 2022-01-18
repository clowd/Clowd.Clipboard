using Clowd.Bitmaps.Core;

namespace Clowd.Bitmaps;

/// <summary>
/// Flags for customizing behavior when reading data into a Bitmap implementation.
/// </summary>
[Flags]
public enum BitmapReaderFlags : uint
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

/// <summary>
/// Flags for customizing behavior when translating a Bitmap implementation into data.
/// </summary>
[Flags]
public enum BitmapWriterFlags : uint
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

public interface IBitmapConverter<TBitmap>
{
    public TBitmap Read(Stream stream);
    public TBitmap Read(Stream stream, BitmapReaderFlags pFlags);
    public TBitmap Read(byte[] data);
    public TBitmap Read(byte[] data, BitmapReaderFlags pFlags);
    public unsafe TBitmap Read(byte* data, int dataLength, BitmapReaderFlags rFlags);
    public byte[] GetBytes(TBitmap bitmap);
    public byte[] GetBytes(TBitmap bitmap, BitmapWriterFlags wFlags);
}

public abstract class BitmapConverterBase<TBitmap> : IBitmapConverter<TBitmap>
{
    /// <inheritdoc/>
    public byte[] GetBytes(TBitmap bitmap) => GetBytes(bitmap, BitmapWriterFlags.None);

    /// <inheritdoc/>
    public abstract byte[] GetBytes(TBitmap bitmap, BitmapWriterFlags wFlags);

    /// <inheritdoc/>
    public TBitmap Read(Stream stream) => Read(StructUtil.ReadBytes(stream));

    /// <inheritdoc/>
    public TBitmap Read(Stream stream, BitmapReaderFlags pFlags) => Read(StructUtil.ReadBytes(stream), pFlags);

    /// <inheritdoc/>
    public TBitmap Read(byte[] data) => Read(data, BitmapReaderFlags.None);

    /// <inheritdoc/>
    public unsafe TBitmap Read(byte[] data, BitmapReaderFlags pFlags)
    {
        fixed (byte* ptr = data)
            return Read(ptr, data.Length, pFlags);
    }

    /// <inheritdoc/>
    public abstract unsafe TBitmap Read(byte* data, int dataLength, BitmapReaderFlags rFlags);
}