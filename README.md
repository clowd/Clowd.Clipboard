

# Clowd.Clipboard
This library is a light-weight clipboard replacement library for dotnet. It is not tightly coupled to a UI framework, and it contains a [custom bitmap parser](#clowdclipboardbitmaps) to help deal with some of the inconsistencies which are present in clipboard images.

Join us on Discord: [![Discord](https://img.shields.io/discord/767856501477343282?style=flat-square&color=purple)](https://discord.gg/M6he8ZPAAJ)



| **Package**                                                  | **Nuget**                                                    | **Notes**                                                    |
| ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| [Clowd.Clipboard](https://www.nuget.org/packages/Clowd.Clipboard) | [![Nuget](https://img.shields.io/nuget/v/Clowd.Clipboard?style=flat-square)](https://www.nuget.org/packages/Clowd.Clipboard/) | Core library containing basic clipboard functionality. No image/bitmap support. |
| [Clowd.Clipboard.Gdi](https://www.nuget.org/packages/Clowd.Clipboard.Gdi) | [![Nuget](https://img.shields.io/nuget/v/Clowd.Clipboard.Gdi?style=flat-square)](https://www.nuget.org/packages/Clowd.Clipboard.Gdi/) | Adds `ClipboardGdi`, images using `System.Drawing.Bitmap`.   |
| [Clowd.Clipboard.Wpf](https://www.nuget.org/packages/Clowd.Clipboard.Wpf) | [![Nuget](https://img.shields.io/nuget/v/Clowd.Clipboard.Wpf?style=flat-square)](https://www.nuget.org/packages/Clowd.Clipboard.Wpf/) | Adds `ClipboardWpf`, images using `System.Windows.Media.Imaging.BitmapSource`. |
| [Clowd.Clipboard.Avalonia](https://www.nuget.org/packages/Clowd.Clipboard.Avalonia) | [![Nuget](https://img.shields.io/nuget/v/Clowd.Clipboard.Avalonia?style=flat-square)](https://www.nuget.org/packages/Clowd.Clipboard.Avalonia/) | Adds `ClipboardAvalonia`, images using `Avalonia.Media.Imaging.Bitmap`. |



## Clipboard Examples

Below are several examples of how to do clipboard operations.



### Getting or setting simple types (single operation)

The static helper (eg. `ClipboardWpf` or `ClipboardGdi`) has methods for simple things. This takes care of opening the clipboard, reading the data, and then disposing the handle.

```cs
// get text on clipboard
string clipboardText = await ClipboardWpf.GetTextAsync();

// get image on clipboard
BitmapSource clipboardImg = await ClipboardWpf.GetImageAsync();

// set image on clipboard (this clears what was previously on clipboard)
ClipboardWpf.SetImage(clipboardImg);
```



### Getting or setting complex types (multi operation)

You should not use the static helper methods if you need to perform several clipboard operations. If you need to read or set multiple formats at once, or check for what formats are available, you should instead open a clipboard handle and dispose it when you're done.

```cs
BitmapSource clipImage;
string clipText;

using (var handle = await ClipboardWpf.OpenAsync());
{
    if (handle.ContainsText()) 
    {
        clipText = handle.GetText();
    }
    
    if (handle.ContainsImage())
    {
        clipImage = handle.GetImage();
    }
}
```



### Reading list of currently available clipboard formats

```cs
using (var handle = await ClipboardWpf.OpenAsync());
{
    Console.WriteLine("Formats currently on the clipboard: ");
    foreach (var f in handle.GetPresentFormats())
        Console.WriteLine(" - " + f.Name);
}
```



### Using custom clipboard formats

Using a custom/application specific format is very easy with this library. You should first register your format somewhere statically in your application.

```cs
// a custom format stored on the clipboard as UTF-8. 
// the name chosen for the format should be globally unique, so consider
// using a guid if you do not intend to share with other applications.
private static readonly ClipboardFormat<string> myCustomTextFormat 
    = ClipboardFormat.CreateCustomFormat("MyGloballyUniqueFormatId", new TextUtf8());

// a custom format for storing binary data
private static readonly ClipboardFormat myCustomBinaryFormat 
    = ClipboardFormat.CreateCustomFormat("MyGloballyUniqueFormatId_2");
```

Once your clipboard format is registered, you can use it the same way as any built-in format.

```csharp
// set custom data to clipboard
using (var handle = await ClipboardWpf.OpenAsync())
{
    // it is possible to set multiple items to the clipboard.
    handle.SetFormat(myCustomTextFormat, "My Custom Text");
    handle.SetFormat(myCustomBinaryFormat, new byte[0]);

    // this is a common "built-in" specific format not covered by the simple functions such as "GetText".
    handle.SetFormat(ClipboardFormat.Html, "<html>Some Html to share with other applications</html>");
}

// later, read custom data from clipboard
using (var handle = await ClipboardWpf.OpenAsync())
{
    if (handle.ContainsFormat(myCustomTextFormat)) 
    {
        string myCustomText = handle.GetFormatType(myCustomTextFormat);
    }
    if (handle.ContainsFormat(myCustomBinaryFormat)) 
    {
    	byte[] myCustomBytes = handle.GetFormatBytes(myCustomBinaryFormat);  
    }
}
```

**Note:** you can use `CreateCustomFormat(string formatName)` to read data added to the clipboard by other windows applications. You can provide your own `IDataConverter<T>` if you wish to automatically translate this to a dotnet type using the `GetFormatType` method, or you can retrieve the data as a `byte[]` with the `GetFormatBytes` method.



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
