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
            if (stream is MemoryStream mem)
            {
                return Read(mem.GetBuffer(), colorOptions, pFlags);
            }
            else
            {
                byte[] buffer = new byte[4096];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                            return Read(ms.GetBuffer(), colorOptions, pFlags);
                        ms.Write(buffer, 0, read);
                    }
                }
            }
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
            BITMAP_DETAILS info;

            BitmapCore.GetInfo(data, dataLength, out info);

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

            var bitmap = new WriteableBitmap(
                info.imgWidth,
                info.imgHeight,
                info.dpiX,
                info.dpiY,
                System.Windows.Media.PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);

            var buf = (byte*)bitmap.BackBuffer;

            bitmap.Lock();
            var preserveAlpha = (pFlags & ParserFlags.PreserveInvalidAlphaChannel) > 0;
            BitmapCore.ReadPixelsToBGRA8(ref info, (data + info.imgDataOffset), buf, preserveAlpha);

            bool transformed = false;

            if (colorOptions == CalibrationOptions.FlattenTo_sRGB)
            {
                // ColorConvertedBitmap fallback ?
                lcms.TransformBGRA8(ref info, data, buf, bitmap.BackBufferStride);
                transformed = true;
            }
            else if (colorOptions == CalibrationOptions.TryBestEffort)
            {
                try
                {
                    lcms.TransformBGRA8(ref info, data, buf, bitmap.BackBufferStride);
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
    }
}
