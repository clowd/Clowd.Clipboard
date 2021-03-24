using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader
{
    // this class exists separately so it can be included as a submodule/file in ClipboardGapWpf and not create conflicts upstream - rather than including as a project.
    internal class BitmapWpfInternal
    {
        public unsafe static BitmapFrame Read(ref BITMAP_READ_DETAILS info, byte* data, int dataLength, bool preserveAlpha, bool strictPreserveFormat, bool forceBgra32)
        {
            byte* pixels = data + info.imgDataOffset;

            // we do this parsing here since BitmapCore has no references to PresentationCore
            if (info.compression == BitmapCompressionMode.BI_PNG)
            {
                var stream = new PointerStream(pixels, info.imgDataSize);
                var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return png.Frames[0];
            }
            else if (info.compression == BitmapCompressionMode.BI_JPEG)
            {
                var stream = new PointerStream(pixels, info.imgDataSize);
                var jpg = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return jpg.Frames[0];
            }

            PixelFormat wpfFmt = PixelFormats.Bgra32;
            BitmapCorePixelFormat coreFmt = BitmapCorePixelFormat.Bgra32;

            bool sourceMatch = false;
            if (!forceBgra32 && info.imgSourceFmt != null)
            {
                var origFmt = info.imgSourceFmt;
                var pxarr = Formats.Where(m => m.coreFmt == origFmt).ToArray();
                if (pxarr.Length > 0)
                {
                    var px = pxarr.First();
                    wpfFmt = px.wpfFmt;
                    coreFmt = px.coreFmt;
                    sourceMatch = true;
                }
            }

            BitmapPalette palette = null;
            if (info.imgColorTable.Length > 0)
            {
                var clrs = info.imgColorTable.Select(c => Color.FromRgb(c.rgbRed, c.rgbGreen, c.rgbBlue));
                if (info.imgColorTable.Length > 256) // wpf throws on oversized palettes
                    clrs = clrs.Take(256);
                palette = new BitmapPalette(clrs.ToList());
            }

            if (!sourceMatch && strictPreserveFormat)
                throw new NotSupportedException("The pixel format of this bitmap is not supported, and StrictPreserveOriginalFormat is set.");

            var bitmap = new WriteableBitmap(
                info.imgWidth,
                info.imgHeight,
                info.dpiX,
                info.dpiY,
                wpfFmt,
                palette);

            var buf = (byte*)bitmap.BackBuffer;

            bitmap.Lock();

            BitmapCore.ReadPixels(ref info, coreFmt, pixels, buf, preserveAlpha);

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, info.imgWidth, info.imgHeight));
            bitmap.Unlock();
            bitmap.Freeze(); // dispose back buffer

            return BitmapFrame.Create(bitmap);
        }

        private struct PxMap
        {
            public PixelFormat wpfFmt;
            public BitmapCorePixelFormat coreFmt;

            public PxMap(PixelFormat wpf, BitmapCorePixelFormat core)
            {
                wpfFmt = wpf;
                coreFmt = core;
            }
        }

        private static PxMap[] Formats = new PxMap[]
        {
            new PxMap(PixelFormats.Bgra32, BitmapCorePixelFormat.Bgra32),
            new PxMap(PixelFormats.Rgb24, BitmapCorePixelFormat.Rgb24),
            new PxMap(PixelFormats.Bgr24, BitmapCorePixelFormat.Bgr24),
            new PxMap(PixelFormats.Bgr555, BitmapCorePixelFormat.Bgr555X),
            new PxMap(PixelFormats.Bgr565, BitmapCorePixelFormat.Bgr565),
            new PxMap(PixelFormats.Indexed8, BitmapCorePixelFormat.Indexed8),
            new PxMap(PixelFormats.Indexed4, BitmapCorePixelFormat.Indexed4),
            new PxMap(PixelFormats.Indexed2, BitmapCorePixelFormat.Indexed2),
            new PxMap(PixelFormats.Indexed1, BitmapCorePixelFormat.Indexed1),
        };

        public static unsafe byte[] GetBytes(BitmapFrame bitmap, bool inclFileHeader, bool forceV5, bool forceInfo)
        {
            uint stride = BitmapCore.calc_stride((ushort)bitmap.Format.BitsPerPixel, bitmap.PixelWidth);

            byte[] buffer = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(buffer, (int)stride, 0);

            var clrs = bitmap.Palette == null ? null : bitmap.Palette.Colors.Select(c => new RGBQUAD { rgbRed = c.R, rgbBlue = c.B, rgbGreen = c.G }).ToArray();

            var htype = BitmapCoreHeaderType.BestFit;
            if (forceV5) htype = BitmapCoreHeaderType.ForceV5;
            else if (forceInfo) htype = BitmapCoreHeaderType.ForceVINFO;

            BITMAP_WRITE_REQUEST req = new BITMAP_WRITE_REQUEST
            {
                dpiX = bitmap.DpiX,
                dpiY = bitmap.DpiY,
                imgWidth = bitmap.PixelWidth,
                imgHeight = bitmap.PixelHeight,
                imgStride = stride,
                imgTopDown = true,
                imgColorTable = clrs,
                headerIncludeFile = inclFileHeader,
                iccEmbed = false,
                iccProfileData = null,
                headerType = htype,
            };

            BITMASKS masks = default;

            uint getBitmask(IList<byte> mask)
            {
                uint result = 0;
                int shift = 0;
                for (int i = 0; i < mask.Count; i++)
                {
                    result = result | (uint)(mask[i] << shift);
                    shift += 8;
                }
                return result;
            }

            if (bitmap.Format.Masks != null && bitmap.Format.Masks.Count == 3)
            {
                var wpfmasks = bitmap.Format.Masks;
                masks.maskBlue = getBitmask(wpfmasks[0].Mask);
                masks.maskGreen = getBitmask(wpfmasks[1].Mask);
                masks.maskRed = getBitmask(wpfmasks[2].Mask);
            }
            else if (bitmap.Format.Masks != null && bitmap.Format.Masks.Count == 4)
            {
                var wpfmasks = bitmap.Format.Masks;
                masks.maskBlue = getBitmask(wpfmasks[0].Mask);
                masks.maskGreen = getBitmask(wpfmasks[1].Mask);
                masks.maskRed = getBitmask(wpfmasks[2].Mask);
                masks.maskAlpha = getBitmask(wpfmasks[3].Mask);
            }

            fixed (byte* ptr = buffer)
                return BitmapCore.WriteToBMP(ref req, ptr, masks, (ushort)bitmap.Format.BitsPerPixel);
        }

        private class BitmapWpfColorManagement
        {
            [DllImport("WindowsCodecs", EntryPoint = "WICCreateImagingFactory_Proxy")]
            private static extern int CreateImagingFactory(UInt32 SDKVersion, out IntPtr ppICodecFactory);

            [DllImport("WindowsCodecs", EntryPoint = "WICCreateColorContext_Proxy")]
            private static extern int /* HRESULT */ CreateColorContext(IntPtr pICodecFactory, out IntPtr /* IWICColorContext */ ppColorContext);

            [DllImport("WindowsCodecs", EntryPoint = "IWICColorContext_InitializeFromMemory_Proxy")]
            private unsafe static extern int /* HRESULT */ InitializeFromMemory(IntPtr THIS_PTR, void* pbBuffer, uint cbBufferSize);

            private const int WINCODEC_SDK_VERSION = 0x0236;

            public unsafe static ColorContext GetWpfColorContext(void* profilePtr, uint profileSize)
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

                        var colorContextType = typeof(ColorContext);
                        var milHandleType = colorContextType.Assembly.GetType("System.Windows.Media.SafeMILHandle");

                        var milHandle = Activator.CreateInstance(milHandleType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { colorContextPtr }, null);
                        var colorContext = Activator.CreateInstance(colorContextType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { milHandle }, null);

                        return (ColorContext)colorContext;
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
}
