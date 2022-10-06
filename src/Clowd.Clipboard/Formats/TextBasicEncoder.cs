using System.Text;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// A base class for encoding strings for the clipboard.
/// </summary>
public abstract class TextBasicEncoder : BytesDataConverterBase<string>
{
    /// <summary>
    /// Gets the encoder used to convert a string to bytes and back.
    /// </summary>
    public abstract Encoding GetEncoding();

    /// <summary>
    /// Read a string from the specified bytes
    /// </summary>
    public override string ReadFromBytes(byte[] data) 
        => GetEncoding().GetString(data).TrimEnd('\0');

    /// <summary>
    /// Converts the specified string to bytes
    /// </summary>
    public override byte[] WriteToBytes(string obj)
        // strings need to have 1-2 null terminating characters (depends on encoding) but extra are harmless
        => GetEncoding().GetBytes(String.Concat(obj, "\0\0"));
}

/// <summary>
/// For ANSI/MultiByte encoded strings.
/// </summary>
public class TextAnsi : TextBasicEncoder
{
    /// <inheritdoc/>
    public override Encoding GetEncoding() => Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
}

/// <summary>
/// For widechar encoded strings.
/// </summary>
public class TextUnicode : TextBasicEncoder
{
    /// <inheritdoc/>
    public override Encoding GetEncoding() => Encoding.Unicode;
}

/// <summary>
/// For UTF-8 encoded strings.
/// </summary>
public class TextUtf8 : TextBasicEncoder
{
    /// <inheritdoc/>
    public override Encoding GetEncoding() => Encoding.UTF8;
}
