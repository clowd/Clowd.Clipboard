#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Runtime.InteropServices;

namespace Clowd.Clipboard.Bitmaps;

public struct BITMASKS
{
    public uint maskRed;
    public uint maskGreen;
    public uint maskBlue;
    public uint maskAlpha;

    public BITMASKS(uint r, uint g, uint b)
    {
        maskRed = r;
        maskGreen = g;
        maskBlue = b;
        maskAlpha = 0;
    }

    public BITMASKS(uint r, uint g, uint b, uint a)
    {
        maskRed = r;
        maskGreen = g;
        maskBlue = b;
        maskAlpha = a;
    }

    public uint[] BITFIELDS() => new uint[] { maskRed, maskGreen, maskBlue };

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 13;
            hash = hash * 7 + (int)maskRed;
            hash = hash * 7 + (int)maskGreen;
            hash = hash * 7 + (int)maskBlue;
            hash = hash * 7 + (int)maskAlpha;
            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is BITMASKS bm)
        {
            return maskRed == bm.maskRed && maskGreen == bm.maskGreen && maskBlue == bm.maskBlue && maskAlpha == bm.maskAlpha;
        }
        return false;
    }

    public override string ToString()
    {
        return $"Mask[B=0x{maskBlue.ToString("X")}, G=0x{maskGreen.ToString("X")}, R=0x{maskRed.ToString("X")}, A=0x{maskAlpha.ToString("X")}]";
    }
}

public struct BITMAP_WRITE_REQUEST
{
    public double dpiX;
    public double dpiY;

    public RGBQUAD[] imgColorTable;
    public bool imgTopDown;
    public int imgWidth;
    public int imgHeight;
    public uint imgStride;
}

public struct BITMAP_READ_DETAILS
{
    public BITMAPV5HEADER dibHeader;
    public ushort bbp;
    public double dpiX;
    public double dpiY;
    public BitmapCompressionMode compression;

    public BITMASKS cMasks;
    public bool cTrueAlpha;
    public bool cIndexed;

    public BitmapCorePixelFormat imgSourceFmt;
    public RGBQUAD[] imgColorTable;
    public uint imgStride;
    public uint imgDataOffset;
    public uint imgDataSize;
    public bool imgTopDown;
    public int imgWidth;
    public int imgHeight;

    public mscms.SafeProfileHandle colorProfile;
    public mscms.mscmsIntent colorProfileIntent;
}

public enum ColorSpaceType : uint
{
    LCS_CALIBRATED_RGB = 0,
    LCS_sRGB = 0x73524742, // 'sRGB'
    LCS_WINDOWS_COLOR_SPACE = 0x57696e20, // 'Win '
    PROFILE_LINKED = 1279872587,
    PROFILE_EMBEDDED = 1296188740,
}

public enum Bv5Intent : uint
{
    LCS_GM_BUSINESS = 1, // Graphic / Saturation
    LCS_GM_GRAPHICS = 2, // Proof / Relative Colorimetric
    LCS_GM_IMAGES = 4, // Picture / Perceptual
    LCS_GM_ABS_COLORIMETRIC = 8, // Match / Absolute Colorimetric
}

public enum BitmapCompressionMode : uint
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

[StructLayout(LayoutKind.Sequential)]
public struct BITMAPCOREHEADER // OS21BITMAPHEADER
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
public struct OS22XBITMAPHEADER // 16
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
public struct BITMAPINFOHEADER
{
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
public struct BITMAPV3INFOHEADER
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
public struct BITMAPV4HEADER
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
public struct BITMAPV5HEADER
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
public struct BITMAPFILEHEADER // size = 14
{
    public ushort bfType;
    public uint bfSize;
    public ushort bfReserved1;
    public ushort bfReserved2;
    public uint bfOffBits;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RGBQUAD
{
    public byte rgbBlue;
    public byte rgbGreen;
    public byte rgbRed;
    public byte rgbReserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RGBTRIPLE
{
    public byte rgbBlue;
    public byte rgbGreen;
    public byte rgbRed;
}

[StructLayout(LayoutKind.Sequential)]
public struct MASKTRIPLE
{
    public uint rgbRed;
    public uint rgbGreen;
    public uint rgbBlue;
}

[StructLayout(LayoutKind.Sequential)]
public struct MASKQUAD
{
    public uint rgbRed;
    public uint rgbGreen;
    public uint rgbBlue;
    public uint rgbAlpha;
}
