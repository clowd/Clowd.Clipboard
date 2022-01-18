using Clowd.BmpLib.Gdi;
using Clowd.BmpLib.Wpf;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Clowd.BmpLib.BitmapTests
{
    class Program
    {
        const string htmlPage = "render.html";
        const string outputDir = "output";

        static unsafe void Main(string[] args)
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

            foreach (var file in Directory.EnumerateFiles("bitmaps", "*", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                WriteTableLine(file);
            }

            File.AppendAllText(htmlPage, "</table><br/><br/><span>Images copied from the clipboard</span><br/><br/><table>");

            foreach (var file in Directory.EnumerateFiles("clipboard", "*", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                WriteTableLine(file, "bitmaps\\rgba32-1.bmp");
            }

            File.AppendAllText(htmlPage, "</table><br/><br/><span>Known bad images are below. We are testing these to make sure we do not cause any fatal memory violations</span><br/><br/><table>");

            foreach (var file in Directory.EnumerateFiles("known_bad", "*", SearchOption.TopDirectoryOnly).OrderBy(k => k))
            {
                WriteTableLine(file);
            }

            File.AppendAllText(htmlPage, "</table></body></html>");
            //Process.Start("render.html");
        }

        static void WriteTableLine(string file, string defaultReferenceFile = null)
        {
            if (!(/*file.Contains("clip") &&*/ file.EndsWith(".bmp")))
                return;

            var name = Path.GetFileNameWithoutExtension(file);
            var bmpPath = Path.Combine(outputDir, name + ".bmp");
            var refPath = file.Substring(0, file.Length - 4) + ".png";
            var refTargetPath = Path.Combine(outputDir, name + ".png");
            string error = "";

            if (File.Exists(refPath))
                File.Copy(refPath, refTargetPath);
            else if (defaultReferenceFile != null)
                refTargetPath = defaultReferenceFile;

            File.Copy(file, bmpPath);
            var originalBytes = File.ReadAllBytes(file);
            File.AppendAllText(htmlPage, $"<tr> <td>{name}</td> <td><img src=\"{refTargetPath.Replace("\\", "/")}\" /><br/><br/><img src=\"{bmpPath.Replace("\\", "/")}\" /></td>");

            // WPF
            try
            {
                string suffix = "_wpf";
                var roundPath = Path.Combine(outputDir, name + suffix + ".bmp");
                var bmp = BitmapWpf.Read(originalBytes, BitmapWpfReaderFlags.PreserveInvalidAlphaChannel);
                File.WriteAllBytes(roundPath, BitmapWpf.GetBytes(bmp, BitmapWpfWriterFlags.None));

                var pngPath = Path.Combine(outputDir, name + suffix + ".png");
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(bmp));
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
                var bmp = BitmapGdi.Read(originalBytes, BitmapGdiReaderFlags.PreserveInvalidAlphaChannel);
                File.WriteAllBytes(roundPath, BitmapGdi.GetBytes(bmp, BitmapGdiWriterFlags.None));

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
