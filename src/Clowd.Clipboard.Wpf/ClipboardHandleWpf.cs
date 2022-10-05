using Clowd.Clipboard.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Clowd.Clipboard;

/// <summary>
/// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandleWpf"/>.
/// </summary>
public class ClipboardWpf : ClipboardStaticBase<ClipboardHandleWpf, BitmapSource>
{
    private ClipboardWpf() { }
}

/// <inheritdoc/>
public class ClipboardHandleWpf : ClipboardHandleBase<BitmapSource>
{
    /// <summary>
    /// Sets the image on the clipboard to the specified bitmap.
    /// </summary>
    public override void SetImage(BitmapSource bitmap)
    {
        // Write PNG format as some applications do not support alpha in DIB's and
        // also often will attempt to read PNG format first.
        SetFormatObject(ClipboardFormat.Png.Id, bitmap, new ImageWpfBasicEncoderPng());
        SetFormatObject(ClipboardFormat.DibV5.Id, bitmap, new ImageWpfDibV5());
    }

    /// <summary>
    /// Retrieves any detectable bitmap stored on the clipboard.
    /// </summary>
    public override BitmapSource GetImage()
    {
        var formats = GetPresentFormats().ToArray();

        var fmtPng = ClipboardFormat.Png;
        if (TryGetFormatObject(fmtPng.Id, new ImageWpfBasicEncoderPng(), out var png))
            if (png != null)
                return png;

        // Windows has "Synthesized Formats", if you ask for a CF_DIBV5 when there is only a CF_DIB, it will transparently convert
        // from one format to the other. The issue is, if you ask for a CF_DIBV5 before you ask for a CF_DIB, and the CF_DIB is 
        // the only real format on the clipboard, windows can corrupt the CF_DIB!!! 
        // One quirk is that windows deterministically puts real formats in the list of present formats before it puts synthesized formats
        // so even though we can't really tell what is synthesized or not, we can make a guess based on which comes first.

        Dictionary<ClipboardFormat, IDataConverter<BitmapSource>> gdiFormats = new()
        {
            { ClipboardFormat.Bitmap, new ImageBitmap() },
            { ClipboardFormat.Dib, new ImageWpfDib() },
            { ClipboardFormat.DibV5, new ImageWpfDibV5() },
        };

        foreach (var fmt in formats)
            if (fmt == ClipboardFormat.Bitmap || fmt == ClipboardFormat.Dib || fmt == ClipboardFormat.DibV5)
                if (TryGetFormatObject(fmt.Id, gdiFormats[fmt], out var dib))
                    if (dib != null)
                        return dib;

        Dictionary<ClipboardFormat, IDataConverter<BitmapSource>> imageFormats = new()
        {
            { ClipboardFormat.Tiff, new ImageWpfBasicEncoderTiff() },
            { ClipboardFormat.Jpg, new ImageWpfBasicEncoderJpeg() },
            { ClipboardFormat.Jpeg, new ImageWpfBasicEncoderJpeg() },
            { ClipboardFormat.Jfif, new ImageWpfBasicEncoderJpeg() },
            { ClipboardFormat.Gif, new ImageWpfBasicEncoderGif() },
            { ClipboardFormat.Png, new ImageWpfBasicEncoderPng() },
        };

        foreach (var fmt in formats)
            if (imageFormats.ContainsKey(fmt))
                if (TryGetFormatObject(fmt.Id, imageFormats[fmt], out var img))
                    if (img != null)
                        return img;

        var fmtDrop = ClipboardFormat.FileDrop;
        if (TryGetFormatObject(fmtDrop.Id, new ImageWpfFileDrop(), out var drop))
            if (drop != null)
                return drop;

        return null;
    }
}
