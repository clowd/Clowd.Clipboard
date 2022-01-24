# Clowd.Clipboard
This library is a light-weight clipboard replacement for WPF. Because we bundle in a completely custom bitmap parser, this library does not strictly depend on WPF or GDI, so could easily be ported to use other bitmap representations. The reasons for why a custom bitmap parser was written are below, and it can be used independently of the clipboard code, if all you need is a packed dib parser.

### Clipboard Examples
Below are several examples of how to do clipboard operations.

#### Reading list of current clipboard formats

```cs
using (var handle = new ClipboardHandle())
{
    handle.Open();
    Console.WriteLine("Formats currently on the clipboard: ");
    foreach (var f in handle.GetPresentFormats())
        Console.WriteLine(" - " + f.Name);
}
```

#### Getting or setting simple supported types

```cs
using (var handle = new ClipboardHandle())
{
    handle.Open();
    string currentText = handle.GetText();
    handle.SetImage(new BitmapSource());
}
```

#### Creating and using custom clipboard formats

```cs
// a custom format stored on the clipboard as UTF-8. 
// the name chosen for the format should be globally unique, so consider
// using a guid if you do not intend to share with other applications.
private static readonly myCustomTextFormat = ClipboardFormat.CreateCustomFormat("MyAppString", new TextUtf8());

// a custom format for storing binary data
private static readonly myCustomBinaryFormat = ClipboardFormat.CreateCustomFormat("MyAppBytes");

using (var handle = new ClipboardHandle())
{
    handle.Open();

    handle.SetFormat(myCustomTextFormat, "My Custom Text");
    handle.SetFormat(myCustomBinaryFormat, new byte[0]);

    // this is a common "built-in" specific format not covered by the simple functions such as "GetText".
    handle.SetFormat(ClipboardFormat.Html, "<html>Some Html to share with other applications</html>");

    byte[] roundTrip = handle.GetFormatBytes(myCustomBinaryFormat);
}
```

# Clowd.Clipboard.Bitmaps
The purpose of this library is to attempt to create a more robust BMP/DIB parser than currently exists in WPF or in System.Drawing (GDI+). It's purpose is to provide good interop with packed dibs on the Windows Clipboard, which WPF and GDI do not support.

There are two big barriers to reading Bitmap's on the clipboard; Both `CF_DIB` and `CF_DIBV5` are available, but they lack `BITMAPFILEHEADER`'s. Additionally, WPF and GDI do not support the `BITMAPV5HEADER` present in `CF_DIBV5` and therefore can not properly read transparency data. This library has been tested with a variety of weird, rare, invalid bitmap files and supports every bitmap reference file I could come across - including rare reader formats (like OS2v1, OS2v2) and rare compression algorithms like RLE and Huffman1D.

This library may play better with bitmaps written to the clipboard by native applications than GDI or WPF can manage.

### Bitmap Known Limitations / Issues
 - Bitmap data will be translated to sRGB if calibration data and/or a embedded ICC color profile exists in the bitmap file. This was done by design, because color profile support in WPF and GDI is limited. This library does not support writing a color profile.
 - Not all pixel formats are supported natively, some are converted to Bgra32 when parsing
 - Does not support Gray colorspace bitmaps

### Bitmap Example
There is a convenient to use wrapper for both GDI and WPF, depending on your choice of UI technology.
```cs
byte[] imageBytes = File.ReadAllBytes("file.bmp");
BitmapSource bitmap = BitmapWpf.FromBytes(imageBytes, BitmapWpfReaderFlags.PreserveInvalidAlphaChannel);

// profit!
byte[] imageBytes2 = BitmapWpf.ToBytes(bitmap);
```

### Bitmap Compatibility

 - :heavy_check_mark: Files with or without a `BITMAPFILEHEADER`
 - :heavy_check_mark: Files with any known BMP header format, including WindowsV1, V2, V3, V4, V5, OS2v1, OS2v2
 - :heavy_check_mark: Files with logical color space / calibration data
 - :heavy_check_mark: Files with embedded ICC color formats
 - :heavy_check_mark: Files with any valid compression format, including RGB, BITFIELDS, ALPHABITFIELDS, JPEG, PNG, HUFFMAN1D (G31D), RLE4/8/24
 - :heavy_check_mark: Files with completely non-standard pixel layout (for example, 32bpp with the following layout: 7B-25G-0R-0A)
