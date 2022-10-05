using System.Windows.Media.Imaging;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Reads an image stored in a file drop list as a WPF BitmapSource.
/// </summary>
public class ImageWpfFileDrop : HandleDataConverterBase<BitmapSource>
{
    private static string[] _knownImageExt = new[]
    {
        ".png", ".jpg", ".jpeg",".jpe", ".bmp",
        ".gif", ".tif", ".tiff", ".ico"
    };

    /// <inheritdoc />
    public override int GetDataSize(BitmapSource obj)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override BitmapSource ReadFromHandle(IntPtr ptr, int memSize)
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
                return new BitmapImage(new Uri(filePath));
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override void WriteToHandle(BitmapSource obj, IntPtr ptr)
    {
        throw new NotSupportedException(
            "Cannot write a bitmap directly to the clipboard as a file drop list. " +
            "Use a different format or save it to a file first.");
    }
}
