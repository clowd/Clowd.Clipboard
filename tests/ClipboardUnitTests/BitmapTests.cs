using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Tests
{
    class BmpResource
    {
        public string Name;
        public byte[] Bytes;
    }

    [TestClass]
    public class BitmapTests
    {
        public IEnumerable<string> ImageResourceNames => Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.EndsWith(".bmp"));

        IEnumerable<BmpResource> TestImages()
        {
            foreach (var name in ImageResourceNames)
            {
                yield return new BmpResource()
                {
                    Name = Path.GetFileName(name),
                    Bytes = ReadAllBytesAndDispose(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                };
            }
        }

        public static byte[] ReadAllBytesAndDispose(Stream stream)
        {
            using (stream)
            {
                byte[] buffer = new byte[32768];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }

        //[TestMethod]
        //public void WPFCheckAll()
        //{
        //    var tests = TestImages().ToArray();

        //    MemoryStream ms = new MemoryStream();
        //    for (int ir = 0; ir < tests.Length; ir++)
        //    {
        //        BmpResource resource = (BmpResource)tests[ir];
        //        var bytes = resource.Bytes;

        //        var decoder = new BmpBitmapDecoder(new MemoryStream(bytes), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        //        var bitmap = decoder.Frames[0];

        //        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
        //        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        //        ms.SetLength(0);
        //        encoder.Save(ms);

        //        var bytes2 = ms.GetBuffer();

        //        var head1 = StructUtil.Deserialize<BITMAPFILEHEADER>(bytes, 0);
        //        var head2 = StructUtil.Deserialize<BITMAPFILEHEADER>(bytes2, 0);

        //        var info1 = StructUtil.Deserialize<BITMAPINFOHEADER>(bytes, 14);
        //        var info2 = StructUtil.Deserialize<BITMAPINFOHEADER>(bytes2, 14);

        //        Assert.AreEqual(head1.bfType, head2.bfType);
        //        Assert.AreEqual(head1.bfOffBits, head2.bfOffBits);
        //        Assert.AreEqual(head1.bfSize, head2.bfSize);

        //        Assert.AreEqual(info1.bV5Size, info2.bV5Size);
        //        Assert.AreEqual(info1.bV5Width, info2.bV5Width);
        //        Assert.AreEqual(info1.bV5Height, info2.bV5Height);
        //        Assert.AreEqual(info1.bV5Planes, info2.bV5Planes);
        //        Assert.AreEqual(info1.bV5BitCount, info2.bV5BitCount);
        //        Assert.AreEqual(info1.bV5Compression, info2.bV5Compression);
        //        //Assert.AreEqual(info1.bV5SizeImage, info2.bV5SizeImage);
        //        Assert.AreEqual(info1.bV5XPelsPerMeter, info2.bV5XPelsPerMeter);
        //        Assert.AreEqual(info1.bV5YPelsPerMeter, info2.bV5YPelsPerMeter);
        //        Assert.AreEqual(info1.bV5ClrUsed, info2.bV5ClrUsed);
        //        //Assert.AreEqual(info1.bV5ClrImportant, info2.bV5ClrImportant);

        //        Console.WriteLine();

        //        for (uint i = head1.bfOffBits; i < bytes.Length; i++)
        //        {
        //            var b1 = bytes[i];
        //            var b2 = bytes2[i];

        //            if (b1 != b2)
        //                Assert.Fail($"Position {i}, file '{resource.Name}': Expected {(int)b1}, Actual {(int)b2}");
        //        }

        //    }
        //}
    }
}
