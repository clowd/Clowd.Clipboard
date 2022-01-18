using System;
using System.Text;

namespace Clowd.ClipLib.Formats
{
    public abstract class TextBasicEncoder : BytesDataConverterBase<string>
    {
        public abstract Encoding GetEncoding();

        public override string ReadFromBytes(byte[] data) => GetEncoding().GetString(data).TrimEnd('\0');

        // strings need to have 1-2 null terminating characters (depends on encoding) but extra are harmless
        public override byte[] WriteToBytes(string obj) => GetEncoding().GetBytes(String.Concat(obj, "\0\0"));
    }

    public class TextAnsi : TextBasicEncoder
    {
        public override Encoding GetEncoding() => Encoding.GetEncoding(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
    }

    public class TextUnicode : TextBasicEncoder
    {
        public override Encoding GetEncoding() => Encoding.Unicode;
    }

    public class TextUtf8 : TextBasicEncoder
    {
        public override Encoding GetEncoding() => Encoding.UTF8;
    }
}
