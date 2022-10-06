using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clowd.Clipboard.Formats;

[SupportedOSPlatform("windows")]
public class ImageGdiBitmap : BytesDataConverterBase<Bitmap>
{
    private readonly ImageFormat format;

    public ImageGdiBitmap(ImageFormat format)
    {
        this.format = format;
    }

    public override Bitmap ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return (Bitmap)Bitmap.FromStream(ms);
    }

    public override byte[] WriteToBytes(Bitmap obj)
    {
        using var ms = new MemoryStream();
        obj.Save(ms, format);
        return ms.ToArray();
    }
}
