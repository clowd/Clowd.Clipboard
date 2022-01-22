using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Clowd.Clipboard.Bitmaps.Core;

namespace Clowd.Clipboard.Bitmaps.Core;

// http://zig.tgschultz.com/bmp_file_format.txt
// http://paulbourke.net/dataformats/bitmaps/
// http://www.libertybasicuniversity.com/lbnews/nl100/format.htm
// https://www.displayfusion.com/Discussions/View/converting-c-data-types-to-c/?ID=38db6001-45e5-41a3-ab39-8004450204b3
// https://github.com/FlyingPumba/tp2-orga2/blob/master/entregable/src/bmp/bmp.c

internal unsafe partial class BitmapCore
{
    public const uint
        BC_READ_PRESERVE_INVALID_ALPHA = 1,
        BC_READ_STRICT_PRESERVE_FORMAT = 2,
        BC_READ_FORCE_BGRA32 = 4,
        BC_READ_IGNORE_COLOR_PROFILE = 8,
        BC_WRITE_V5 = 1,
        BC_WRITE_VINFO = 2,
        BC_WRITE_SKIP_FH = 4;

    private const ushort BFH_BM = 0x4D42;

    private const string ERR_HEOF = "Bitmap stream ended while parsing header, but more data was expected. This usually indicates an malformed file or empty data stream.";

    public static void ReadHeader(byte* source, int sourceLength, out BITMAP_READ_DETAILS info, uint bcrFlags)
    {
        var ptr = source;

        if ((sourceLength) < 12) // min header size
            throw new InvalidOperationException(ERR_HEOF);

        bool hasFileHeader = StructUtil.ReadU16(ptr) == BFH_BM;
        var size_fh = Marshal.SizeOf<BITMAPFILEHEADER>();

        int offset = 0;
        var fh = default(BITMAPFILEHEADER);
        if (hasFileHeader)
        {
            var fhsize = Marshal.SizeOf<BITMAPFILEHEADER>();
            if (offset + fhsize > sourceLength)
                throw new InvalidOperationException(ERR_HEOF);

            fh = StructUtil.Deserialize<BITMAPFILEHEADER>(ptr);
            ptr += fhsize;
            offset += fhsize;
        }

        // we'll just unpack all the various header types we support into a standard BMPV5 header 
        // this makes subsequent code easier to maintain as it only needs to refer to one place

        if ((sourceLength - offset) < 12) // min header size
            throw new InvalidOperationException(ERR_HEOF);

        var header_size = StructUtil.ReadU32(ptr);
        var bi = default(BITMAPV5HEADER);
        bool is_os21x_ = false;

        if (header_size == 12)
        {
            var bich = StructUtil.Deserialize<BITMAPCOREHEADER>(ptr);
            bi.bV5Size = bich.bcSize;
            bi.bV5Width = bich.bcWidth;
            bi.bV5Height = bich.bcHeight;
            bi.bV5Planes = bich.bcPlanes;
            bi.bV5BitCount = bich.bcBitCount;

            bi.bV5CSType = ColorSpaceType.LCS_sRGB;
            is_os21x_ = true;
        }
        else if (/*header_size == 14 || */header_size == 16 || header_size == 42 || header_size == 46 || header_size == 64)
        {
            var biih = StructUtil.Deserialize<BITMAPINFOHEADER>(ptr);
            bi.bV5Size = biih.bV5Size;
            bi.bV5Width = biih.bV5Width;
            bi.bV5Height = biih.bV5Height;
            bi.bV5Planes = biih.bV5Planes;
            bi.bV5BitCount = biih.bV5BitCount;

            if (header_size > 16)
            {
                bi.bV5Compression = biih.bV5Compression;
                bi.bV5SizeImage = biih.bV5SizeImage;
                bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
                bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
                bi.bV5ClrUsed = biih.bV5ClrUsed;
                bi.bV5ClrImportant = biih.bV5ClrImportant;
            }

            // https://www.fileformat.info/mirror/egff/ch09_05.htm (G31D)
            if (bi.bV5Compression == (BitmapCompressionMode)3 && bi.bV5BitCount == 1)
                bi.bV5Compression = BitmapCompressionMode.OS2_HUFFMAN1D;

            else if (bi.bV5Compression == (BitmapCompressionMode)4 && bi.bV5BitCount == 24)
                bi.bV5Compression = BitmapCompressionMode.OS2_RLE24;

            bi.bV5CSType = ColorSpaceType.LCS_sRGB;

        }
        else if (header_size == 40)
        {
            var biih = StructUtil.Deserialize<BITMAPINFOHEADER>(ptr);
            bi.bV5Size = biih.bV5Size;
            bi.bV5Width = biih.bV5Width;
            bi.bV5Height = biih.bV5Height;
            bi.bV5Planes = biih.bV5Planes;
            bi.bV5BitCount = biih.bV5BitCount;
            bi.bV5Compression = biih.bV5Compression;
            bi.bV5SizeImage = biih.bV5SizeImage;
            bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
            bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
            bi.bV5ClrUsed = biih.bV5ClrUsed;
            bi.bV5ClrImportant = biih.bV5ClrImportant;

            bi.bV5CSType = ColorSpaceType.LCS_sRGB;
        }
        else if (header_size == 52 || header_size == 56)
        {
            var biih = StructUtil.Deserialize<BITMAPV3INFOHEADER>(ptr);
            bi.bV5Size = biih.bV5Size;
            bi.bV5Width = biih.bV5Width;
            bi.bV5Height = biih.bV5Height;
            bi.bV5Planes = biih.bV5Planes;
            bi.bV5BitCount = biih.bV5BitCount;
            bi.bV5Compression = biih.bV5Compression;
            bi.bV5SizeImage = biih.bV5SizeImage;
            bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
            bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
            bi.bV5ClrUsed = biih.bV5ClrUsed;
            bi.bV5ClrImportant = biih.bV5ClrImportant;
            bi.bV5RedMask = biih.bV5RedMask;
            bi.bV5GreenMask = biih.bV5GreenMask;
            bi.bV5BlueMask = biih.bV5BlueMask;

            if (header_size == 56) // 56b header adds alpha mask
                bi.bV5AlphaMask = biih.bV5AlphaMask;

            bi.bV5CSType = ColorSpaceType.LCS_sRGB;
        }
        else if (header_size == 108)
        {
            var biih = StructUtil.Deserialize<BITMAPV4HEADER>(ptr);
            bi.bV5Size = biih.bV5Size;
            bi.bV5Width = biih.bV5Width;
            bi.bV5Height = biih.bV5Height;
            bi.bV5Planes = biih.bV5Planes;
            bi.bV5BitCount = biih.bV5BitCount;
            bi.bV5Compression = biih.bV5Compression;
            bi.bV5SizeImage = biih.bV5SizeImage;
            bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
            bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
            bi.bV5ClrUsed = biih.bV5ClrUsed;
            bi.bV5ClrImportant = biih.bV5ClrImportant;
            bi.bV5RedMask = biih.bV5RedMask;
            bi.bV5GreenMask = biih.bV5GreenMask;
            bi.bV5BlueMask = biih.bV5BlueMask;
            bi.bV5AlphaMask = biih.bV5AlphaMask;
            bi.bV5CSType = biih.bV5CSType;
            bi.bV5Endpoints_1x = biih.bV5Endpoints_1x;
            bi.bV5Endpoints_1y = biih.bV5Endpoints_1y;
            bi.bV5Endpoints_1z = biih.bV5Endpoints_1z;
            bi.bV5Endpoints_2x = biih.bV5Endpoints_2x;
            bi.bV5Endpoints_2y = biih.bV5Endpoints_2y;
            bi.bV5Endpoints_2z = biih.bV5Endpoints_2z;
            bi.bV5Endpoints_3x = biih.bV5Endpoints_3x;
            bi.bV5Endpoints_3y = biih.bV5Endpoints_3y;
            bi.bV5Endpoints_3z = biih.bV5Endpoints_3z;
            bi.bV5GammaRed = biih.bV5GammaRed;
            bi.bV5GammaGreen = biih.bV5GammaGreen;
            bi.bV5GammaBlue = biih.bV5GammaBlue;
        }
        else if (header_size == 124)
        {
            bi = StructUtil.Deserialize<BITMAPV5HEADER>(ptr);
        }
        else
        {
            throw new NotSupportedException($"Bitmap header size '{header_size}' not known / supported.");
        }

        ptr += header_size;
        offset += (int)header_size;

        ushort nbits = bi.bV5BitCount;

        //if (bi.bV5Planes != 1)
        //    throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

        // we don't support linked profiles, custom windows profiles, etc - so default to sRGB instead of throwing...

        if (bi.bV5CSType != ColorSpaceType.LCS_CALIBRATED_RGB && bi.bV5CSType != ColorSpaceType.PROFILE_EMBEDDED)
            bi.bV5CSType = ColorSpaceType.LCS_sRGB;

        uint maskR = 0;
        uint maskG = 0;
        uint maskB = 0;
        uint maskA = 0;

        bool hasAlphaChannel = false;
        bool skipVerifyBppAndMasks = false;

        switch (bi.bV5Compression)
        {
            case BitmapCompressionMode.BI_BITFIELDS:

                var rgb = StructUtil.Deserialize<MASKTRIPLE>(ptr);
                maskR = rgb.rgbRed;
                maskG = rgb.rgbGreen;
                maskB = rgb.rgbBlue;
                offset += Marshal.SizeOf<MASKTRIPLE>();

                break;
            case BitmapCompressionMode.BI_ALPHABITFIELDS:

                var rgba = StructUtil.Deserialize<MASKQUAD>(ptr);
                maskR = rgba.rgbRed;
                maskG = rgba.rgbGreen;
                maskB = rgba.rgbBlue;
                maskA = rgba.rgbAlpha;
                offset += Marshal.SizeOf<MASKQUAD>();

                hasAlphaChannel = true;
                break;
            case BitmapCompressionMode.BI_RGB:
                switch (nbits)
                {
                    case 32:
                        // windows wrongly uses the 4th byte of BI_RGB 32bit dibs as alpha
                        // but we need to do it too if we have any hope of reading alpha data
                        maskB = 0xff;
                        maskG = 0xff00;
                        maskR = 0xff0000;
                        maskA = 0xff000000; // fake transparency?
                        break;
                    case 24:
                        maskB = 0xff;
                        maskG = 0xff00;
                        maskR = 0xff0000;
                        break;
                    case 16:
                        maskB = 0x001f;
                        maskG = 0x03e0;
                        maskR = 0x7c00;
                        // we can check for transparency in 16b RGB but it is slower and is very uncommon
                        // maskA = 0x8000; // fake transparency?
                        break;
                }
                break;
            case BitmapCompressionMode.BI_JPEG:
            case BitmapCompressionMode.BI_PNG:
            case BitmapCompressionMode.BI_RLE4:
            case BitmapCompressionMode.BI_RLE8:
            case BitmapCompressionMode.OS2_RLE24:
                if (bi.bV5Height < 0) throw new NotSupportedException("Top-down bitmaps are not supported with RLE/JPEG/PNG compression.");
                skipVerifyBppAndMasks = true;
                break;
            case BitmapCompressionMode.OS2_HUFFMAN1D:
                if (bi.bV5Height < 0) throw new NotSupportedException("Top-down bitmaps are not supported with Huffman1D compression.");
                if (bi.bV5BitCount != 1) throw new NotSupportedException("Huffman1D compression is only supported with 1bpp bitmaps");
                skipVerifyBppAndMasks = true;
                break;
            default:
                throw new NotSupportedException($"Bitmap with bV5Compression of '{bi.bV5Compression.ToString()}' is not supported.");
        }

        // lets use the v3/v4/v5 masks if present instead of RGB
        // according to some readers (FIREFOX!) these masks are only valid if the compression mode is 
        // BI_BITFIELDS, meaning they might write garbage here when the compression is RGB
        if (bi.bV5Size >= 52 && bi.bV5Compression == BitmapCompressionMode.BI_BITFIELDS)
        {
            if (bi.bV5RedMask != 0) maskR = bi.bV5RedMask;
            if (bi.bV5BlueMask != 0) maskB = bi.bV5BlueMask;
            if (bi.bV5GreenMask != 0) maskG = bi.bV5GreenMask;
        }

        // if an alpha mask has been provided in the header, lets use it.
        if (bi.bV5Size >= 56 && bi.bV5AlphaMask != 0)
        {
            maskA = bi.bV5AlphaMask;
            hasAlphaChannel = true;
        }

        // try to infer alpha if 32bpp & no alpha mask was set (ie, BI_BITFIELDS)
        // this will only be used if the PRESERVE_FAKE_ALPHA flag is set
        if (maskA == 0 && nbits == 32)
        {
            maskA = (maskB | maskG | maskR) ^ 0xFFFFFFFF;
        }

        bool smBit = nbits == 1 || nbits == 2 || nbits == 4 || nbits == 8;
        bool lgBit = nbits == 16 || nbits == 24 || nbits == 32;

        if (!skipVerifyBppAndMasks)
        {
            if (!lgBit && !smBit)
                throw new NotSupportedException($"Bitmap with bits per pixel of '{nbits}' are not valid.");

            if (lgBit && maskR == 0 && maskB == 0 && maskG == 0)
                throw new NotSupportedException($"Bitmap (bbp {nbits}) color masks could not be determined, this usually indicates a malformed bitmap file.");
        }

        // The number of entries in the palette is either 2n (where n is the number of bits per pixel) or a smaller number specified in the header
        // always allocate at least 256 entries so we can ignore bad data which seeks past the end of palette data.
        var pallength = nbits < 16 ? (1 << nbits) : 0;
        if (bi.bV5ClrUsed > 0)
            pallength = (int)bi.bV5ClrUsed;

        if (pallength > 256) // technically the max is 256..? some bitmaps have invalidly/absurdly large palettes
        {
            if (hasFileHeader)
            {
                // if we have a file header, we can correct our pixel data offset below, so the only 
                // important thing is that we don't read too many colors.
                pallength = 256;
            }
            else
            {
                throw new NotSupportedException("Bitmap has an oversized/invalid color palette.");
            }
        }

        RGBQUAD[] palette = new RGBQUAD[pallength];
        var clrSize = is_os21x_ ? Marshal.SizeOf<RGBTRIPLE>() : Marshal.SizeOf<RGBQUAD>();
        for (int i = 0; i < palette.Length; i++)
        {
            if (is_os21x_)
            {
                var small = StructUtil.Deserialize<RGBTRIPLE>(ptr);
                palette[i] = new RGBQUAD { rgbBlue = small.rgbBlue, rgbGreen = small.rgbGreen, rgbRed = small.rgbRed };
            }
            else
            {
                palette[i] = StructUtil.Deserialize<RGBQUAD>(ptr);
            }
            ptr += clrSize;
        }

        offset += pallength * clrSize;

        // For RGB DIBs, the image orientation is indicated by the biHeight member of the BITMAPINFOHEADER structure. 
        // If biHeight is positive, the image is bottom-up. If biHeight is negative, the image is top-down.
        // DirectDraw uses top-down DIBs. In GDI, all DIBs are bottom-up. 
        // Also, any DIB type that uses a FOURCC in the biCompression member, should express its biHeight as a positive number 
        // no matter what its orientation is, since the FOURCC itself identifies a compression scheme whose image orientation 
        // should be understood by any compatible filter. Common YUV formats such as UYVY, YV12, and YUY2 are top-down oriented. 
        // It is invalid to store an image with these compression types in bottom-up orientation. 
        // The sign of biHeight for such formats must always be set positive

        var width = bi.bV5Width;
        var height = bi.bV5Height;
        bool topDown = false;

        if (height < 0)
        {
            height = -height;
            topDown = true;
        }

        if (width < 0)
            throw new NotSupportedException("Bitmap with negative width is not allowed");

        uint source_stride = StructUtil.CalcStride(nbits, width);
        uint dataOffset = hasFileHeader ? fh.bfOffBits : (uint)offset;
        uint dataSize = bi.bV5SizeImage > 0 ? bi.bV5SizeImage : (source_stride * (uint)height);

        if (dataOffset + dataSize > sourceLength)
            throw new InvalidOperationException(ERR_HEOF);

        var profileSize = bi.bV5ProfileSize;
        uint profileOffset = (hasFileHeader ? (uint)size_fh : 0) + bi.bV5ProfileData;

        if (profileOffset + profileSize > sourceLength)
            throw new InvalidOperationException(ERR_HEOF);

        var masks = new BITMASKS
        {
            maskRed = maskR,
            maskGreen = maskG,
            maskBlue = maskB,
            maskAlpha = maskA,
        };

        var fmt = BitmapCorePixelFormat.Formats.SingleOrDefault(f => f.IsMatch(nbits, masks));

        // currently we only support RLE -> Bgra32
        if (bi.bV5Compression == BitmapCompressionMode.BI_RLE4 || bi.bV5Compression == BitmapCompressionMode.BI_RLE8 || bi.bV5Compression == BitmapCompressionMode.OS2_RLE24)
            fmt = null;

        double pixelPerMeterToDpi(int pels)
        {
            if (pels == 0) return 96;
            return pels * 0.0254d;
        }

        mscms.SafeProfileHandle clrsource = null;
        mscms.mscmsIntent clrintent = mscms.mscmsIntent.INTENT_PERCEPTUAL;

        bool ignoreColorProfile = (bcrFlags & BC_READ_IGNORE_COLOR_PROFILE) > 0;
        if (!ignoreColorProfile)
        {
            try
            {
                if (bi.bV5CSType == ColorSpaceType.LCS_CALIBRATED_RGB) clrsource = mscms.CreateProfileFromLogicalColorSpace(bi);
                else if (bi.bV5CSType == ColorSpaceType.PROFILE_EMBEDDED) clrsource = mscms.OpenProfile((source + profileOffset), profileSize);

                switch (bi.bV5Intent)
                {
                    case Bv5Intent.LCS_GM_BUSINESS:
                        clrintent = mscms.mscmsIntent.INTENT_RELATIVE_COLORIMETRIC;
                        break;
                    case Bv5Intent.LCS_GM_GRAPHICS:
                        clrintent = mscms.mscmsIntent.INTENT_SATURATION;
                        break;
                    case Bv5Intent.LCS_GM_ABS_COLORIMETRIC:
                        clrintent = mscms.mscmsIntent.INTENT_ABSOLUTE_COLORIMETRIC;
                        break;
                }

                if (clrsource != null && fmt != null && fmt.MscmsFormat.HasValue && fmt.IsIndexed)
                {
                    // transform color table if indexed image
                    palette = mscms.TransformColorsTo_sRGB(clrsource, palette, clrintent);
                }
            }
            catch
            {
                // ignore color profile errors
            }
        }

        info = new BITMAP_READ_DETAILS
        {
            dibHeader = bi,
            bbp = nbits,
            compression = bi.bV5Compression,
            dpiX = pixelPerMeterToDpi(bi.bV5XPelsPerMeter),
            dpiY = pixelPerMeterToDpi(bi.bV5YPelsPerMeter),

            cMasks = masks,
            cIndexed = smBit,
            cTrueAlpha = hasAlphaChannel,

            imgColorTable = palette,
            imgHeight = height,
            imgTopDown = topDown,
            imgWidth = width,
            imgDataOffset = dataOffset,
            imgDataSize = dataSize,
            imgStride = source_stride,
            imgSourceFmt = fmt,

            colorProfile = clrsource,
            colorProfileIntent = clrintent,
        };
    }

    public static void ReadPixels(ref BITMAP_READ_DETAILS info, BitmapCorePixelFormat imgDestFmt, byte* sourceBufferStart, byte* destBufferStart, uint bcrFlags)
    {
        bool forcebgra32 = (bcrFlags & BC_READ_FORCE_BGRA32) > 0;
        bool preserveFormat = (bcrFlags & BC_READ_STRICT_PRESERVE_FORMAT) > 0;
        bool preserveAlpha = (bcrFlags & BC_READ_PRESERVE_INVALID_ALPHA) > 0;
        bool ignoreColorProfile = (bcrFlags & BC_READ_IGNORE_COLOR_PROFILE) > 0;

        if (preserveFormat && (info.imgSourceFmt != imgDestFmt))
            throw new NotSupportedException("StrictPreserveFormat was set while the source and/or target format is not supported.");

        if (forcebgra32 && imgDestFmt != BitmapCorePixelFormat.Bgra32)
            throw new InvalidOperationException("ForceBGRA32 was set but this is not supported with the source pixel format.");

        if (imgDestFmt == null)
            throw new ArgumentNullException(nameof(imgDestFmt));

        var compr = info.compression;
        if (compr == BitmapCompressionMode.BI_JPEG || compr == BitmapCompressionMode.BI_PNG)
        {
            throw new NotSupportedException("BI_JPEG and BI_PNG passthrough compression is not supported.");
        }
        else if (compr == BitmapCompressionMode.BI_RLE4 || compr == BitmapCompressionMode.BI_RLE8 || compr == BitmapCompressionMode.OS2_RLE24)
        {
            if (imgDestFmt != BitmapCorePixelFormat.Bgra32) throw new NotSupportedException("RLE only supports being translated to Bgra32");
            BitmapCorePixelReader.ReadRLE_32(ref info, sourceBufferStart, destBufferStart);
        }
        else if (compr == BitmapCompressionMode.OS2_HUFFMAN1D)
        {
            BitmapCorePixelReader.ReadHuffmanG31D(ref info, sourceBufferStart, destBufferStart);
        }
        else if (info.imgSourceFmt == imgDestFmt && info.cTrueAlpha == imgDestFmt.HasAlpha)
        {
            // if the source is a known/supported/standard format, we can basically just copy the buffer straight over with no further processing
            if (info.imgTopDown)
            {
                var size = info.imgStride * info.imgHeight;
                Buffer.MemoryCopy(sourceBufferStart, destBufferStart, size, size);
            }
            else
            {
                uint stride = info.imgStride;
                int y, height = info.imgHeight, h = height;
                while (--h >= 0)
                {
                    y = height - h - 1;
                    byte* sourceln = sourceBufferStart + (y * stride);
                    byte* destln = destBufferStart + (h * stride);
                    Buffer.MemoryCopy(sourceln, destln, stride, stride);
                }
            }
        }
        else if (info.bbp <= 8)
        {
            if (imgDestFmt != BitmapCorePixelFormat.Bgra32)
                throw new NotSupportedException("RLE only supports being translated to Bgra32");

            BitmapCorePixelReader.ReadIndexedTo32(ref info, sourceBufferStart, destBufferStart);
        }
        else if (info.bbp > 8)
        {
            BitmapCorePixelReader.ConvertChannelBGRA(ref info, imgDestFmt, sourceBufferStart, destBufferStart, preserveAlpha);
        }
        else
        {
            throw new NotSupportedException("Pixel format / compression not supported");
        }

        // translate pixels to sRGB via embedded color profile
        if (!ignoreColorProfile && info.colorProfile != null && !info.colorProfile.IsInvalid && !imgDestFmt.IsIndexed && imgDestFmt.MscmsFormat.HasValue)
        {
            try
            {
                uint stride = StructUtil.CalcStride(imgDestFmt.BitsPerPixel, info.imgWidth);
                mscms.TransformPixelsTo_sRGB(info.colorProfile, imgDestFmt.MscmsFormat.Value, destBufferStart, info.imgWidth, info.imgHeight, stride, info.colorProfileIntent);
            }
            catch
            {
                // ignore color transformation errors
            }
        }
    }

    public static byte[] WriteToBMP(ref BITMAP_WRITE_REQUEST info, byte* sourceBufferStart, BitmapCorePixelFormat fmt, uint bcwFlags)
    {
        return WriteToBMP(ref info, sourceBufferStart, fmt.Masks, fmt.BitsPerPixel, bcwFlags);
    }

    public static byte[] WriteToBMP(ref BITMAP_WRITE_REQUEST info, byte* sourceBufferStart, BITMASKS masks, ushort nbits, uint bcwFlags)
    {
        bool skipFileHeader = (bcwFlags & BC_WRITE_SKIP_FH) > 0;
        bool forceV5 = (bcwFlags & BC_WRITE_V5) > 0;
        bool forceInfo = (bcwFlags & BC_WRITE_VINFO) > 0;

        if (forceV5 && forceInfo)
            throw new ArgumentException("ForceV5 and ForceInfo flags can not be used at the same time.");

        // NOT SUPPORTED RIGHT NOW
        bool iccEmbed = false;
        byte[] iccProfileData = new byte[0];

        var hasAlpha = masks.maskAlpha != 0;

        if (nbits < 16 && (info.imgColorTable == null || info.imgColorTable.Length == 0))
            throw new InvalidOperationException("A indexed bitmap must have a color table / palette.");

        //int dpiToPelsPM(double dpi)
        //{
        //    if (Math.Round(dpi) == 96d) return 0;
        //    return (int)Math.Round(dpi / 0.0254d);
        //}

        uint paletteSize = info.imgColorTable == null ? 0 : (uint)info.imgColorTable.Length;

        var fhSize = (uint)Marshal.SizeOf<BITMAPFILEHEADER>();
        var quadSize = (uint)Marshal.SizeOf<RGBQUAD>();

        byte[] buffer;
        uint pxOffset, pxSize;

        // BI_BITFIELDS is not valid for 24bpp, so if the masks are not RGB we need to use a V5 header.
        //var nonStandard24bpp = nbits == 24 && !BitmapCorePixelFormat2.Bgr24.IsMatch(24, masks);

        BitmapCompressionMode compr = BitmapCompressionMode.BI_RGB;

        // some parsers do not respect the v5 header masks unless BI_BITFIELDS is used...
        // this is true of Chrome (only for 16bpp) and is also true of FireFox (16 and 32bpp)
        if (nbits == 16 || nbits == 32)
            compr = BitmapCompressionMode.BI_BITFIELDS;

        // write V5 header if embedded color profile or has alpha data
        if (forceV5 || hasAlpha || iccEmbed)
        {
            var v5Size = (uint)Marshal.SizeOf<BITMAPV5HEADER>();
            // Typical structure:
            // - BITMAPFILEHEADER (Optional)
            // - BITMAPV5HEADER
            // - * Note, never write BI_BITFIELDS at the end of a V5 header, these masks are contained within the header itself
            // - Color Table (Optional)
            // - Pixel Data
            // - Embedded Color Profile (Optional)

            var fh = new BITMAPFILEHEADER
            {
                bfType = BFH_BM,
            };

            var v5 = new BITMAPV5HEADER
            {
                bV5Size = v5Size,
                bV5Planes = 1,
                bV5BitCount = nbits,
                bV5Height = info.imgTopDown ? -info.imgHeight : info.imgHeight,
                bV5Width = info.imgWidth,
                bV5Compression = compr,
                bV5XPelsPerMeter = 0,
                bV5YPelsPerMeter = 0,

                bV5RedMask = masks.maskRed,
                bV5GreenMask = masks.maskGreen,
                bV5BlueMask = masks.maskBlue,
                bV5AlphaMask = masks.maskAlpha,

                bV5ClrImportant = paletteSize,
                bV5ClrUsed = paletteSize,
                bV5SizeImage = (uint)(info.imgStride * info.imgHeight),

                bV5CSType = ColorSpaceType.LCS_sRGB,
                bV5Intent = Bv5Intent.LCS_GM_IMAGES,
            };

            uint offset = skipFileHeader ? 0 : fhSize;
            offset += v5Size;
            offset += paletteSize * quadSize;

            // fh offset points to beginning of pixel data
            fh.bfOffBits = pxOffset = offset;
            pxSize = v5.bV5SizeImage;

            offset += v5.bV5SizeImage;

            if (iccEmbed)
            {
                v5.bV5CSType = ColorSpaceType.PROFILE_EMBEDDED;
                v5.bV5ProfileData = offset;
                v5.bV5ProfileSize = (uint)iccProfileData.Length;
                offset += v5.bV5ProfileSize;
            }

            // fh size must be total file size
            fh.bfSize = offset;

            buffer = new byte[offset];
            offset = 0;

            if (!skipFileHeader)
                StructUtil.SerializeTo(fh, buffer, ref offset);

            StructUtil.SerializeTo(v5, buffer, ref offset);

            if (info.imgColorTable != null)
                foreach (var p in info.imgColorTable)
                    StructUtil.SerializeTo(p, buffer, ref offset);

            Marshal.Copy((IntPtr)sourceBufferStart, buffer, (int)offset, (int)v5.bV5SizeImage);
            offset += v5.bV5SizeImage;

            if (iccEmbed)
                Buffer.BlockCopy(iccProfileData, 0, buffer, (int)offset, iccProfileData.Length);
        }
        else
        {
            var infoSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
            // Typical structure:
            // - BITMAPFILEHEADER (Optional)
            // - BITMAPINFOHEADER
            // - BI_BITFIELDS (Optional)
            // - Color Table (Optional)
            // - Pixel Data

            // this would be ideal, we can specify transparency in VINFO headers... but many applications incl FireFox do not support this.
            // if (hasAlpha) compr = BitmapCompressionMode.BI_ALPHABITFIELDS;

            var fh = new BITMAPFILEHEADER
            {
                bfType = BFH_BM,
            };

            var vinfo = new BITMAPINFOHEADER
            {
                bV5Size = infoSize,
                bV5Planes = 1,
                bV5BitCount = nbits,
                bV5Height = info.imgTopDown ? -info.imgHeight : info.imgHeight,
                bV5Width = info.imgWidth,
                bV5Compression = compr,
                bV5XPelsPerMeter = 0,
                bV5YPelsPerMeter = 0,

                bV5ClrImportant = paletteSize,
                bV5ClrUsed = paletteSize,
                bV5SizeImage = (uint)(info.imgStride * info.imgHeight),
            };

            uint offset = skipFileHeader ? 0 : fhSize;
            offset += infoSize;

            if (compr == BitmapCompressionMode.BI_BITFIELDS)
                offset += sizeof(uint) * 3;

            offset += paletteSize * quadSize;

            // fh offset points to beginning of pixel data
            fh.bfOffBits = pxOffset = offset;
            pxSize = vinfo.bV5SizeImage;

            offset += vinfo.bV5SizeImage;

            // fh size must be total file size
            fh.bfSize = offset;

            buffer = new byte[offset];
            offset = 0;

            if (!skipFileHeader)
                StructUtil.SerializeTo(fh, buffer, ref offset);

            StructUtil.SerializeTo(vinfo, buffer, ref offset);

            if (compr == BitmapCompressionMode.BI_BITFIELDS)
            {
                Buffer.BlockCopy(masks.BITFIELDS(), 0, buffer, (int)offset, sizeof(uint) * 3);
                offset += sizeof(uint) * 3;
            }

            if (info.imgColorTable != null)
                foreach (var p in info.imgColorTable)
                    StructUtil.SerializeTo(p, buffer, ref offset);

            Marshal.Copy((IntPtr)sourceBufferStart, buffer, (int)offset, (int)vinfo.bV5SizeImage);
        }

        return buffer;
    }
}
