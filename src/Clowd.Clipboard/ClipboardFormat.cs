using Clowd.Clipboard.Formats;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;

namespace Clowd.Clipboard
{
    /// <summary>
    /// A class containing a list of all built-in registered clipboard types, as well as 
    /// helper functions for registering new custom formats.
    /// </summary>
    public class ClipboardFormat : IEquatable<ClipboardFormat>
    {
        /// <summary>
        /// The list of clipboard formats currently known to this process.
        /// </summary>
        public static ClipboardFormat[] Formats => _lookup.Values.ToArray();
        private static readonly Dictionary<uint, ClipboardFormat> _lookup = new Dictionary<uint, ClipboardFormat>();

        private const uint
            CF_TEXT = 1,
            CF_BITMAP = 2,
            CF_METAFILEPICT = 3,
            CF_SYLK = 4,
            CF_DIF = 5,
            CF_TIFF = 6,
            CF_OEMTEXT = 7,
            CF_DIB = 8,
            CF_PALETTE = 9,
            CF_PENDATA = 10,
            CF_RIFF = 11,
            CF_WAVE = 12,
            CF_UNICODETEXT = 13,
            CF_ENHMETAFILE = 14,
            CF_HDROP = 15,
            CF_LOCALE = 16,
            CF_DIBV5 = 17;

        // === STANDARD FORMATS ===

        /// <summary> CF_TEXT built-in, possibly synthesized format. Represents text encoded as Ansi. </summary>
        public static readonly ClipboardFormat<string> Text = DefaultFormat(CF_TEXT, "Text", new TextAnsi());
        /// <summary> CF_BITMAP built-in, possibly synthesized format. Represents image data stored as a GDI handle to a Device Dependent Bitmap. </summary>
        public static readonly ClipboardFormat Bitmap = DefaultFormat(CF_BITMAP, "Bitmap");
        /// <summary> CF_METAFILEPICT built-in format. Handle to a metafile picture format as defined by the METAFILEPICT structure. </summary>
        public static readonly ClipboardFormat MetafilePict = DefaultFormat(CF_METAFILEPICT, "MetaFilePict");
        /// <summary> CF_SYLK built-in format. Microsoft Symbolic Link (SYLK) format. </summary>
        public static readonly ClipboardFormat SymbolicLink = DefaultFormat(CF_SYLK, "SymbolicLink");
        /// <summary> CF_DIF built-in format. Software Arts' Data Interchange Format. </summary>
        public static readonly ClipboardFormat DataInterchangeFormat = DefaultFormat(CF_DIF, "DataInterchangeFormat");
        /// <summary> CF_TIFF built-in format. Tagged-image file format. </summary>
        public static readonly ClipboardFormat Tiff = DefaultFormat(CF_TIFF, "TaggedImageFileFormat");
        /// <summary> CF_OEMTEXT built-in format. Text format containing characters in the OEM character set. </summary>
        public static readonly ClipboardFormat<string> OemText = DefaultFormat(CF_OEMTEXT, "OEMText", new TextAnsi());
        /// <summary> CF_DIB built-in, possibly synthesized format. A memory object containing a BITMAPINFO structure followed by the bitmap bits. </summary>
        public static readonly ClipboardFormat Dib = DefaultFormat(CF_DIB, "DeviceIndependentBitmap");
        /// <summary> CF_PALETTE built-in format. Handle to a color palette. Whenever an application places data in the clipboard that depends
        /// on or assumes a color palette, it should place the palette on the clipboard as well. </summary>
        public static readonly ClipboardFormat Palette = DefaultFormat(CF_PALETTE, "Palette");
        /// <summary> CF_PENDATA built-in format. Data for the pen extensions to the Microsoft Windows for Pen Computing. </summary>
        public static readonly ClipboardFormat PenData = DefaultFormat(CF_PENDATA, "PenData");
        /// <summary> CF_RIFF built-in format. Represents audio data more complex than can be represented in a CF_WAVE standard wave format. </summary>
        public static readonly ClipboardFormat RiffAudio = DefaultFormat(CF_RIFF, "RiffAudio");
        /// <summary> CF_WAVE built-in format. Represents audio data in one of the standard wave formats, such as 11 kHz or 22 kHz PCM. </summary>
        public static readonly ClipboardFormat WaveAudio = DefaultFormat(CF_WAVE, "WaveAudio");
        /// <summary> CF_UNICODETEXT built-in, possibly synthesized format. Represents text encoded as widechar. </summary>
        public static readonly ClipboardFormat<string> UnicodeText = DefaultFormat(CF_UNICODETEXT, "UnicodeText", new TextUnicode());
        /// <summary> CF_ENHMETAFILE built-in format. A handle to an enhanced metafile (HENHMETAFILE). </summary>
        public static readonly ClipboardFormat EnhancedMetafile = DefaultFormat(CF_ENHMETAFILE, "EnhancedMetafile");
        /// <summary> CF_HDROP built-in format. A handle to type HDROP that identifies a list of files. </summary>
        public static readonly ClipboardFormat<string[]> FileDrop = DefaultFormat(CF_HDROP, "FileDrop", new FileDrop());
        /// <summary> CF_LOCALE built-in format. The data is a handle (HGLOBAL) to the locale identifier (LCID) associated with text in the clipboard. </summary>
        public static readonly ClipboardFormat<CultureInfo> Locale = DefaultFormat(CF_LOCALE, "Locale", new Locale());
        /// <summary> CF_DIBV5 built-in, possibly synthesized format. 
        /// A memory object containing a BITMAPV5HEADER structure followed by the bitmap color space information and the bitmap bits. </summary>
        public static readonly ClipboardFormat DibV5 = DefaultFormat(CF_DIBV5, "Format17");

        // === CUSTOM FORMATS ===

        /// <summary> HTML encoded in UTF-8. </summary>
        public static readonly ClipboardFormat<string> Html = DefaultFormat("HTML Format", new TextUtf8());
        /// <summary> Rich text format encoded in ANSI. </summary>
        public static readonly ClipboardFormat<string> Rtf = DefaultFormat("Rich Text Format", new TextAnsi());
        /// <summary> CSV encoded in ANSI. </summary>
        public static readonly ClipboardFormat<string> Csv = DefaultFormat("CSV", new TextAnsi());
        /// <summary> XAML encoded in UTF-8. </summary>
        public static readonly ClipboardFormat<string> Xaml = DefaultFormat("Xaml", new TextUtf8());
        /// <summary> JPEG image format. </summary>
        public static readonly ClipboardFormat Jpg = DefaultFormat("JPG");
        /// <summary> JPEG image format. </summary>
        public static readonly ClipboardFormat Jpeg = DefaultFormat("JPEG");
        /// <summary> JPEG image format. </summary>
        public static readonly ClipboardFormat Jfif = DefaultFormat("Jfif");
        /// <summary> GIF image format. </summary>
        public static readonly ClipboardFormat Gif = DefaultFormat("Gif");
        /// <summary> PNG image format. </summary>
        public static readonly ClipboardFormat Png = DefaultFormat("PNG");
        /// <summary> Specifies the user action that created the current clipboard state (eg. copied or cut files to clipboard). </summary>
        public static readonly ClipboardFormat<int> DropEffect = DefaultFormat("Preferred DropEffect", new Int32DataConverter());
        /// <summary> Legacy format for storing a single file path on the clipboard as an asni string. </summary>
        [Obsolete] public static readonly ClipboardFormat<string> FileName = DefaultFormat("FileName", new TextAnsi());
        /// <summary> Legacy format for storing a single file path on the clipboard as a widechar string. </summary>
        [Obsolete] public static readonly ClipboardFormat<string> FileNameW = DefaultFormat("FileNameW", new TextUnicode());

        /// <summary>
        /// The Id of this clipboard format. Could be a built-in format like CF_TEXT or CF_BITMAP, 
        /// or could be the Id of a custom clipboard format that was registered with User32!RegisterClipboardFormat.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// The human-friendly name of this clipboard format. 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Do not use.
        /// </summary>
        protected ClipboardFormat()
        {
        }

        /// <summary>
        /// Create a new clipboard format class. This does not register the format with windows, 
        /// use <see cref="CreateCustomFormat(string)"/> instead.
        /// </summary>
        protected ClipboardFormat(uint std, string name)
        {
            Id = std;
            Name = name;
        }

        /// <summary>
        /// Used internally by this library to register built-in types. Use <see cref="CreateCustomFormat(string)"/> for registering
        /// clipboard formats specific to your application.
        /// </summary>
        protected static ClipboardFormat<T> DefaultFormat<T>(uint formatId, string formatName, IDataConverter<T> formats)
        {
            var fmt = new ClipboardFormat<T>(formatId, formatName, formats);
            _lookup.Add(formatId, fmt);
            return fmt;
        }

        /// <summary>
        /// Used internally by this library to register built-in types. Use <see cref="CreateCustomFormat(string)"/> for registering
        /// clipboard formats specific to your application.
        /// </summary>
        protected static ClipboardFormat DefaultFormat(uint formatId, string formatName)
        {
            var fmt = new ClipboardFormat(formatId, formatName);
            _lookup.Add(formatId, fmt);
            return fmt;
        }

        /// <summary>
        /// Used internally by this library to register built-in types. Use <see cref="CreateCustomFormat(string)"/> for registering
        /// clipboard formats specific to your application.
        /// </summary>
        protected static ClipboardFormat DefaultFormat(string formatName)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return DefaultFormat(formatId, formatName);
        }

        /// <summary>
        /// Used internally by this library to register built-in types. Use <see cref="CreateCustomFormat(string)"/> for registering
        /// clipboard formats specific to your application.
        /// </summary>
        protected static ClipboardFormat<T> DefaultFormat<T>(string formatName, IDataConverter<T> formats)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return DefaultFormat(formatId, formatName, formats);
        }

        /// <summary>
        /// Gets the clipboard format currently registered with the specified Id,
        /// or throws an exception if this clipboard format does not exist.
        /// </summary>
        public static ClipboardFormat GetFormatById(uint formatId)
        {
            if (_lookup.TryGetValue(formatId, out var std))
                return std;

            StringBuilder sb = new StringBuilder(255);
            var len = NativeMethods.GetClipboardFormatName(formatId, sb, 255);
            if (len == 0)
                throw new Win32Exception();

            return new ClipboardFormat(formatId, sb.ToString());
        }

        /// <summary>
        /// Registers a new custom clipboard format with windows.
        /// </summary>
        public static ClipboardFormat CreateCustomFormat(string formatName)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return new ClipboardFormat(formatId, formatName);
        }

        /// <summary>
        /// Registers a new custom clipboard format with windows, and specifies a converter that should
        /// be used to read and write data from a managed object into the clipboard.
        /// </summary>
        public static ClipboardFormat<T> CreateCustomFormat<T>(string formatName, IDataConverter<T> converter)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return new ClipboardFormat<T>(formatId, formatName, converter);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ClipboardFormat other) return Equals(other);
            return false;
        }

        /// <inheritdoc/>
        public override string ToString() => $"Id={Id}, Name={Name}";

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc/>
        public bool Equals(ClipboardFormat other) => other.Id == Id;
    }

    /// <summary>
    /// A clipboard format containing a custom data converter, allowing it to be transformed into 
    /// a managed type easily.
    /// </summary>
    public class ClipboardFormat<T> : ClipboardFormat
    {
        /// <summary>
        /// The data converter used to translate a managed type to and from a clipboard handle.
        /// </summary>
        public IDataConverter<T> TypeObjectReader { get; private set; }

        /// <summary>
        /// Create a new clipboard format class. This does not register the format with windows, 
        /// use <see cref="ClipboardFormat.CreateCustomFormat{T}(string, IDataConverter{T})"/> instead.
        /// </summary>
        public ClipboardFormat(uint std, string name, IDataConverter<T> formats) : base(std, name)
        {
            TypeObjectReader = formats;
        }

        /// <summary>
        /// Replaces the current format data converter with the specified converter.
        /// </summary>
        public void SetObjectReader(IDataConverter<T> reader)
        {
            TypeObjectReader = reader;
        }
    }
}
