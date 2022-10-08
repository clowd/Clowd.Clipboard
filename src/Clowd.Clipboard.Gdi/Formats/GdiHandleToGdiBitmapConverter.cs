using System.Drawing;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Data converter for translating CF_BITMAP (gdi image handle) into a WPF BitmapSource.
/// </summary>
[SupportedOSPlatform("windows")]
public class GdiHandleToGdiBitmapConverter : IDataConverter<Bitmap>
{
    /// <inheritdoc/>
    public Bitmap ReadFromHGlobal(IntPtr hGlobal)
    {
        return Bitmap.FromHbitmap(hGlobal);
    }

    /// <inheritdoc/>
    public IntPtr WriteToHGlobal(Bitmap obj)
    {
        throw new NotSupportedException("Should always write a DIB to the clipboard instead of a DDB.");
    }
}
