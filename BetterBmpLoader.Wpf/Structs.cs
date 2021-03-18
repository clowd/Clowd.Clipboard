using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BetterBmpLoader
{
    internal unsafe class StructUtil
    {
        public static T Deserialize<T>(byte* ptr)
        {
            return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
        }

        public static void SerializeTo<T>(T s, byte[] buffer, ref uint destOffset) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(s, ptr, true);
            Marshal.Copy(ptr, buffer, (int)destOffset, size);
            Marshal.FreeHGlobal(ptr);
            destOffset += (uint)size;
        }

        public static ushort ReadU16(byte* ptr)
        {
            var arr = new byte[] { *ptr, *(ptr + 1) };
            return BitConverter.ToUInt16(arr, 0);
        }

        public static uint ReadU24(byte* ptr)
        {
            var arr = new byte[] { *ptr, *(ptr + 1), *(ptr + 2) };
            return BitConverter.ToUInt32(arr, 0);
        }

        public static uint ReadU32(byte* ptr)
        {
            var arr = new byte[] { *ptr, *(ptr + 1), *(ptr + 2), *(ptr + 3) };
            return BitConverter.ToUInt32(arr, 0);
        }

        public static byte[] ReadBytes(Stream stream)
        {
            if (stream is MemoryStream mem)
            {
                return mem.ToArray();
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
                            return ms.ToArray();
                        ms.Write(buffer, 0, read);
                    }
                }
            }
        }
    }

    internal unsafe delegate byte* WritePixelToPtr(byte* ptr, byte b, byte g, byte r, byte a);

    internal unsafe class BitmapCorePixelFormat2 : IEquatable<BitmapCorePixelFormat2>
    {
        public bool IsStandardRGB { get; private set; }
        public bool IsIndexed => BitsPerPixel < 16;
        public bool HasAlpha => Masks != null && Masks.Length == 4;

        public uint LcmsFormat { get; private set; }
        public uint[] Masks { get; private set; }
        public WritePixelToPtr Write { get; private set; }
        public ushort BitsPerPixel { get; private set; }

        private BitmapCorePixelFormat2() { }

        public static readonly BitmapCorePixelFormat2 Indexed1 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 1,
            IsStandardRGB = true,
        };

        public static readonly BitmapCorePixelFormat2 Indexed2 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 2,
            IsStandardRGB = true,
        };

        public static readonly BitmapCorePixelFormat2 Indexed4 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 4,
            IsStandardRGB = true,
        };

        public static readonly BitmapCorePixelFormat2 Indexed8 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 8,
            IsStandardRGB = true,
        };

        public static readonly BitmapCorePixelFormat2 Bgr555X1 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 16,
            Masks = BitFields.BITFIELDS_BGRA_555X,
            IsStandardRGB = true,
            Write = (ptr, b, g, r, a) =>
            {
                const ushort max5 = 0x1F;
                const double mult5 = max5 / 255d;

                ushort* dest = (ushort*)ptr;

                byte cb = (byte)Math.Ceiling(b * mult5);
                byte cg = (byte)Math.Ceiling(g * mult5);
                byte cr = (byte)Math.Ceiling(r * mult5);

                *dest += (ushort)(b | (g << 5) | (r << 10));

                return (byte*)dest;
            },
        };

        public static readonly BitmapCorePixelFormat2 Bgr5551 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 16,
            Masks = BitFields.BITFIELDS_BGRA_5551,
            Write = (ptr, b, g, r, a) =>
            {
                const ushort max5 = 0x1F;
                const double mult5 = max5 / 255d;

                ushort* dest = (ushort*)ptr;

                byte cb = (byte)Math.Ceiling(b * mult5);
                byte cg = (byte)Math.Ceiling(g * mult5);
                byte cr = (byte)Math.Ceiling(r * mult5);
                byte ca = a > 0 ? (byte)1 : (byte)0;

                *dest += (ushort)(b | (g << 5) | (r << 10) | (a << 15));

                return (byte*)dest;
            },
        };

        public static readonly BitmapCorePixelFormat2 Bgr565 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 16,
            Masks = BitFields.BITFIELDS_BGR_565,
            Write = (ptr, b, g, r, a) =>
            {
                const ushort max5 = 0x1F;
                const ushort max6 = 0x3F;
                const double mult5 = max5 / 255d;
                const double mult6 = max6 / 255d;

                ushort* dest = (ushort*)ptr;

                byte cb = (byte)Math.Ceiling(b * mult5);
                byte cg = (byte)Math.Ceiling(g * mult6);
                byte cr = (byte)Math.Ceiling(r * mult5);

                *dest += (ushort)(b | (g << 5) | (r << 11));

                return (byte*)dest;
            },
        };

        public static readonly BitmapCorePixelFormat2 Rgb24 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 24,
            LcmsFormat = Lcms.TYPE_RGB_8,
            Masks = BitFields.BITFIELDS_RGB_24,
            Write = (ptr, b, g, r, a) =>
            {
                *ptr++ = r;
                *ptr++ = g;
                *ptr++ = b;
                return ptr;
            },
        };

        public static readonly BitmapCorePixelFormat2 Bgr24 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 24,
            LcmsFormat = Lcms.TYPE_BGR_8,
            Masks = BitFields.BITFIELDS_BGR_24,
            IsStandardRGB = true,
            Write = (ptr, b, g, r, a) =>
            {
                *ptr++ = b;
                *ptr++ = g;
                *ptr++ = r;
                return ptr;
            },
        };

        public static readonly BitmapCorePixelFormat2 Bgra32 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 32,
            LcmsFormat = Lcms.TYPE_BGRA_8,
            Masks = BitFields.BITFIELDS_BGRA_32,
            Write = (ptr, b, g, r, a) =>
            {
                uint* dest = (uint*)ptr;
                *dest++ = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
                return (byte*)dest;
            },
        };

        public static readonly BitmapCorePixelFormat2[] Formats = new BitmapCorePixelFormat2[]
        {
            Indexed1,
            Indexed2,
            Indexed4,
            Indexed8,
            Bgr555X1,
            Bgr5551,
            Bgr565,
            Rgb24,
            Bgr24,
            Bgra32,
        };

        public bool IsMatch(ushort bits, uint maskB, uint maskG, uint maskR, uint maskA)
        {
            if (bits != BitsPerPixel)
                return false;

            if (IsIndexed)
            {
                return true;
            }
            else
            {
                if (maskA > 0 && Masks.Length == 4)
                    return Masks[0] == maskR && Masks[1] == maskG && Masks[2] == maskB && Masks[3] == maskA;

                if (maskA == 0 && Masks.Length == 3)
                    return Masks[0] == maskR && Masks[1] == maskG && Masks[2] == maskB;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is BitmapCorePixelFormat2 fmt) return fmt.Equals(this);
            return false;
        }

        public bool Equals(BitmapCorePixelFormat2 other)
        {
            if (other.BitsPerPixel != this.BitsPerPixel)
                return false;

            if ((Masks == null) != (other.Masks == null))
                return false;

            if (Masks != null)
            {
                if (Masks.Length != other.Masks.Length)
                    return false;

                for (int i = 0; i < Masks.Length; i++)
                    if (Masks[i] != other.Masks[i])
                        return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 7) + BitsPerPixel.GetHashCode();

                if (Masks != null)
                {
                    for (int i = 0; i < Masks.Length; i++)
                        hash = (hash * 7) + Masks[i].GetHashCode();
                }

                return hash;
            }
        }
    }

    internal class BitFields
    {
        public static readonly uint[] BITFIELDS_RGB_24 = new uint[] { 0xff, 0xff00, 0xff0000 };
        public static readonly uint[] BITFIELDS_BGR_24 = new uint[] { 0xff0000, 0xff00, 0xff };
        public static readonly uint[] BITFIELDS_BGRA_32 = new uint[] { 0xff0000, 0xff00, 0xff, 0xff000000 };
        //private static readonly uint[] BITFIELDS_BGR_32 = new uint[] { 0xff0000, 0xff00, 0xff };
        public static readonly uint[] BITFIELDS_BGR_565 = new uint[] { 0xf800, 0x7e0, 0x1f };
        public static readonly uint[] BITFIELDS_BGRA_5551 = new uint[] { 0x7c00, 0x03e0, 0x001f, 0x8000 };
        public static readonly uint[] BITFIELDS_BGRA_555X = new uint[] { 0x7c00, 0x03e0, 0x001f };
    }

    //internal enum BitmapCorePixelFormat
    //{
    //    // Unsupported WPF formats
    //    //Rgba128Float,
    //    //Gray32Float,
    //    //Gray16,
    //    //Prgba64,
    //    //Rgba64,
    //    //Rgb48,
    //    //Rgb128Float,
    //    //Gray8,
    //    //Gray4,
    //    //Gray2,
    //    //BlackWhite,
    //    //Prgba128Float,
    //    //Cmyk32,

    //    // Unsupported GDI formats
    //    //Undefined,
    //    //DontCare,
    //    //Indexed,
    //    //Gdi,
    //    //Alpha,
    //    //PAlpha,
    //    //Extended,
    //    //Format16bppGrayScale,
    //    //Canonical,
    //    //Format64bppArgb,
    //    //Format16bppRgb555,
    //    //Format16bppRgb565,
    //    //Format24bppRgb,
    //    //Format32bppRgb,
    //    //Format1bppIndexed,
    //    //Format4bppIndexed,
    //    //Format8bppIndexed,
    //    //Format16bppArgb1555,
    //    //Format32bppPArgb,
    //    //Format64bppPArgb,
    //    //Format32bppArgb,
    //    //Unknown,
    //    //Rgb24,
    //    //Pbgra32,
    //    Bgra32,
    //    Bgr32,
    //    //Bgr101010,
    //    //Bgr24,
    //    //Bgr565,
    //    //Bgr555,
    //    //Bgra5551,
    //    //Indexed8,
    //    //Indexed4,
    //    //Indexed2,
    //    //Indexed1,
    //}

    enum BitmapCoreHeaderType
    {
        BestFit,
        ForceVINFO,
        ForceV5,
    }

    internal struct BITMAP_WRITE_REQUEST
    {
        public BitmapCoreHeaderType headerType;

        public double dpiX;
        public double dpiY;

        public RGBQUAD[] imgColorTable;
        public bool imgTopDown;
        public int imgWidth;
        public int imgHeight;
        public uint imgStride;
        public BitmapCorePixelFormat2 fmt;
    }

    internal struct BITMAP_READ_DETAILS
    {
        public BITMAPV5HEADER dibHeader;
        public ushort bbp;
        public double dpiX;
        public double dpiY;
        public BitmapCompressionMode compression;

        public uint cMaskRed;
        public uint cMaskGreen;
        public uint cMaskBlue;
        public uint cMaskAlpha;
        public bool cTrueAlpha;
        public bool cIndexed;

        public BitmapCorePixelFormat2 imgFmt;
        public RGBQUAD[] imgColorTable;
        public uint imgStride;
        public uint imgDataOffset;
        public uint imgDataSize;
        public bool imgTopDown;
        public int imgWidth;
        public int imgHeight;

        public uint iccProfileOffset;
        public uint iccProfileSize;
        public ColorSpaceType iccProfileType;
        public Bv5Intent iccProfileIntent;
    }

    internal enum ColorSpaceType : uint
    {
        LCS_CALIBRATED_RGB = 0,
        LCS_sRGB = 0x73524742, // 'sRGB'
        LCS_WINDOWS_COLOR_SPACE = 0x57696e20, // 'Win '
        PROFILE_LINKED = 1279872587,
        PROFILE_EMBEDDED = 1296188740,
    }

    internal enum Bv5Intent : uint
    {
        LCS_GM_BUSINESS = 1, // Graphic / Saturation
        LCS_GM_GRAPHICS = 2, // Proof / Relative Colorimetric
        LCS_GM_IMAGES = 4, // Picture / Perceptual
        LCS_GM_ABS_COLORIMETRIC = 8, // Match / Absolute Colorimetric
    }

    internal enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5,
        BI_ALPHABITFIELDS = 6,
        BI_CMYK = 11,
        BI_CMYKRLE8 = 12,
        BI_CMYKRLE4 = 13,

        // OS2 bitmap compression modes re-mapped for clarity
        OS2_RLE24 = 98,
        OS2_HUFFMAN1D = 99,
    }

    //internal struct CIEXYZd
    //{
    //    public double ciexyzX;
    //    public double ciexyzY;
    //    public double ciexyzZ;
    //}

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPCOREHEADER // OS21BITMAPHEADER
    {
        // SIZE = 12
        public uint bcSize;
        public ushort bcWidth;
        public ushort bcHeight;
        public ushort bcPlanes;
        public ushort bcBitCount;
    }

    // https://d3s.mff.cuni.cz/legacy/teaching/principles_of_computers/Zkouska%20Principy%20pocitacu%202017-18%20-%20varianta%2002%20-%20priloha%20-%20format%20BMP%20z%20Wiki.pdf
    [StructLayout(LayoutKind.Sequential)]
    internal struct OS22XBITMAPHEADER // 16
    {
        public uint Size;             /* Size of this structure in bytes */
        public uint Width;            /* Bitmap width in pixels */
        public uint Height;           /* Bitmap height in pixel */
        public ushort NumPlanes;        /* Number of bit planes (color depth) */
        public ushort BitsPerPixel;     /* Number of bits per pixel per plane */
        public BitmapCompressionMode Compression;      /* Bitmap compression scheme */
        public uint ImageDataSize;    /* Size of bitmap data in bytes */
        public uint XResolution;      /* X resolution of display device */
        public uint YResolution;      /* Y resolution of display device */
        public uint ColorsUsed;       /* Number of color table indices used */
        public uint ColorsImportant;  /* Number of important color indices */
        public ushort Units;            /* Type of units used to measure resolution */
        public ushort Reserved;         /* Pad structure to 4-byte boundary */
        public ushort Recording;        /* Recording algorithm */
        public ushort Rendering;        /* Halftoning algorithm used */
        public uint Size1;            /* Reserved for halftoning algorithm use */
        public uint Size2;            /* Reserved for halftoning algorithm use */
        public uint ColorEncoding;    /* Color model used in bitmap */
        public uint Identifier;       /* Reserved for application use */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
    {
        // BITMAPINFOHEADER SIZE = 40
        public uint bV5Size; // uint
        public int bV5Width;
        public int bV5Height; // LONG
        public ushort bV5Planes; // WORD
        public ushort bV5BitCount;
        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV3INFOHEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV4HEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
        public ColorSpaceType bV5CSType;
        public uint bV5Endpoints_1x;
        public uint bV5Endpoints_1y;
        public uint bV5Endpoints_1z;
        public uint bV5Endpoints_2x;
        public uint bV5Endpoints_2y;
        public uint bV5Endpoints_2z;
        public uint bV5Endpoints_3x;
        public uint bV5Endpoints_3y;
        public uint bV5Endpoints_3z;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV5HEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        // BITMAPCOREHEADER = 16

        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        // BITMAPINFOHEADER = 40

        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        // BITMAPV2INFOHEADER = 52

        public uint bV5AlphaMask;
        // BITMAPV3INFOHEADER = 56

        public ColorSpaceType bV5CSType;
        public uint bV5Endpoints_1x;
        public uint bV5Endpoints_1y;
        public uint bV5Endpoints_1z;
        public uint bV5Endpoints_2x;
        public uint bV5Endpoints_2y;
        public uint bV5Endpoints_2z;
        public uint bV5Endpoints_3x;
        public uint bV5Endpoints_3y;
        public uint bV5Endpoints_3z;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
        // BITMAPV4HEADER = 108

        public Bv5Intent bV5Intent;
        public uint bV5ProfileData;
        public uint bV5ProfileSize;
        public uint bV5Reserved;
        // BITMAPV5HEADER = 124
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct BITMAPFILEHEADER // size = 14
    {
        public ushort bfType;
        public uint bfSize;
        public ushort bfReserved1;
        public ushort bfReserved2;
        public uint bfOffBits;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RGBTRIPLE
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
    }
}
