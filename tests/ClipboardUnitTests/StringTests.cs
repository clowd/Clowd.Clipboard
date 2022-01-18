using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClipboardGapWpf.Tests
{
    [TestClass]
    public class StringTests
    {
        private string _text;
        public StringTests()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClipboardGapWpf.Tests.utf8.txt"))
            using (var reader = new StreamReader(stream, Encoding.UTF8, false))
                _text = reader.ReadToEnd();
        }

        private string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void StrCompare(string stcp, string reference = null)
        {
            if (reference == null)
                reference = _text;

            for (int i = 0; i < reference.Length; i++)
            {
                var c1 = reference[i];
                var c2 = stcp[i];

                if (c1 != c2)
                    Assert.Fail($"Char position {i}/{_text.Length}: Expected '{c1}' ({(int)c1} {EncodeNonAsciiCharacters(c1.ToString())}), Actual '{c2}' ({(int)c2} {EncodeNonAsciiCharacters(c2.ToString())}).");
            }
        }

        [TestMethod]
        public void U16toU16_Inline()
        {
            string round;
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                handle.SetFormat(ClipboardFormat.UnicodeText, _text);
                round = handle.GetFormatType(ClipboardFormat.UnicodeText);
                handle.Empty();
            }
            StrCompare(round);
        }

        [TestMethod]
        public void U16toU16_Break()
        {
            string round;
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                handle.SetFormat(ClipboardFormat.UnicodeText, _text);
            }
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                round = handle.GetFormatType(ClipboardFormat.UnicodeText);
                handle.Empty();
            }
            StrCompare(round);
        }

        [TestMethod]
        public void ANSItoU16()
        {
            var reference = "Hello, I am a test! :)";
            string round;
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                handle.SetFormat(ClipboardFormat.Text, reference);
            }
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                round = handle.GetFormatType(ClipboardFormat.UnicodeText);
                handle.Empty();
            }
            StrCompare(round, reference);
        }

        [TestMethod]
        public void U16toANSI()
        {
            var reference = "Hello, I am a test! :)";
            string round;
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                handle.SetFormat(ClipboardFormat.UnicodeText, reference);
            }
            using (var handle = new ClipboardHandle())
            {
                handle.Open();
                round = handle.GetFormatType(ClipboardFormat.Text);
                handle.Empty();
            }
            StrCompare(round, reference);
        }

        [TestMethod]
        public void ANSI_Encode()
        {
            var converter = new ClipboardGapWpf.Formats.TextAnsi();
            var hglobal = converter.WriteToHGlobal(_text);
            var back = converter.ReadFromHGlobal(hglobal);
            var ansi = Encoding.GetEncoding(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
            var tnansi = ansi.GetString(ansi.GetBytes(_text));
            StrCompare(back, tnansi);
        }

        [TestMethod]
        public void UTF16_Encode()
        {
            var converter = new ClipboardGapWpf.Formats.TextUnicode();
            var hglobal = converter.WriteToHGlobal(_text);
            var back = converter.ReadFromHGlobal(hglobal);
            var enc = Encoding.Unicode;
            var fx = enc.GetString(enc.GetBytes(_text));
            StrCompare(back, fx);
        }

        [TestMethod]
        public void UTF8_Encode()
        {
            var converter = new ClipboardGapWpf.Formats.TextUtf8();
            var hglobal = converter.WriteToHGlobal(_text);
            var back = converter.ReadFromHGlobal(hglobal);
            var enc = Encoding.UTF8;
            var fx = enc.GetString(enc.GetBytes(_text));
            StrCompare(back, fx);
        }
    }
}
