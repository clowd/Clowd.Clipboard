using Clowd.Clipboard.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Clowd.Clipboard
{
    public class ClipboardFormat : IEquatable<ClipboardFormat>
    {
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

        // STANDARD FORMATS
        public static readonly ClipboardFormat<string> Text = DefaultFormat(CF_TEXT, "Text", new TextAnsi());
        public static readonly ClipboardFormat Bitmap = DefaultFormat(CF_BITMAP, "Bitmap");
        public static readonly ClipboardFormat MetafilePict = DefaultFormat(CF_METAFILEPICT, "MetaFilePict");
        public static readonly ClipboardFormat SymbolicLink = DefaultFormat(CF_SYLK, "SymbolicLink");
        public static readonly ClipboardFormat DataInterchangeFormat = DefaultFormat(CF_DIF, "DataInterchangeFormat");
        public static readonly ClipboardFormat<BitmapSource> Tiff = DefaultFormat(CF_TIFF, "TaggedImageFileFormat", new ImageWpfBasicEncoderTiff());
        public static readonly ClipboardFormat<string> OemText = DefaultFormat(CF_OEMTEXT, "OEMText", new TextAnsi());
        public static readonly ClipboardFormat<BitmapSource> Dib = DefaultFormat(CF_DIB, "DeviceIndependentBitmap", new ImageWpfDib());
        public static readonly ClipboardFormat Palette = DefaultFormat(CF_PALETTE, "Palette");
        public static readonly ClipboardFormat PenData = DefaultFormat(CF_PENDATA, "PenData");
        public static readonly ClipboardFormat RiffAudio = DefaultFormat(CF_RIFF, "RiffAudio");
        public static readonly ClipboardFormat WaveAudio = DefaultFormat(CF_WAVE, "WaveAudio");
        public static readonly ClipboardFormat<string> UnicodeText = DefaultFormat(CF_UNICODETEXT, "UnicodeText", new TextUnicode());
        public static readonly ClipboardFormat EnhancedMetafile = DefaultFormat(CF_ENHMETAFILE, "EnhancedMetafile");
        public static readonly ClipboardFormat<string[]> FileDrop = DefaultFormat(CF_HDROP, "FileDrop", new FileDrop());
        public static readonly ClipboardFormat<CultureInfo> Locale = DefaultFormat(CF_LOCALE, "Locale", new Locale());
        public static readonly ClipboardFormat<BitmapSource> DibV5 = DefaultFormat(CF_DIBV5, "Format17", new ImageWpfDibV5());

        // CUSTOM FORMATS
        public static readonly ClipboardFormat<string> Html = DefaultFormat("HTML Format", new TextUtf8());
        public static readonly ClipboardFormat<string> Rtf = DefaultFormat("Rich Text Format", new TextAnsi());
        public static readonly ClipboardFormat<string> Csv = DefaultFormat("CSV", new TextAnsi());
        public static readonly ClipboardFormat<string> Xaml = DefaultFormat("Xaml", new TextUtf8());
        public static readonly ClipboardFormat<BitmapSource> Jpg = DefaultFormat("JPG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Jpeg = DefaultFormat("JPEG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Jfif = DefaultFormat("Jfif", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Gif = DefaultFormat("Gif", new ImageWpfBasicEncoderGif());
        public static readonly ClipboardFormat<BitmapSource> Png = DefaultFormat("PNG", new ImageWpfBasicEncoderPng());
        public static readonly ClipboardFormat<DragDropEffects> DropEffect = DefaultFormat("Preferred DropEffect", new DropEffect());
        [Obsolete] public static readonly ClipboardFormat<string> FileName = DefaultFormat("FileName", new TextAnsi());
        [Obsolete] public static readonly ClipboardFormat<string> FileNameW = DefaultFormat("FileNameW", new TextUnicode());

        public uint Id { get; }
        public string Name { get; }

        protected ClipboardFormat(uint std, string name)
        {
            Id = std;
            Name = name;
        }

        private static ClipboardFormat<T> DefaultFormat<T>(uint formatId, string formatName, IDataConverter<T> formats)
        {
            var fmt = new ClipboardFormatDRVP<T>(formatId, formatName, formats);
            _lookup.Add(formatId, fmt);
            return fmt;
        }

        private static ClipboardFormat DefaultFormat(uint formatId, string formatName)
        {
            var fmt = new ClipboardFormat(formatId, formatName);
            _lookup.Add(formatId, fmt);
            return fmt;
        }

        private static ClipboardFormat<T> DefaultFormat<T>(string formatName, IDataConverter<T> formats)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return DefaultFormat(formatId, formatName, formats);
        }

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

        public static ClipboardFormat CreateCustomFormat(string formatName)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return new ClipboardFormat(formatId, formatName);
        }

        public static ClipboardFormat<T> CreateCustomFormat<T>(string formatName, IDataConverter<T> converter)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return new ClipboardFormatDRVP<T>(formatId, formatName, converter);
        }

        public override bool Equals(object obj)
        {
            if (obj is ClipboardFormat other) return Equals(other);
            return false;
        }

        public override string ToString() => $"Id={Id}, Name={Name}";

        public override int GetHashCode() => Id.GetHashCode();

        public bool Equals(ClipboardFormat other) => other.Id == Id;
    }

    public class ClipboardFormat<T> : ClipboardFormat
    {
        public IDataConverter<T> TypeObjectReader { get; private set; }
        protected ClipboardFormat(uint std, string name, IDataConverter<T> formats) : base(std, name)
        {
            TypeObjectReader = formats;
        }

        public void SetObjectReader(IDataConverter<T> reader)
        {
            TypeObjectReader = reader;
        }
    }

    internal class ClipboardFormatDRVP<T> : ClipboardFormat<T>
    {
        public ClipboardFormatDRVP(uint std, string name, IDataConverter<T> formats) : base(std, name, formats)
        {
        }
    }
}
