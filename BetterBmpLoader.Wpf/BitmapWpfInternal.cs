using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace BetterBmpLoader.Wpf
{
    // this class exists separately so it can be included as a submodule/file in ClipboardGapWpf and not create conflicts upstream - rather than including as a project.
    internal class BitmapWpfInternal
    {
        internal unsafe static BitmapFrame Read(ref BITMAP_READ_DETAILS info, byte* data, int dataLength, bool preserveAlpha)
        {
            // we do this parsing here since BitmapCore has no references to PresentationCore
            var size = info.imgDataSize != 0 ? info.imgDataSize : (uint)dataLength;
            if (info.compression == BitmapCompressionMode.BI_PNG)
            {
                var stream = new PointerStream(data, size);
                var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return png.Frames[0];
            }
            else if (info.compression == BitmapCompressionMode.BI_JPEG)
            {
                var stream = new PointerStream(data, size);
                var jpg = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return jpg.Frames[0];
            }

            BitmapPalette palette = null;
            if (info.imgColorTable.Length > 0)
            {
                var clrs = info.imgColorTable.Select(c => System.Windows.Media.Color.FromRgb(c.rgbRed, c.rgbGreen, c.rgbBlue));
                if (info.imgColorTable.Length > 256) // wpf throws on oversized palettes
                    clrs = clrs.Take(256);
                palette = new BitmapPalette(clrs.ToList());
            }

            // defaults
            System.Windows.Media.PixelFormat wpfFmt = System.Windows.Media.PixelFormats.Bgra32;
            BitmapCorePixelFormat2 coreFmt = BitmapCorePixelFormat2.Bgra32;

            var origFmt = info.imgFmt;
            if (origFmt != null)
            {
                var pxarr = Formats.Where(m => m.coreFmt == origFmt).ToArray();
                if (pxarr.Length > 0)
                {
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

            BitmapCore.ReadPixels(ref info, coreFmt, (data + info.imgDataOffset), buf, preserveAlpha);

#if EXPERIMENTAL_CMM
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
#endif

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, info.imgWidth, info.imgHeight));
            bitmap.Unlock();
            bitmap.Freeze(); // dispose back buffer

#if EXPERIMENTAL_CMM

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
#endif

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
            //new PxMap(System.Windows.Media.PixelFormats.Bgr32, BitmapCorePixelFormat2.Bgra32),
        };

        public static unsafe byte[] GetBytes(BitmapFrame bitmap, bool inclFileHeader, bool forceV5, bool forceInfo)
        {
            int stride = (bitmap.Format.BitsPerPixel * bitmap.PixelWidth + 31) / 32 * 4;

            byte[] buffer = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(buffer, stride, 0);

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
                imgStride = (uint)stride,
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
                return BitmapCore.WriteToBMP(ref req, ptr, masks, (ushort)bitmap.Format.BitsPerPixel, 0);

#if EXPERIMENTAL_CMM
            //var pxarr = Formats.Where(m => m.wpfFmt == bitmap.Format).ToArray();
            //if (pxarr.Length == 0)
            //    throw new NotSupportedException($"Pixel format '{bitmap.Format.ToString()}' not supported.");

            //var px = pxarr.First();

            byte[] ctxBytes = null;

            if (bitmap.ColorContexts != null && bitmap.ColorContexts.Any())
            {
                var ctx = bitmap.ColorContexts.First();
                ctxBytes = StructUtil.ReadBytes(ctx.OpenProfileStream());
            }

            fixed (byte* ptr = buffer)
                return BitmapCore.WriteToBMP(ref req, true, ptr, ctxBytes, true);
#endif
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

        private class BitmapWpfColorManagement
        {
            [DllImport("WindowsCodecs", EntryPoint = "WICCreateImagingFactory_Proxy")]
            private static extern int CreateImagingFactory(UInt32 SDKVersion, out IntPtr ppICodecFactory);

            [DllImport("WindowsCodecs", EntryPoint = "WICCreateColorContext_Proxy")]
            private static extern int /* HRESULT */ CreateColorContext(IntPtr pICodecFactory, out IntPtr /* IWICColorContext */ ppColorContext);

            [DllImport("WindowsCodecs", EntryPoint = "IWICColorContext_InitializeFromMemory_Proxy")]
            private unsafe static extern int /* HRESULT */ InitializeFromMemory(IntPtr THIS_PTR, void* pbBuffer, uint cbBufferSize);

            private const int WINCODEC_SDK_VERSION = 0x0236;

            public unsafe static System.Windows.Media.ColorContext GetWpfColorContext(void* profilePtr, uint profileSize)
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
}
