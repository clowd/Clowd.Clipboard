using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using PixelFormats = System.Windows.Media.PixelFormats;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using ImageLockMode = System.Drawing.Imaging.ImageLockMode;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Data converter for translating CF_BITMAP (gdi image handle) into a WPF BitmapSource.
/// </summary>
public class ImageBitmap : IDataConverter<BitmapSource>
{
    /// <inheritdoc/>
    public BitmapSource ReadFromHGlobal(IntPtr hGlobal)
    {
        using var bitmap = Bitmap.FromHbitmap(hGlobal);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var bitmapSource = BitmapSource.Create(
            bitmapData.Width,
            bitmapData.Height,
            bitmap.HorizontalResolution,
            bitmap.VerticalResolution,
            PixelFormats.Bgra32,
            null,
            bitmapData.Scan0,
            bitmapData.Stride * bitmapData.Height,
            bitmapData.Stride);

        bitmap.UnlockBits(bitmapData);

        return bitmapSource;
    }

    /// <inheritdoc/>
    public IntPtr WriteToHGlobal(BitmapSource obj)
    {
        throw new NotSupportedException("Should always write a DIB to the clipboard instead of a DDB.");
        //var bitmap = new FormatConvertedBitmap(obj, PixelFormats.Bgra32, null, 0);
        //using var gdi = new Bitmap(bitmap.PixelWidth, bitmap.PixelHeight, PixelFormat.Format32bppArgb);

        //var bitmapData = gdi.LockBits(
        //    new Rectangle(0, 0, gdi.Width, gdi.Height),
        //    ImageLockMode.ReadOnly,
        //    PixelFormat.Format32bppArgb);

        //obj.CopyPixels(
        //    new Int32Rect(0, 0, bitmapData.Width, bitmapData.Height), 
        //    bitmapData.Scan0, 
        //    bitmapData.Stride, 
        //    0);

        //// this sill not work. 
        //// https://stackoverflow.com/questions/35154938/how-to-place-gdi-bitmap-onto-the-clipboard?rq=1
        //return gdi.GetHbitmap();
    }
}
