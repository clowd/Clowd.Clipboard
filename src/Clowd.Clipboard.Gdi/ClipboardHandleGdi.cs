using Clowd.Clipboard.Formats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clowd.Clipboard;

/// <summary>
/// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandleGdi"/>.
/// </summary>
public class ClipboardGdi : ClipboardStaticBase<ClipboardHandleGdi, Bitmap>
{
    private ClipboardGdi() { }
}

/// <inheritdoc/>
public class ClipboardHandleGdi : ClipboardHandleBase<Bitmap>
{
    /// <summary>
    /// Sets the image on the clipboard to the specified bitmap.
    /// </summary>
    public override void SetImage(Bitmap bitmap)
    {
        // Write PNG format as some applications do not support alpha in DIB's and
        // also often will attempt to read PNG format first.
        SetFormatObject(ClipboardFormat.Png.Id, bitmap, new ImageGdiBitmap(ImageFormat.Png));
        SetFormatObject(ClipboardFormat.DibV5.Id, bitmap, new ImageGdiDibV5());
    }

    /// <summary>
    /// Retrieves any detectable bitmap stored on the clipboard.
    /// </summary>
    public override Bitmap GetImage()
    {
        var formats = GetPresentFormats().ToArray();

        var fmtPng = ClipboardFormat.Png;
        if (TryGetFormatObject(fmtPng.Id, new ImageGdiBitmap(ImageFormat.Png), out var png))
            if (png != null)
                return png;

        // Windows has "Synthesized Formats", if you ask for a CF_DIBV5 when there is only a CF_DIB, it will transparently convert
        // from one format to the other. The issue is, if you ask for a CF_DIBV5 before you ask for a CF_DIB, and the CF_DIB is 
        // the only real format on the clipboard, windows can corrupt the CF_DIB!!! 
        // One quirk is that windows deterministically puts real formats in the list of present formats before it puts synthesized formats
        // so even though we can't really tell what is synthesized or not, we can make a guess based on which comes first.

        Dictionary<ClipboardFormat, IDataConverter<Bitmap>> gdiFormats = new()
        {
            { ClipboardFormat.Bitmap, new ImageGdiHandle() },
            { ClipboardFormat.Dib, new ImageGdiDib() },
            { ClipboardFormat.DibV5, new ImageGdiDibV5() },
        };

        foreach (var fmt in formats)
            if (fmt == ClipboardFormat.Bitmap || fmt == ClipboardFormat.Dib || fmt == ClipboardFormat.DibV5)
                if (TryGetFormatObject(fmt.Id, gdiFormats[fmt], out var dib))
                    if (dib != null)
                        return dib;

        Dictionary<ClipboardFormat, IDataConverter<Bitmap>> imageFormats = new()
        {
            { ClipboardFormat.Tiff, new ImageGdiBitmap(ImageFormat.Tiff) },
            { ClipboardFormat.Jpg, new ImageGdiBitmap(ImageFormat.Jpeg) },
            { ClipboardFormat.Jpeg, new ImageGdiBitmap(ImageFormat.Jpeg) },
            { ClipboardFormat.Jfif, new ImageGdiBitmap(ImageFormat.Jpeg) },
            { ClipboardFormat.Gif, new ImageGdiBitmap(ImageFormat.Gif) },
            { ClipboardFormat.Png, new ImageGdiBitmap(ImageFormat.Png) },
        };

        foreach (var fmt in formats)
            if (imageFormats.ContainsKey(fmt))
                if (TryGetFormatObject(fmt.Id, imageFormats[fmt], out var img))
                    if (img != null)
                        return img;

        var fmtDrop = ClipboardFormat.FileDrop;
        if (TryGetFormatObject(fmtDrop.Id, new ImageGdiFileDrop(), out var drop))
            if (drop != null)
                return drop;

        return null;
    }
}
