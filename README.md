## Clowd.BmpLib
The purpose of this library is to attempt to create a more robust BMP/DIB parser than currently exists in WPF or in System.Drawing (GDI+). It's initial purpose was to provide good interop with packed dibs on the Windows Clipboard. 

There are two big barriers to reading Bitmap's on the clipboard; Both `CF_DIB` and `CF_DIBV5` are available, but they lack `BITMAPFILEHEADER`'s. Additionally, WPF and GDI do not support the `BITMAPV5HEADER` present in `CF_DIBV5` and therefore can not properly read transparency data. This library has been tested with a variety of weird, rare, invalid bitmap files and supports every bitmap reference file I could come across - including rare reader formats (like OS2v1, OS2v2) and rare compression algorithms like RLE and Huffman1D.

### When should I use this library?
If you have a need to read or write bitmaps to the windows clipboard, or if you need excellent compatibility with bitmaps written by other native applications, this library may possibly do a better job than GDI or WPF.

### How do I get it?

 - Git Submodule: You can include `BitmapCore.cs` as a link to use this library without any additional dependencies. You can also include the GDI or WPF project as a reference.
 - Nuget (coming soon)

### Known Limitations / Issues
 - Bitmap data will be translated to sRGB if calibration data and/or a embedded ICC color profile exists in the bitmap file. This was done by design, because color profile support in WPF and GDI is limited. This library does not support writing a color profile.
 - Not all pixel formats are supported natively, some are converted to Bgra32 when parsing
 - Does not support Gray colorspace bitmaps

### Example
There is a convenient to use wrapper for both GDI and WPF, depending on your choice of UI technology.
```cs
byte[] imageBytes = File.ReadAllBytes("file.bmp");
BitmapSource bitmap = BitmapWpf.FromBytes(imageBytes, BitmapWpfReaderFlags.PreserveInvalidAlphaChannel);

// profit!

byte[] imageBytes2 = BitmapWpf.ToBytes(bitmap);
```

### Compatibility

 - :heavy_check_mark: Files with or without a `BITMAPFILEHEADER`
 - :heavy_check_mark: Files with any known BMP header format, including WindowsV1, V2, V3, V4, V5, OS2v1, OS2v2
 - :heavy_check_mark: Files with logical color space / calibration data
 - :heavy_check_mark: Files with embedded ICC color formats
 - :heavy_check_mark: Files with any valid compression format, including RGB, BITFIELDS, ALPHABITFIELDS, JPEG, PNG, HUFFMAN1D (G31D), RLE4/8/24
 - :heavy_check_mark: Files with completely non-standard pixel layout (for example, 32bpp with the following layout: 7B-25G-0R-0A)
