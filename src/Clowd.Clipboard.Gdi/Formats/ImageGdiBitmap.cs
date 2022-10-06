using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Converts bitmap bytes to a specific encoder format: eg. PNG or TIFF
/// </summary>
[SupportedOSPlatform("windows")]
public class ImageGdiBitmap : BytesDataConverterBase<Bitmap>
{
    private readonly ImageFormat format;

    /// <summary>
    /// Creates a <see cref="ImageGdiBitmap"/> with the specified encoding.
    /// </summary>
    public ImageGdiBitmap(ImageFormat format)
    {
        this.format = format;
    }

    /// <inheritdoc/>
    public override Bitmap ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return (Bitmap)Bitmap.FromStream(ms);
    }

    /// <inheritdoc/>
    public override byte[] WriteToBytes(Bitmap obj)
    {
        using var ms = new MemoryStream();
        obj.Save(ms, format);
        return ms.ToArray();
    }
}
