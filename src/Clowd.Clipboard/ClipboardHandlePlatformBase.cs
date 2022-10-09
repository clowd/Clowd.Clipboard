using Clowd.Clipboard.Formats;

namespace Clowd.Clipboard;

/// <summary>
/// Provides the platform-specific clipboard methods (such as retrieving and setting images).
/// </summary>
public interface IClipboardHandlePlatform<TBitmap>
{
    /// <summary>
    /// Check if the clipboard currently contains any known image format.
    /// </summary>
    bool ContainsImage();

    /// <summary>
    /// Retrieves any detectable bitmap stored on the clipboard.
    /// </summary>
    TBitmap GetImage();

    /// <summary>
    /// Sets the image on the clipboard to the specified bitmap.
    /// </summary>
    void SetImage(TBitmap bitmap);
}

/// <summary>
/// Represents a handle to the clipboard. Open the handle via <see cref="ClipboardHandle.Open"/>, read or 
/// set the clipboard, and then dispose this class as quickly as possible. Leaving this handle
/// open for too long will prevent other applications from accessing the clipboard, and may 
/// even cause them to freeze for a time.
/// </summary>
[SupportedOSPlatform("windows")]
public abstract class ClipboardHandlePlatformBase<TBitmap> : ClipboardHandle
    where TBitmap : class
{
    private List<ClipboardFormat<TBitmap>> _prioritisedFormats = new();
    private List<ClipboardFormat<TBitmap>> _clipboardOrderFormats = new();
    private List<ClipboardFormat<TBitmap>> _otherFormats = new();

    private IEnumerable<ClipboardFormat<TBitmap>> _allImageFormats
    {
        get
        {
            foreach (var b in _prioritisedFormats) yield return b;
            foreach (var b in _clipboardOrderFormats) yield return b;
            foreach (var b in _otherFormats) yield return b;
        }
    }

    /// <summary>
    /// Creates a new <see cref="ClipboardHandlePlatformBase{TBitmap}"/>.
    /// </summary>
    protected ClipboardHandlePlatformBase()
    {
        var jpeg = GetJpegConverter();
        var png = GetPngConverter();
        var tiff = GetTiffConverter();
        var gif = GetGifConverter();

        _prioritisedFormats.Add(ClipboardFormat.Png.WithConverter(png));
        _prioritisedFormats.Add(ClipboardFormat.CreateCustomFormat("image/png", png));

        _clipboardOrderFormats.Add(ClipboardFormat.Dib.WithConverter(GetDibConverter()));
        _clipboardOrderFormats.Add(ClipboardFormat.DibV5.WithConverter(GetDibV5Converter()));
        _clipboardOrderFormats.Add(ClipboardFormat.Bitmap.WithConverter(GetGdiHandleConverter()));

        _otherFormats.Add(ClipboardFormat.Jpeg.WithConverter(jpeg));
        _otherFormats.Add(ClipboardFormat.CreateCustomFormat("JPG", jpeg));
        _otherFormats.Add(ClipboardFormat.CreateCustomFormat("JFIF", jpeg));
        _otherFormats.Add(ClipboardFormat.CreateCustomFormat("image/jpeg", jpeg));

        _otherFormats.Add(ClipboardFormat.Tiff.WithConverter(tiff));
        _otherFormats.Add(ClipboardFormat.CreateCustomFormat("image/tiff", tiff));

        _otherFormats.Add(ClipboardFormat.Gif.WithConverter(gif));
        _otherFormats.Add(ClipboardFormat.CreateCustomFormat("image/gif", gif));
    }


    /// <summary>
    /// Check if the clipboard currently contains any known image format.
    /// </summary>
    public virtual bool ContainsImage() => ContainsFormat(_allImageFormats.ToArray()) || TryGetFileDropImagePath(out _);

    /// <summary>
    /// Get a list of known image file extensions.
    /// </summary>
    protected virtual string[] KnownImageExtensions => new[]
    {
        ".png", ".jpg", ".jpeg",".jpe", ".jfif",
        ".bmp", ".gif", ".tif", ".tiff", ".ico",
    };

    /// <summary>
    /// Returns true if there is only a single file in the file drop list, and that file has an image extension.
    /// </summary>
    protected virtual bool TryGetFileDropImagePath(out string filePath)
    {
        filePath = null;

        if (!ContainsFileDropList())
            return false;

        var fileDropList = GetFileDropList();
        if (fileDropList != null && fileDropList.Length == 1)
        {
            var f = fileDropList[0];
            if (File.Exists(f) && KnownImageExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                filePath = f;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets the image on the clipboard to the specified bitmap.
    /// </summary>
    protected virtual void SetImageImpl(TBitmap bitmap)
    {
        var pngFormat = _allImageFormats.FirstOrDefault(f => f == ClipboardFormat.Png);
        if (pngFormat?.TypeObjectReader != null)
            SetFormat(pngFormat, bitmap);

        var dibFormat = _allImageFormats.FirstOrDefault(f => f == ClipboardFormat.Dib);
        if (dibFormat?.TypeObjectReader != null)
            SetFormat(dibFormat, bitmap);
    }

    /// <summary>
    /// Retrieves any detectable bitmap stored on the clipboard.
    /// </summary>
    protected virtual TBitmap GetImageImpl()
    {
        var formats = GetPresentFormats().ToArray();

        // first we search for prioritised formats (like PNG)
        foreach (var f in _prioritisedFormats)
            if (formats.Contains(f))
                if (TryGetFormatObject(f.Id, f.TypeObjectReader, out var bitmap))
                    return bitmap;

        // Windows has "Synthesized Formats", if you ask for a CF_DIBV5 when there is only a CF_DIB, it will transparently convert
        // from one format to the other. The issue is, if you ask for a CF_DIBV5 before you ask for a CF_DIB, and the CF_DIB is 
        // the only real format on the clipboard, windows can corrupt the CF_DIB!!! 
        // One quirk is that windows deterministically puts real formats in the list of present formats before it puts synthesized formats
        // so even though we can't really tell what is synthesized or not, we can make a guess based on which comes first.
        foreach (var fmt in formats)
        {
            var orderedFormat = _clipboardOrderFormats.FirstOrDefault(f => f == fmt);
            if (orderedFormat?.TypeObjectReader != null)
            {
                if (TryGetFormatObject(orderedFormat.Id, orderedFormat.TypeObjectReader, out var dib))
                {
                    if (dib != null)
                    {
                        return dib;
                    }
                }
            }
        }

        // now we search "other" formats (like JPEG)
        foreach (var f in _prioritisedFormats)
            if (formats.Contains(f))
                if (TryGetFormatObject(f.Id, f.TypeObjectReader, out var bitmap))
                    return bitmap;

        // check the windows file drop list to see if someone copied an image from explorer.
        if (TryGetFileDropImagePath(out var fileDropImagePath))
            return LoadFromFile(fileDropImagePath);

        return null;
    }

    /// <summary> Load's a bitmap from a local file path. </summary>
    protected abstract TBitmap LoadFromFile(string filePath);

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetJpegConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetTiffConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetGifConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetPngConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetGdiHandleConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetDibConverter();

    /// <summary> Get specified image converter. </summary>
    protected abstract IDataConverter<TBitmap> GetDibV5Converter();
}
