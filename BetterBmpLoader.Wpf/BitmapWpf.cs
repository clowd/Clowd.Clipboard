using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader.Wpf
{
    public enum CalibrationOptions
    {
        /// <summary>
        /// Any embedded color profile or calibration will be ignored completely.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// Recommended: If an embedded color profile or calibration is found, we will try first to convert it to sRGB with lcms2.dll. 
        /// In the event this library can not be found or an error ocurrs, we will attempt to create and return a BitmapFrame with an embedded color profile instead.
        /// If embedding a WPF color profile also fails, we will return a bitmap without any color profile - equivalent to <see cref="CalibrationOptions.Ignore"/>.
        /// </summary>
        TryBestEffort = 1,

        /// <summary>
        /// Not recommended: Attempts to parse an embedded color profile to a WPF color context. This does not support calibrated bitmaps (calibration will be ignored).
        /// Also, WPF ignores color profiles when rendering a BitmapSource, so this should only be used if the intention is to immediately encode the returned BitmapFrame
        /// into another image type, using an encoder such as <see cref="PngBitmapEncoder"/>.
        /// </summary>
        PreserveColorProfile = 2,

        /// <summary>
        /// This will attempt to convert any embedded profile or calibration to sRGB with lcms2.dll, and will throw if an error occurs.
        /// </summary>
        FlattenTo_sRGB = 3,
    }

    [Flags]
    public enum ParserFlags
    {
        /// <summary>
        /// No special parsing flags
        /// </summary>
        None = 0,

        /// <summary>
        /// In windows, many applications create 16 or 32 bpp Bitmaps that indicate they have no transparency, but actually do have transparency.
        /// For example, in a 32bpp RGB encoded bitmap, you'd have the following R8, G8, B8, and the remaining 8 bits are to be ignored / zero.
        /// Sometimes, these bits are not zero, and with this flag set, we will use heuristics to determine if that unused channel contains 
        /// transparency data, and if so, parse it as such.
        /// </summary>
        PreserveInvalidAlphaChannel = 1,
    }

    public sealed class BitmapWpf
    {
        [DllImport("WindowsCodecs", EntryPoint = "WICCreateImagingFactory_Proxy")]
        private static extern int CreateImagingFactory(UInt32 SDKVersion, out IntPtr ppICodecFactory);

        [DllImport("WindowsCodecs", EntryPoint = "WICCreateColorContext_Proxy")]
        private static extern int /* HRESULT */ CreateColorContext(IntPtr pICodecFactory, out IntPtr /* IWICColorContext */ ppColorContext);

        [DllImport("WindowsCodecs", EntryPoint = "IWICColorContext_InitializeFromMemory_Proxy")]
        private unsafe static extern int /* HRESULT */ InitializeFromMemory(IntPtr THIS_PTR, void* pbBuffer, uint cbBufferSize);

        private const int WINCODEC_SDK_VERSION = 0x0236;

        public static BitmapFrame Read(Stream stream) => Read(stream, CalibrationOptions.Ignore);

        public static BitmapFrame Read(Stream stream, CalibrationOptions colorOptions) => Read(stream, colorOptions, ParserFlags.None);

        public static BitmapFrame Read(Stream stream, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            return Read(StructUtil.ReadBytes(stream), colorOptions, pFlags);
        }

        public static BitmapFrame Read(byte[] data) => Read(data, CalibrationOptions.Ignore);

        public static BitmapFrame Read(byte[] data, CalibrationOptions colorOptions) => Read(data, colorOptions, ParserFlags.None);

        public unsafe static BitmapFrame Read(byte[] data, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            fixed (byte* ptr = data)
                return Read(ptr, data.Length, colorOptions, pFlags);
        }

        public unsafe static BitmapFrame Read(byte* data, int dataLength, CalibrationOptions colorOptions, ParserFlags pFlags)
        {
            BITMAP_READ_DETAILS info;

            BitmapCore.ReadHeader(data, dataLength, out info);

            // we do this parsing here since BitmapCore has no references to PresentationCore
            if (info.compression == BitmapCompressionMode.BI_PNG)
            {
                byte[] pngImg = new byte[info.imgDataSize];
                Marshal.Copy((IntPtr)(data + info.imgDataOffset), pngImg, 0, pngImg.Length);
                var png = new PngBitmapDecoder(new MemoryStream(pngImg), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return png.Frames[0];
            }
            else if (info.compression == BitmapCompressionMode.BI_JPEG)
            {
                byte[] jpegImg = new byte[info.imgDataSize];
                Marshal.Copy((IntPtr)(data + info.imgDataOffset), jpegImg, 0, jpegImg.Length);
                var jpg = new JpegBitmapDecoder(new MemoryStream(jpegImg), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return jpg.Frames[0];
            }

            BitmapPalette palette = null;

            if (info.imgColorTable.Length > 0)
            {
                var clrs = info.imgColorTable.Select(c => System.Windows.Media.Color.FromRgb(c.rgbRed, c.rgbGreen, c.rgbBlue));
                if (info.imgColorTable.Length > 256)
                    clrs = clrs.Take(256);
                palette = new BitmapPalette(clrs.ToList());
            }

            // defaults
            System.Windows.Media.PixelFormat wpfFmt = System.Windows.Media.PixelFormats.Bgra32;
            BitmapCorePixelFormat2 coreFmt = BitmapCorePixelFormat2.Bgra32;

            if (info.imgFmt != null)
            {
                var pxarr = Formats.Where(m => m.coreFmt == info.imgFmt).ToArray();
                if (pxarr.Length > 0)
                {
                    if (pxarr.Length == 0)
                        throw new NotSupportedException("Pixel format not supported.");
                    var px = pxarr.First();
                    wpfFmt = px.wpfFmt;
                    coreFmt = px.coreFmt;
                }
            }

            var bitmap = new WriteableBitmap(
                info.imgWidth,
                info.imgHeight,
                info.dpiX,
                info.dpiY,
                wpfFmt,
                palette);

            var buf = (byte*)bitmap.BackBuffer;

            bitmap.Lock();
            var preserveAlpha = (pFlags & ParserFlags.PreserveInvalidAlphaChannel) > 0;

            BitmapCore.ReadPixels(ref info, coreFmt, (data + info.imgDataOffset), buf, preserveAlpha);

            bool transformed = false;

            if (colorOptions == CalibrationOptions.FlattenTo_sRGB)
            {
                // ColorConvertedBitmap fallback ?
                Lcms.TransformBGRA8(ref info, data, buf, bitmap.BackBufferStride);
                transformed = true;
            }
            else if (colorOptions == CalibrationOptions.TryBestEffort)
            {
                try
                {
                    Lcms.TransformBGRA8(ref info, data, buf, bitmap.BackBufferStride);
                    transformed = true;
                }
                catch
                {
                    // if lib not available, we just return color profile via wpf below
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, info.imgWidth, info.imgHeight));
            bitmap.Unlock();
            bitmap.Freeze(); // dispose back buffer

            if (info.iccProfileType == ColorSpaceType.PROFILE_EMBEDDED)
            {
                if ((!transformed && colorOptions == CalibrationOptions.TryBestEffort) || colorOptions == CalibrationOptions.PreserveColorProfile)
                {
                    try
                    {
                        var profile = GetWpfColorContext((data + info.iccProfileOffset), info.iccProfileSize);
                        var bl = new System.Windows.Media.ColorContext[] { profile }.ToList();
                        return BitmapFrame.Create(bitmap, null, null, new System.Collections.ObjectModel.ReadOnlyCollection<System.Windows.Media.ColorContext>(bl));
                    }
                    catch
                    {
                        if (colorOptions != CalibrationOptions.TryBestEffort)
                            throw;
                    }
                }
            }

            return BitmapFrame.Create(bitmap);
        }

        private static PxMap[] Formats = new PxMap[]
        {
            //new PxMap(System.Windows.Media.PixelFormats.Bgra32, BitmapCorePixelFormat2.Bgr5551, BitmapCorePixelFormat2.Bgra32),
            new PxMap(System.Windows.Media.PixelFormats.Bgra32, BitmapCorePixelFormat2.Bgra32),
            new PxMap(System.Windows.Media.PixelFormats.Rgb24, BitmapCorePixelFormat2.Rgb24),
            new PxMap(System.Windows.Media.PixelFormats.Bgr24, BitmapCorePixelFormat2.Bgr24),
            new PxMap(System.Windows.Media.PixelFormats.Bgr555, BitmapCorePixelFormat2.Bgr555X1),
            new PxMap(System.Windows.Media.PixelFormats.Bgr565, BitmapCorePixelFormat2.Bgr565),
            new PxMap(System.Windows.Media.PixelFormats.Indexed8, BitmapCorePixelFormat2.Indexed8),
            new PxMap(System.Windows.Media.PixelFormats.Indexed4, BitmapCorePixelFormat2.Indexed4),
            new PxMap(System.Windows.Media.PixelFormats.Indexed2, BitmapCorePixelFormat2.Indexed2),
            new PxMap(System.Windows.Media.PixelFormats.Indexed1, BitmapCorePixelFormat2.Indexed1),

            // this is at the end so it will never be used when parsing, only when writing
            new PxMap(System.Windows.Media.PixelFormats.Bgr32, BitmapCorePixelFormat2.Bgra32),
        };

        public static unsafe byte[] GetBytes(BitmapFrame bitmap)
        {
            var pxarr = Formats.Where(m => m.wpfFmt == bitmap.Format).ToArray();
            if (pxarr.Length == 0)
                throw new NotSupportedException($"Pixel format '{bitmap.Format.ToString()}' not supported.");

            var px = pxarr.First();
            int stride = (px.coreFmt.BitsPerPixel * bitmap.PixelWidth + 31) / 32 * 4;

            byte[] buffer = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(buffer, stride, 0);

            var clrs = bitmap.Palette == null ? null : bitmap.Palette.Colors.Select(c => new RGBQUAD { rgbRed = c.R, rgbBlue = c.B, rgbGreen = c.G }).ToArray();

            BITMAP_WRITE_REQUEST req = new BITMAP_WRITE_REQUEST
            {
                dpiX = bitmap.DpiX,
                dpiY = bitmap.DpiY,
                imgWidth = bitmap.PixelWidth,
                imgHeight = bitmap.PixelHeight,
                imgStride = (uint)stride,
                imgTopDown = true,
                imgColorTable = clrs,
                fmt = px.coreFmt,
            };

            byte[] ctxBytes = null;

            if (bitmap.ColorContexts != null && bitmap.ColorContexts.Any())
            {
                var ctx = bitmap.ColorContexts.First();
                ctxBytes = StructUtil.ReadBytes(ctx.OpenProfileStream());
            }

            fixed (byte* ptr = buffer)
                return BitmapCore.WriteToBMP(ref req, true, ptr, ctxBytes, true);
        }

        private unsafe static System.Windows.Media.ColorContext GetWpfColorContext(void* profilePtr, uint profileSize)
        {
            IntPtr factoryPtr, colorContextPtr;

            var hr = CreateImagingFactory(WINCODEC_SDK_VERSION, out factoryPtr);
            if (hr != 0) throw new Win32Exception(hr);

            try
            {
                hr = CreateColorContext(factoryPtr, out colorContextPtr);
                if (hr != 0) throw new Win32Exception(hr);

                try
                {
                    hr = InitializeFromMemory(colorContextPtr, profilePtr, profileSize);
                    if (hr != 0) throw new Win32Exception(hr);

                    var colorContextType = typeof(System.Windows.Media.ColorContext);
                    var milHandleType = colorContextType.Assembly.GetType("System.Windows.Media.SafeMILHandle");

                    var milHandle = Activator.CreateInstance(milHandleType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { colorContextPtr }, null);
                    var colorContext = Activator.CreateInstance(colorContextType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { milHandle }, null);

                    return (System.Windows.Media.ColorContext)colorContext;
                }
                catch
                {
                    // Only free colorContextPtr if there is an error. Otherwise, it will be freed by WPF
                    Marshal.Release(colorContextPtr);
                    throw;
                }
            }
            finally
            {
                Marshal.Release(factoryPtr);
            }
        }

        private struct PxMap
        {
            public System.Windows.Media.PixelFormat wpfFmt;
            public BitmapCorePixelFormat2 coreFmt;

            public PxMap(System.Windows.Media.PixelFormat wpf, BitmapCorePixelFormat2 core)
            {
                wpfFmt = wpf;
                coreFmt = core;
            }
        }
    }
}
