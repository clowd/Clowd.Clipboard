using BetterBmpLoader.Gdi;
using BetterBmpLoader.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader.BitmapTests
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Directory.Exists("output"))
                Directory.Delete("output", true);
            Directory.CreateDirectory("output");

            File.WriteAllText("render.html",
                @"<html><head><style> a { color: white; } td { border: 2px solid rgba(255,255,255,0.7); padding: 10px; } html,body { background: #333; color: rgba(255,255,255,0.8); } </style></head><body>
<br /><br /><a target=""_blank"" href=""http://entropymine.com/jason/bmpsuite/bmpsuite/html/bmpsuite.html"">All reference images, click here</a><br /><br /><br />
<table>");

            foreach (var file in Directory.EnumerateFiles("bitmaps", "*.bmp", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                //if (!file.Contains("rgb32-7187"))
                //if (!file.Contains("huff"))
                //    continue;

                var name = Path.GetFileNameWithoutExtension(file);
                var bmpPath = Path.Combine("output", name + ".bmp");
                var pngPath = Path.Combine("output", name + ".png");
                var pngGdiPath = Path.Combine("output", name + "_gdi.bmp");

                string error = "Success";

                try
                {
                    File.Copy(file, bmpPath);
                    var data = File.ReadAllBytes(file);

                    Console.WriteLine(name);

                    // WPF
                    var bmp = BitmapWpf.Read(data, Wpf.BitmapWpfParserFlags.PreserveInvalidAlphaChannel);
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Frames.Add(bmp as BitmapFrame ?? BitmapFrame.Create(bmp));
                    var ms = new MemoryStream();
                    enc.Save(ms);
                    File.WriteAllBytes(pngPath, ms.GetBuffer());

                    // GDI
                    var b = BitmapWpf.GetBytes(bmp, BitmapWpfWriterFlags.None);
                    File.WriteAllBytes(pngGdiPath, b);

                    //var p2 = BitmapWpf.Read(b);

                    //var asd = new BmpBitmapDecoder(new MemoryStream(b), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    //var p2 = asd.Frames[0];

                    //var asdjasd = new BmpBitmapEncoder();
                    //asdjasd.Frames.Add(p2);
                    //var ms2 = new MemoryStream();
                    //asdjasd.Save(ms2);
                    //File.WriteAllBytes(pngGdiPath, ms2.GetBuffer());


                    //var bmp2 = BitmapGdi.Read(data, Gdi.CalibrationOptions.TryBestEffort, Gdi.ParserFlags.PreserveInvalidAlphaChannel);
                    //bmp2.Save(pngGdiPath, ImageFormat.Png);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

                File.AppendAllText("render.html", $"<tr> <td>{name}</td> <td><img src=\"{bmpPath.Replace("\\", "/")}\" /></td> <td><img src=\"{pngPath.Replace("\\", "/")}\" /></td> <td><img src=\"{pngGdiPath.Replace("\\", "/")}\" /></td> <td>{error}</td>");
            }


            File.AppendAllText("render.html", "</table></body></html>");
            //Process.Start("render.html");
            //Console.Read();
        }
    }
}
