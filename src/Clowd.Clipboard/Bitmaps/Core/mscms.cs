using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Clowd.Clipboard.Bitmaps.Core;

namespace Clowd.Clipboard.Bitmaps.Core;

#if NET5_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
internal class mscms
{
    // https://docs.microsoft.com/en-us/windows/win32/api/icm/
    private const string libmscms = "mscms";

    //private const uint LCS_SIGNATURE = 0x50534f43;
    //private const UInt32 PROOF_MODE = 0x00000001;
    //private const UInt32 NORMAL_MODE = 0x00000002;
    private const UInt32 BEST_MODE = 0x00000003;
    //private const UInt32 ENABLE_GAMUT_CHECKING = 0x00010000;
    private const UInt32 USE_RELATIVE_COLORIMETRIC = 0x00020000;
    //private const UInt32 FAST_TRANSLATE = 0x00040000;

    [DllImport(libmscms)]
    private static extern SafeTransformHandle CreateMultiProfileTransform(IntPtr[] /* PHPROFILE */ pahProfiles, uint nProfiles, uint[] padwIntent, uint nIntents, uint dwFlags, uint indexPreferredCMM);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool DeleteColorTransform(IntPtr hColorTransform);

    [DllImport(libmscms, SetLastError = true)]
    private static extern unsafe bool TranslateBitmapBits(SafeTransformHandle hTransform, void* pSrcBits, mscmsPxFormat bmFormat, uint dwWidth, uint dwHeight, uint dwInputStride, void* pDestBits, mscmsPxFormat bmOutput, uint dwOutputStride, IntPtr pfnCallBack, IntPtr lParam);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool CloseColorProfile(IntPtr hHandle);

    [DllImport(libmscms, SetLastError = true)]
    private static extern SafeProfileHandle OpenColorProfile(ref PROFILE pProfile, uint dwDesiredAccess, uint dwShareMode, uint dwCreationMode);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool CreateProfileFromLogColorSpace(ref LOGCOLORSPACE pLogColorSpace, out IntPtr pProfile);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool GetStandardColorSpaceProfile(IntPtr pNull, ColorSpaceType dwSCS, StringBuilder pProfileName, out int pdwSize);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool GetColorDirectory(IntPtr pNull, StringBuilder pBuffer, out int pdwSize);

    [DllImport(libmscms, SetLastError = true)]
    private static extern bool TranslateColors(SafeTransformHandle hTransform, IntPtr paInputColors, uint nColors, COLOR_TYPE ctInput, IntPtr paOutputColors, COLOR_TYPE ctOutput);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    private static extern int GlobalSize(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    private static extern IntPtr GlobalFree(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    private static extern bool GlobalUnlock(IntPtr handle);

    public static unsafe RGBQUAD[] TransformColorsTo_sRGB(SafeProfileHandle source, RGBQUAD[] color, mscmsIntent dwIntent)
    {
        var colorSize = Environment.Is64BitProcess ? 16 : 8;
        var size = colorSize * color.Length;

        // https://docs.microsoft.com/en-us/windows/win32/api/icm/nf-icm-translatecolors
        // https://docs.microsoft.com/en-us/windows/win32/api/icm/ns-icm-color

        IntPtr paInputColors = Marshal.AllocHGlobal(size);
        IntPtr paOutputColors = Marshal.AllocHGlobal(size);

        const double cmax = 255d;
        const double nmax = 0xFFFF;
        const ushort nmask = 0xFFFF;

        try
        {
            var inputPtr = (byte*)paInputColors;
            foreach (var c in color)
            {
                var nclr = (long)(c.rgbRed / cmax * nmax) | ((long)(c.rgbGreen / cmax * nmax) << 16) | ((long)(c.rgbBlue / cmax * nmax) << 32);
                *((long*)inputPtr) = nclr;
                inputPtr += colorSize;
            }

            using (var dest = OpenProfile_sRGB())
            using (var transform = CreateTransform(source, dest, dwIntent))
            {
                var success = TranslateColors(transform, paInputColors, (uint)color.Length, COLOR_TYPE.COLOR_3_CHANNEL, paOutputColors, COLOR_TYPE.COLOR_3_CHANNEL);
                if (!success)
                    throw new Win32Exception();

                var outputPtr = (byte*)paOutputColors;
                var output = new RGBQUAD[color.Length];
                for (int i = 0; i < color.Length; i++)
                {
                    long nclr = *((long*)outputPtr);
                    output[i] = new RGBQUAD
                    {
                        rgbRed = (byte)((nclr & nmask) / nmax * cmax),
                        rgbGreen = (byte)(((nclr >> 16) & nmask) / nmax * cmax),
                        rgbBlue = (byte)(((nclr >> 32) & nmask) / nmax * cmax),
                    };
                    outputPtr += colorSize;
                }

                return output;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(paInputColors);
            Marshal.FreeHGlobal(paOutputColors);
        }
    }

    public static unsafe void TransformPixelsTo_sRGB(SafeProfileHandle source, mscmsPxFormat pxFormat, void* data, int width, int height, uint stride, mscmsIntent dwIntent)
    {
        using (var dest = OpenProfile_sRGB())
        using (var transform = CreateTransform(source, dest, dwIntent))
        {
            var success = TranslateBitmapBits(transform, data, pxFormat, (uint)width, (uint)height, stride, data, pxFormat, stride, IntPtr.Zero, IntPtr.Zero);
            if (!success)
                throw new Win32Exception();
        }
    }

    public static SafeTransformHandle CreateTransform(SafeProfileHandle sourceProfile, SafeProfileHandle destinationProfile, mscmsIntent dwIntent)
    {
        if (sourceProfile == null || sourceProfile.IsInvalid)
            throw new ArgumentNullException("sourceProfile");

        if (destinationProfile == null || destinationProfile.IsInvalid)
            throw new ArgumentNullException("destinationProfile");

        IntPtr[] handles = new IntPtr[2];
        bool success = true;

        sourceProfile.DangerousAddRef(ref success);
        destinationProfile.DangerousAddRef(ref success);

        try
        {
            handles[0] = sourceProfile.DangerousGetHandle();
            handles[1] = destinationProfile.DangerousGetHandle();

            uint[] dwIntents = new uint[2] { (uint)dwIntent, (uint)dwIntent };

            var htransform = CreateMultiProfileTransform(
                handles, (uint)handles.Length,
                dwIntents, (uint)dwIntents.Length,
                BEST_MODE | USE_RELATIVE_COLORIMETRIC, 0);

            if (htransform.IsInvalid)
                throw new Win32Exception();

            return htransform;
        }
        finally
        {
            sourceProfile.DangerousRelease();
            destinationProfile.DangerousRelease();
        }
    }

    public static unsafe SafeProfileHandle CreateProfileFromLogicalColorSpace(BITMAPV5HEADER info)
    {
        // https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logcolorspacew
        var lcs = new LOGCOLORSPACE
        {
            lcsCSType = ColorSpaceType.LCS_CALIBRATED_RGB,
            lcsVersion = 0x400,
            lcsSignature = 0x50534F43, // 'PSOC'
            lcsFilename = "\0",
            lcsIntent = info.bV5Intent,
            lcsEndpoints_1x = info.bV5Endpoints_1x,
            lcsEndpoints_1y = info.bV5Endpoints_1y,
            lcsEndpoints_1z = info.bV5Endpoints_1z,
            lcsEndpoints_2x = info.bV5Endpoints_2x,
            lcsEndpoints_2y = info.bV5Endpoints_2y,
            lcsEndpoints_2z = info.bV5Endpoints_2z,
            lcsEndpoints_3x = info.bV5Endpoints_3x,
            lcsEndpoints_3y = info.bV5Endpoints_3y,
            lcsEndpoints_3z = info.bV5Endpoints_3z,
            lcsGammaBlue = info.bV5GammaBlue,
            lcsGammaGreen = info.bV5GammaGreen,
            lcsGammaRed = info.bV5GammaRed,
        };

        var success = CreateProfileFromLogColorSpace(ref lcs, out var hGlobal);
        if (!success) throw new Win32Exception();

        var hsize = GlobalSize(hGlobal);
        var hptr = GlobalLock(hGlobal);
        try
        {
            return OpenProfile((void*)hptr, (uint)hsize);
        }
        finally
        {
            GlobalUnlock(hGlobal);
            GlobalFree(hGlobal);
        }
    }

    public static unsafe SafeProfileHandle OpenProfile(byte[] profileData)
    {
        fixed (void* pBytes = profileData)
            return OpenProfile(pBytes, (uint)profileData.Length);
    }

    public static unsafe SafeProfileHandle OpenProfile(void* pProfileData, uint pLength)
    {
        var profile = new PROFILE
        {
            dwType = ProfileType.PROFILE_MEMBUFFER,
            pProfileData = pProfileData,
            cbDataSize = pLength,
        };

        return OpenColorProfile(ref profile, 1, 1, 3 /*OPEN_EXISTING*/);
    }

    public static unsafe SafeProfileHandle OpenProfile_sRGB()
    {
        bool success;
        int length;
        StringBuilder buffer;

        length = 1024;
        buffer = new StringBuilder(length);
        success = GetStandardColorSpaceProfile(IntPtr.Zero, ColorSpaceType.LCS_sRGB, buffer, out length);
        if (!success) throw new Win32Exception();
        string profilePath = buffer.ToString();

        if (!Uri.TryCreate(profilePath, UriKind.Absolute, out var profileUri))
        {
            // GetStandardColorSpaceProfile() returns whatever was given to SetStandardColorSpaceProfile().
            // If it were set to a relative path by the user, we should throw an exception to avoid any possible
            // security issues. However, the Vista control panel uses the same API and sometimes likes to set
            // relative paths. Since we can't tell the difference and we want people to be able to change
            // their color profile from the control panel, we'll tack on the system directory.

            length = 1024;
            buffer = new StringBuilder(length);
            success = GetColorDirectory(IntPtr.Zero, buffer, out length);
            if (!success) throw new Win32Exception();
            string colorDir = buffer.ToString();

            profilePath = Path.Combine(colorDir, profilePath);
        }

        var profileBytes = File.ReadAllBytes(profilePath);
        return OpenProfile(profileBytes);
    }

    public class SafeTransformHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeTransformHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return DeleteColorTransform(handle);
        }
    }

    public class SafeProfileHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeProfileHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return CloseColorProfile(handle);
        }
    }

    enum ProfileType : uint
    {
        PROFILE_FILENAME = 1,
        PROFILE_MEMBUFFER = 2
    };

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PROFILE
    {
        public ProfileType dwType; // profile type
        public void* pProfileData;         // either the filename of the profile or buffer containing profile depending upon dwtype
        public uint cbDataSize;           // size in bytes of pProfileData
    };

    [StructLayout(LayoutKind.Sequential)]
    struct LOGCOLORSPACE
    {
        public uint lcsSignature;
        public uint lcsVersion;
        public ColorSpaceType lcsCSType;
        public Bv5Intent lcsIntent;
        public uint lcsEndpoints_1x;
        public uint lcsEndpoints_1y;
        public uint lcsEndpoints_1z;
        public uint lcsEndpoints_2x;
        public uint lcsEndpoints_2y;
        public uint lcsEndpoints_2z;
        public uint lcsEndpoints_3x;
        public uint lcsEndpoints_3y;
        public uint lcsEndpoints_3z;
        public uint lcsGammaRed;
        public uint lcsGammaGreen;
        public uint lcsGammaBlue;
        public string lcsFilename;
    }

    enum COLOR_TYPE : uint
    {
        COLOR_GRAY = 1,
        COLOR_RGB,
        COLOR_XYZ,
        COLOR_Yxy,
        COLOR_Lab,
        COLOR_3_CHANNEL,        // WORD per channel
        COLOR_CMYK,
        COLOR_5_CHANNEL,        // BYTE per channel
        COLOR_6_CHANNEL,        //      - do -
        COLOR_7_CHANNEL,        //      - do -
        COLOR_8_CHANNEL,        //      - do -
        COLOR_NAMED,
    }

    public enum mscmsIntent : uint
    {
        INTENT_PERCEPTUAL = 0,
        INTENT_RELATIVE_COLORIMETRIC = 1,
        INTENT_SATURATION = 2,
        INTENT_ABSOLUTE_COLORIMETRIC = 3,
    }

    public enum mscmsPxFormat : uint
    {
        //
        // 16bpp - 5 bits per channel. The most significant bit is ignored.
        //

        BM_x555RGB = 0x0000,
        BM_x555XYZ = 0x0101,
        BM_x555Yxy,
        BM_x555Lab,
        BM_x555G3CH,

        //
        // Packed 8 bits per channel => 8bpp for GRAY and
        // 24bpp for the 3 channel colors, more for hifi channels
        //

        BM_RGBTRIPLETS = 0x0002,
        BM_BGRTRIPLETS = 0x0004,
        BM_XYZTRIPLETS = 0x0201,
        BM_YxyTRIPLETS,
        BM_LabTRIPLETS,
        BM_G3CHTRIPLETS,
        BM_5CHANNEL,
        BM_6CHANNEL,
        BM_7CHANNEL,
        BM_8CHANNEL,
        BM_GRAY,

        //
        // 32bpp - 8 bits per channel. The most significant byte is ignored
        // for the 3 channel colors.
        //

        BM_xRGBQUADS = 0x0008,
        BM_xBGRQUADS = 0x0010,
        BM_xG3CHQUADS = 0x0304,
        BM_KYMCQUADS,
        BM_CMYKQUADS = 0x0020,

        //
        // 32bpp - 10 bits per channel. The 2 most significant bits are ignored.
        //

        BM_10b_RGB = 0x0009,
        BM_10b_XYZ = 0x0401,
        BM_10b_Yxy,
        BM_10b_Lab,
        BM_10b_G3CH,

        //
        // 32bpp - named color indices (1-based)
        //

        BM_NAMED_INDEX,

        //
        // Packed 16 bits per channel => 16bpp for GRAY and
        // 48bpp for the 3 channel colors.
        //

        BM_16b_RGB = 0x000A,
        BM_16b_XYZ = 0x0501,
        BM_16b_Yxy,
        BM_16b_Lab,
        BM_16b_G3CH,
        BM_16b_GRAY,

        //
        // 16 bpp - 5 bits for Red & Blue, 6 bits for Green
        //

        BM_565RGB = 0x0001,

        //#if NTDDI_VERSION >= NTDDI_VISTA
        //
        // scRGB - 32 bits per channel floating point
        //         16 bits per channel floating point
        //

        BM_32b_scRGB = 0x0601,
        BM_32b_scARGB = 0x0602,
        BM_S2DOT13FIXED_scRGB = 0x0603,
        BM_S2DOT13FIXED_scARGB = 0x0604,
        //#endif // NTDDI_VERSION >= NTDDI_VISTA

        //#if NTDDI_VERSION >= NTDDI_WIN7
        BM_R10G10B10A2 = 0x0701,
        BM_R10G10B10A2_XR = 0x0702,
        BM_R16G16B16A16_FLOAT = 0x0703
        //#endif // NTDDI_VERSION >= NTDDI_WIN7
    }
}
