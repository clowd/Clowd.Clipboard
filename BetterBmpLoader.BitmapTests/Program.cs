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
        const string htmlPage = "render.html";
        const string outputDir = "output";

        static void Main(string[] args)
        {
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);

            File.WriteAllText(htmlPage,
@"<html>
<head><style> a { color: white; } td { border: 2px solid rgba(255,255,255,0.7); padding: 10px; } html,body { background: #333; color: rgba(255,255,255,0.8); } </style></head>
<body>
<br /><br /><a target=""_blank"" href=""http://entropymine.com/jason/bmpsuite/bmpsuite/html/bmpsuite.html"">All reference images, click here</a><br /><br /><br />
<table>");

            File.AppendAllText(htmlPage, "<tr><th>FILENAME</th><th>REFERENCE</th><th>WPF</th><th>GDI</th><th>ERROR</th></tr>");

            foreach (var file in Directory.EnumerateFiles("bitmaps", "*.bmp", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                //if (!file.Contains("rgba32")) continue;
                WriteTableLine(file);
            }

            foreach (var file in Directory.EnumerateFiles("known_bad", "*.bmp", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                WriteTableLine(file);
            }

            File.AppendAllText(htmlPage, "</table></body></html>");
            //Process.Start("render.html");
            //Console.Read();
        }

        static void WriteTableLine(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var bmpPath = Path.Combine(outputDir, name + ".bmp");
            string error = "";

            File.Copy(file, bmpPath);
            var originalBytes = File.ReadAllBytes(file);
            File.AppendAllText(htmlPage, $"<tr> <td>{name}</td> <td><img src=\"{bmpPath.Replace("\\", "/")}\" /><br/><br/><img src=\"{bmpPath.Replace("\\", "/")}\" /></td>");

            // WPF
            try
            {
                string suffix = "_wpf";
                var roundPath = Path.Combine(outputDir, name + suffix + ".bmp");
                var bmp = BitmapWpf.Read(originalBytes, BitmapWpfParserFlags.PreserveInvalidAlphaChannel);
                File.WriteAllBytes(roundPath, BitmapWpf.GetBytes(bmp));

                var pngPath = Path.Combine(outputDir, name + suffix + ".png");
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(bmp);
                var ms = new MemoryStream();
                pngEncoder.Save(ms);
                File.WriteAllBytes(pngPath, ms.ToArray());

                File.AppendAllText(htmlPage, $"<td><img src=\"{pngPath.Replace("\\", "/")}\" /><br/><br/><img src=\"{roundPath.Replace("\\", "/")}\" /></td>");
            }
            catch (Exception ex)
            {
                File.AppendAllText(htmlPage, "<td></td>");
                error += ex.ToString();
            }

            // GDI
            try
            {
                string suffix = "_gdi";
                var roundPath = Path.Combine(outputDir, name + suffix + ".bmp");
                var bmp = BitmapGdi.Read(originalBytes, BitmapGdiParserFlags.PreserveInvalidAlphaChannel);
                File.WriteAllBytes(roundPath, BitmapGdi.GetBytes(bmp)); // not yet supported

                //error += bmp.PixelFormat.ToString();
                //bmp.Save(roundPath, ImageFormat.Bmp);
                //var bmp2 = BitmapGdi.Read(File.ReadAllBytes(roundPath));

                var pngPath = Path.Combine(outputDir, name + suffix + ".png");
                bmp.Save(pngPath, ImageFormat.Png);

                File.AppendAllText(htmlPage, $"<td><img src=\"{pngPath.Replace("\\", "/")}\" /><br/><br/><img src=\"{roundPath.Replace("\\", "/")}\" /></td>");
            }
            catch (Exception ex)
            {
                File.AppendAllText(htmlPage, "<td></td>");
                error += ex.ToString();
            }

            File.AppendAllText(htmlPage, $"<td>{error}</td> </tr>");
        }
    }
}
