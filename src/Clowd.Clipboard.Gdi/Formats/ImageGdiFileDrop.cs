
using System.Drawing;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Reads an image stored in a file drop list as a WPF BitmapSource.
/// </summary>
[SupportedOSPlatform("windows")]
public class ImageGdiFileDrop : HandleDataConverterBase<Bitmap>
{
    private static string[] _knownImageExt = new[]
    {
        ".png", ".jpg", ".jpeg",".jpe", ".bmp",
        ".gif", ".tif", ".tiff", ".ico"
    };

    /// <inheritdoc />
    public override int GetDataSize(Bitmap obj)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override Bitmap ReadFromHandle(IntPtr ptr, int memSize)
    {
        var reader = new FileDrop();
        var fileDropList = reader.ReadFromHandle(ptr, memSize);

        // if - there is a single file in the file drop list
        //    - the file in the file drop list is an image (file name ends with image extension)
        //    - the file exists on disk

        if (fileDropList != null && fileDropList.Length == 1)
        {
            var filePath = fileDropList[0];
            if (File.Exists(filePath) && _knownImageExt.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return (Bitmap)Bitmap.FromFile(filePath);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override void WriteToHandle(Bitmap obj, IntPtr ptr)
    {
        throw new NotSupportedException(
            "Cannot write a bitmap directly to the clipboard as a file drop list. " +
            "Use a different format or save it to a file first.");
    }
}
