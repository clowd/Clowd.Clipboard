using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BetterBmpLoader
{
    internal static class Lcms
    {
        private enum Intent : uint
        {
            Perceptual = 0,
            RelativeColorimetric = 1,
            Saturation = 2,
            AbsoluteColorimetric = 3,
        }

        [Flags]
        private enum PixelType : uint
        {
            Any = 0,    // Don't check colorspace
                        // Enumeration values 1 & 2 are reserved
            Gray = 3,
            RGB = 4,
            CMY = 5,
            CMYK = 6,
            YCbCr = 7,
            YUV = 8,    // Lu'v'
            XYZ = 9,
            Lab = 10,
            YUVK = 11,  // Lu'v'K
            HSV = 12,
            HLS = 13,
            Yxy = 14,
            MCH1 = 15,
            MCH2 = 16,
            MCH3 = 17,
            MCH4 = 18,
            MCH5 = 19,
            MCH6 = 20,
            MCH7 = 21,
            MCH8 = 22,
            MCH9 = 23,
            MCH10 = 24,
            MCH11 = 25,
            MCH12 = 26,
            MCH13 = 27,
            MCH14 = 28,
            MCH15 = 29,
            LabV2 = 30
        }

        private static uint FLOAT_SH(uint s) { return s << 22; }
        private static uint OPTIMIZED_SH(uint s) { return s << 21; }
        private static uint COLORSPACE_SH(PixelType s) { return Convert.ToUInt32(s) << 16; }
        private static uint SWAPFIRST_SH(uint s) { return s << 14; }
        private static uint FLAVOR_SH(uint s) { return s << 13; }
        private static uint PLANAR_SH(uint s) { return s << 12; }
        private static uint ENDIAN16_SH(uint s) { return s << 11; }
        private static uint DOSWAP_SH(uint s) { return s << 10; }
        private static uint EXTRA_SH(uint s) { return s << 7; }
        private static uint CHANNELS_SH(uint s) { return s << 3; }
        private static uint BYTES_SH(uint s) { return s; }

        public static readonly uint TYPE_GRAY_8
                = COLORSPACE_SH(PixelType.Gray) | CHANNELS_SH(1) | BYTES_SH(1);
        public static readonly uint TYPE_GRAY_8_REV
                = COLORSPACE_SH(PixelType.Gray) | CHANNELS_SH(1) | BYTES_SH(1) | FLAVOR_SH(1);
        public static readonly uint TYPE_GRAY_16
                = COLORSPACE_SH(PixelType.Gray) | CHANNELS_SH(1) | BYTES_SH(2);
        public static readonly uint TYPE_GRAY_16_REV
                = COLORSPACE_SH(PixelType.Gray) | CHANNELS_SH(1) | BYTES_SH(2) | FLAVOR_SH(1);
        public static readonly uint TYPE_GRAY_16_SE
                = COLORSPACE_SH(PixelType.Gray) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1);
        public static readonly uint TYPE_GRAYA_8
                = COLORSPACE_SH(PixelType.Gray) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1);
        public static readonly uint TYPE_GRAYA_16
                = COLORSPACE_SH(PixelType.Gray) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2);
        public static readonly uint TYPE_GRAYA_16_SE
                = COLORSPACE_SH(PixelType.Gray) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | ENDIAN16_SH(1);
        public static readonly uint TYPE_GRAYA_8_PLANAR
                = COLORSPACE_SH(PixelType.Gray) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_GRAYA_16_PLANAR
                = COLORSPACE_SH(PixelType.Gray) | EXTRA_SH(1) | CHANNELS_SH(1) | BYTES_SH(2) | PLANAR_SH(1);

        public static readonly uint TYPE_RGB_8
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(1);
        public static readonly uint TYPE_RGB_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_BGR_8
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1);
        public static readonly uint TYPE_BGR_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_RGB_16
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2);
        public static readonly uint TYPE_RGB_16_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
        public static readonly uint TYPE_RGB_16_SE
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);
        public static readonly uint TYPE_BGR_16
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);
        public static readonly uint TYPE_BGR_16_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_BGR_16_SE
                = COLORSPACE_SH(PixelType.RGB) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

        public static readonly uint TYPE_RGBA_8
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1);
        public static readonly uint TYPE_RGBA_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_RGBA_16
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2);
        public static readonly uint TYPE_RGBA_16_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | PLANAR_SH(1);
        public static readonly uint TYPE_RGBA_16_SE
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | ENDIAN16_SH(1);

        public static readonly uint TYPE_ARGB_8
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | SWAPFIRST_SH(1);
        public static readonly uint TYPE_ARGB_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | SWAPFIRST_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_ARGB_16
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | SWAPFIRST_SH(1);

        public static readonly uint TYPE_ABGR_8
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1);
        public static readonly uint TYPE_ABGR_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_ABGR_16
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1);
        public static readonly uint TYPE_ABGR_16_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_ABGR_16_SE
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | ENDIAN16_SH(1);

        public static readonly uint TYPE_BGRA_8
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
        public static readonly uint TYPE_BGRA_8_PLANAR
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | PLANAR_SH(1);
        public static readonly uint TYPE_BGRA_16
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1);
        public static readonly uint TYPE_BGRA_16_SE
                = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(2) | DOSWAP_SH(1) | SWAPFIRST_SH(1) | ENDIAN16_SH(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct CIExyY
        {
            [MarshalAs(UnmanagedType.R8)]
            public double x;
            [MarshalAs(UnmanagedType.R8)]
            public double y;
            [MarshalAs(UnmanagedType.R8)]
            public double Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIExyYTRIPLE
        {
            public CIExyY Red;
            public CIExyY Green;
            public CIExyY Blue;

            public static CIExyYTRIPLE FromHandle(IntPtr handle)
            {
                return Marshal.PtrToStructure<CIExyYTRIPLE>(handle);
            }
        }

        private const string Liblcms = "lcms2";

        [DllImport(Liblcms, EntryPoint = "cmsDoTransformLineStride", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern void DoTransformLineStride(
            IntPtr transform,
            void* inputBuffer,
            void* outputBuffer,
            [MarshalAs(UnmanagedType.U4)] int pixelsPerLine,
            [MarshalAs(UnmanagedType.U4)] int lineCount,
            [MarshalAs(UnmanagedType.U4)] int bytesPerLineIn,
            [MarshalAs(UnmanagedType.U4)] int bytesPerLineOut,
            [MarshalAs(UnmanagedType.U4)] int bytesPerPlaneIn,
            [MarshalAs(UnmanagedType.U4)] int bytesPerPlaneOut);

        [DllImport(Liblcms, EntryPoint = "cmsCreateTransform", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateTransform(
            IntPtr inputProfile,
            [MarshalAs(UnmanagedType.U4)] uint inputFormat,
            IntPtr outputProfile,
            [MarshalAs(UnmanagedType.U4)] uint outputFormat,
            [MarshalAs(UnmanagedType.U4)] uint intent,
            [MarshalAs(UnmanagedType.U4)] uint flags);

        [DllImport(Liblcms, EntryPoint = "cmsCreate_sRGBProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr Create_sRGBProfile();

        [DllImport(Liblcms, EntryPoint = "cmsCreateRGBProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateRGBProfile(
            in CIExyY whitePoint,
            in CIExyYTRIPLE primaries,
            IntPtr[] transferFunction);

        [DllImport(Liblcms, EntryPoint = "cmsCloseProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern int CloseProfile(IntPtr handle);

        [DllImport(Liblcms, EntryPoint = "cmsOpenProfileFromMem", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern IntPtr OpenProfileFromMem(/*const*/ void* memPtr, [MarshalAs(UnmanagedType.U4)] int memSize);

        [DllImport(Liblcms, EntryPoint = "cmsDeleteTransform", CallingConvention = CallingConvention.StdCall)]
        private static extern void DeleteTransform(IntPtr transform);

        [DllImport(Liblcms, EntryPoint = "cmsBuildGamma", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr BuildGammaCurve(IntPtr handle, [MarshalAs(UnmanagedType.R8)] double gamma);

        [DllImport(Liblcms, EntryPoint = "cmsFreeToneCurve", CallingConvention = CallingConvention.StdCall)]
        private static extern void FreeToneCurve(IntPtr handle);

        public static bool CheckLibAvailble()
        {
            try
            {
                DeleteTransform(IntPtr.Zero);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        private static CIExyYTRIPLE GetPrimariesFromBMPEndpoints(uint red_x, uint red_y, uint green_x, uint green_y, uint blue_x, uint blue_y)
        {
            uint[] chk = new uint[] { red_x, red_y, 1, green_x, green_y, 1, blue_x, blue_y, 1 };
            var invalid = chk.Where(c => c == 0).ToArray();
            if (invalid.Length > 0)
                throw new NotSupportedException("Bitmap with LCS_CALIBRATED_RGB must explicitly set RGB endpoints >0 in header. Values were: " + String.Join(", ", chk));

            double fxpt2dot30_to_float(uint fxpt2dot30) => fxpt2dot30 * 9.31322574615478515625e-10f;
            double rx = fxpt2dot30_to_float(red_x);
            double ry = fxpt2dot30_to_float(red_y);
            double gx = fxpt2dot30_to_float(green_x);
            double gy = fxpt2dot30_to_float(green_y);
            double bx = fxpt2dot30_to_float(blue_x);
            double by = fxpt2dot30_to_float(blue_y);

            return new CIExyYTRIPLE
            {
                Red = new CIExyY { x = rx, y = ry, Y = 1 },
                Green = new CIExyY { x = gx, y = gy, Y = 1 },
                Blue = new CIExyY { x = bx, y = by, Y = 1 }
            };
        }

        private static CIExyY GetWhitePoint_sRGB()
        {
            double kD65x = 0.31271;
            double kD65y = 0.32902;
            return new CIExyY { x = kD65x, y = kD65y, Y = 1, };
        }

        private static Intent ConvertIntent(Bv5Intent iccProfileIntent)
        {
            Intent lcmsIntent;
            switch (iccProfileIntent)
            {
                case Bv5Intent.LCS_GM_BUSINESS: lcmsIntent = Intent.Saturation; break;
                case Bv5Intent.LCS_GM_GRAPHICS: lcmsIntent = Intent.RelativeColorimetric; break;
                case Bv5Intent.LCS_GM_IMAGES: lcmsIntent = Intent.Perceptual; break;
                case Bv5Intent.LCS_GM_ABS_COLORIMETRIC: lcmsIntent = Intent.AbsoluteColorimetric; break;
                default: throw new ArgumentOutOfRangeException(nameof(iccProfileIntent));
            }
            return lcmsIntent;
        }

        public unsafe static void TransformBGRA8(ref BITMAP_READ_DETAILS info, byte* source, byte* dataBuffer, int dataStride)
        {
            if (info.iccProfileType == ColorSpaceType.LCS_sRGB || info.iccProfileType == ColorSpaceType.LCS_WINDOWS_COLOR_SPACE)
                return; // do nothing


            if (info.iccProfileType == ColorSpaceType.LCS_CALIBRATED_RGB)
            {
                var primaries = Lcms.GetPrimariesFromBMPEndpoints(
                      info.dibHeader.bV5Endpoints_1x, info.dibHeader.bV5Endpoints_1y,
                      info.dibHeader.bV5Endpoints_2x, info.dibHeader.bV5Endpoints_2y,
                      info.dibHeader.bV5Endpoints_3x, info.dibHeader.bV5Endpoints_3y);

                var whitepoint = Lcms.GetWhitePoint_sRGB();
                var lcmsIntent = ConvertIntent(info.iccProfileIntent);

                TransformCalibratedBGRA8(
                    primaries, whitepoint,
                    info.dibHeader.bV5GammaRed, info.dibHeader.bV5GammaGreen, info.dibHeader.bV5GammaBlue,
                    dataBuffer, info.imgWidth, info.imgHeight, dataStride, lcmsIntent);
            }
            else if (info.iccProfileType == ColorSpaceType.PROFILE_EMBEDDED)
            {
                TransformEmbeddedPixelFormat(TYPE_BGRA_8, (source + info.iccProfileOffset), info.iccProfileSize, dataBuffer, info.imgWidth, info.imgHeight, dataStride, info.iccProfileIntent);
            }
            else
            {
                throw new NotSupportedException(info.iccProfileType.ToString());
            }
        }

        public unsafe static void TransformEmbeddedPixelFormat(uint pxFormat, void* profilePtr, uint profileSize, void* data, int width, int height, int stride, Bv5Intent bvintent)
        {
            var intent = ConvertIntent(bvintent);

            var source = OpenProfileFromMem(profilePtr, (int)profileSize);
            var target = Create_sRGBProfile();
            var transform = CreateTransform(source, pxFormat, target, pxFormat, (uint)intent, 0);

            try
            {
                if (source == IntPtr.Zero)
                    throw new Exception("Unable to read source color profile.");
                if (target == IntPtr.Zero)
                    throw new Exception("Unable to create target sRGB color profile.");
                if (transform == IntPtr.Zero)
                    throw new Exception("Unable to create color transform.");

                DoTransformLineStride(transform, data, data, width, height, stride, stride, 0, 0);
            }
            finally
            {
                DeleteTransform(transform);
                CloseProfile(target);
                CloseProfile(source);
            }
        }

        private unsafe static void TransformCalibratedBGRA8(CIExyYTRIPLE primaries, CIExyY whitePoint, uint red_gamma, uint green_gamma, uint blue_gamma,
            void* data, int width, int height, int stride, Intent intent)
        {
            // https://github.com/chromium/chromium/blob/99314be8152e688bafbbf9a615536bdbb289ea87/third_party/blink/renderer/platform/image-decoders/bmp/bmp_image_reader.cc#L355
            double SkFixedToFloat(uint z) => ((z) * 1.52587890625e-5f);

            var tr = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(red_gamma));
            var tg = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(green_gamma));
            var tb = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(blue_gamma));
            var source = CreateRGBProfile(whitePoint, primaries, new[] { tr, tg, tb });
            var target = Create_sRGBProfile();
            var transform = CreateTransform(source, TYPE_BGRA_8, target, TYPE_BGRA_8, (uint)intent, 0);

            try
            {
                if (source == IntPtr.Zero)
                    throw new Exception("Unable to read source color profile.");
                if (target == IntPtr.Zero)
                    throw new Exception("Unable to create target sRGB color profile.");
                if (transform == IntPtr.Zero)
                    throw new Exception("Unable to create color transform.");

                DoTransformLineStride(transform, data, data, width, height, stride, stride, 0, 0);
            }
            finally
            {
                DeleteTransform(transform);
                CloseProfile(target);
                CloseProfile(source);
                FreeToneCurve(tr);
                FreeToneCurve(tg);
                FreeToneCurve(tb);
            }
        }
    }
}
