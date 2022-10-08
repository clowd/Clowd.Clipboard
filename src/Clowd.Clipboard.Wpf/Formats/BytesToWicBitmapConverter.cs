using System.Windows.Media.Imaging;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Base class for encoding images to/from bytes using the WPF/WIC encoder classes.
/// </summary>
public class BytesToWicBitmapConverter : BytesDataConverterBase<BitmapSource>
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    // from wincodec.h
    public static readonly Guid Format_Bmp = new Guid(0x0af1d87e, 0xfcfe, 0x4188, 0xbd, 0xeb, 0xa7, 0x90, 0x64, 0x71, 0xcb, 0xe3);
    public static readonly Guid Format_Png = new Guid(0x1b7cfaf4, 0x713f, 0x473c, 0xbb, 0xcd, 0x61, 0x37, 0x42, 0x5f, 0xae, 0xaf);
    public static readonly Guid Format_Ico = new Guid(0xa3a860c4, 0x338f, 0x4c17, 0x91, 0x9a, 0xfb, 0xa4, 0xb5, 0x62, 0x8f, 0x21);
    public static readonly Guid Format_Jpeg = new Guid(0x19e4a5aa, 0x5662, 0x4fc5, 0xa0, 0xc0, 0x17, 0x58, 0x02, 0x8e, 0x10, 0x57);
    public static readonly Guid Format_Tiff = new Guid(0x163bcc30, 0xe2e9, 0x4f0b, 0x96, 0x1d, 0xa3, 0xe9, 0xfd, 0xb7, 0x88, 0xa3);
    public static readonly Guid Format_Gif = new Guid(0x1f8a5601, 0x7d4d, 0x4cbd, 0x9c, 0x82, 0x1b, 0xc8, 0xd4, 0xee, 0xb9, 0xa5);
    public static readonly Guid Format_Wmp = new Guid(0x57a37caa, 0x367a, 0x4540, 0x91, 0x6b, 0xf1, 0x83, 0xc5, 0x09, 0x3a, 0x4b);
    public static readonly Guid Format_Dds = new Guid(0x9967cb95, 0x2e85, 0x4ac8, 0x8c, 0xa2, 0x83, 0xd7, 0xcc, 0xd4, 0x25, 0xc9);
    public static readonly Guid Format_Adng = new Guid(0xf3ff6d0d, 0x38c0, 0x41c4, 0xb1, 0xfe, 0x1f, 0x38, 0x24, 0xf1, 0x7b, 0x84);
    public static readonly Guid Format_Heif = new Guid(0xe1e62521, 0x6787, 0x405b, 0xa3, 0x39, 0x50, 0x07, 0x15, 0xb5, 0x76, 0x3f);
    public static readonly Guid Format_Webp = new Guid(0xe094b0e2, 0x67f2, 0x45b3, 0xb0, 0xea, 0x11, 0x53, 0x37, 0xca, 0x7c, 0xf3);
    public static readonly Guid Format_Raw = new Guid(0xfe99ce60, 0xf19c, 0x433c, 0xa3, 0xae, 0x00, 0xac, 0xef, 0xa9, 0xca, 0x21);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private readonly Guid _containerFormat;

    /// <summary>
    /// Create a new de/encoder with the specified container format.
    /// </summary>
    public BytesToWicBitmapConverter(Guid containerFormat)
    {
        _containerFormat = containerFormat;
    }

    /// <inheritdoc/>
    public override BitmapSource ReadFromBytes(byte[] data)
    {
        var decoder = GetDecoder(new MemoryStream(data));
        BitmapSource bitmapSource = decoder.Frames[0];
        return bitmapSource;
    }

    /// <inheritdoc/>
    public override byte[] WriteToBytes(BitmapSource obj)
    {
        var stream = new MemoryStream();
        var encoder = GetEncoder();
        if (obj is BitmapFrame frame) encoder.Frames.Add(frame);
        else encoder.Frames.Add(BitmapFrame.Create(obj));
        encoder.Save(stream);
        return stream.GetBuffer();
    }

    /// <summary>
    /// Creates an image decoder from the specified stream.
    /// </summary>
    protected virtual BitmapDecoder GetDecoder(Stream stream)
    {
        return BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
    }

    /// <summary>
    /// Creates an image encoder.
    /// </summary>
    protected virtual BitmapEncoder GetEncoder()
    {
        return BitmapEncoder.Create(_containerFormat);
    }
}
