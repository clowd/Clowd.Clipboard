using Clowd.Clipboard;
using Clowd.Clipboard.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ConsoleTests
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            //0-64
            //0-256
            //0b_1111_1000;

            //uint maskR = 0xF800;
            //uint bR = 0b1111_1000_0000_0000;

            //if (val & 0xFFFFFF00 == 0)
            //{

            //}

            //if(val <= 0xFF)

            //maskG = 0x03e0;
            //maskB = 0x001f;
            while (true)
            {

                using (var handle = new ClipboardHandleWpf())
                {
                    handle.Open();
                    var formats = handle.GetPresentFormats().ToArray();

                    Console.WriteLine("Formats: ");
                    foreach (var f in formats)
                    {
                        Console.WriteLine(" - " + f.Name);

                        try
                        {
                            var test = handle.GetFormatType(f, new TextUtf8Converter());

                            if (test != null && test.Length > 200)
                                test = test.Substring(0, 200);

                            Console.WriteLine("    > " + test);
                        }
                        catch { }
                    }


                    //byte[] bytes;
                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();
                    //var v3s = sw.ElapsedMilliseconds;
                    //bytes = handle.GetFormat((ClipboardFormat)ClipboardFormat.Dib);
                    //var v3e = sw.ElapsedMilliseconds;
                    //var v5s = sw.ElapsedMilliseconds;
                    //bytes = handle.GetFormat((ClipboardFormat)ClipboardFormat.DibV5);
                    //var v5e = sw.ElapsedMilliseconds;

                    //var v3 = v3e - v5s;
                    //var v5 = v5e - v5e;
                    //Console.WriteLine();
                    //bytes = handle.GetFormat((ClipboardFormat)ClipboardFormat.Dib);
                    //File.WriteAllBytes("ROMAN-2.bmp", bytes);

                    //handle.SetFormat((ClipboardFormat)ClipboardFormat.Dib, bytes);


                    //var formats = handle.GetPresentFormats().ToArray();
                    //var count = handle.count();
                    //string app = "paintdotnet";
                    //var desired = formats.First(f => f == ClipboardFormat.Dib || f == ClipboardFormat.DibV5);

                    //bytes = handle.GetFormat((ClipboardFormat)desired);
                    //File.WriteAllBytes($"clip-{app}-desired.bmp", bytes);

                    //bytes = handle.GetFormat((ClipboardFormat)ClipboardFormat.DibV5);
                    //File.WriteAllBytes($"clip-{app}-dibv5.bmp", bytes);

                    //bytes = handle.GetFormat((ClipboardFormat)ClipboardFormat.Dib);
                    //File.WriteAllBytes($"clip-{app}-dib.bmp", bytes);
                }
                Console.ReadLine();

            }

        }
    }
}
