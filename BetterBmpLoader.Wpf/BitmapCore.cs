using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BetterBmpLoader
{
    // http://zig.tgschultz.com/bmp_file_format.txt
    // http://paulbourke.net/dataformats/bitmaps/
    // http://www.libertybasicuniversity.com/lbnews/nl100/format.htm
    // https://www.displayfusion.com/Discussions/View/converting-c-data-types-to-c/?ID=38db6001-45e5-41a3-ab39-8004450204b3
    // https://github.com/FlyingPumba/tp2-orga2/blob/master/entregable/src/bmp/bmp.c

    internal unsafe partial class BitmapCore
    {
        private const ushort BFH_BM = 0x4D42;

        private const string
            ERR_HEOF = "Bitmap stream ended while parsing header, but more data was expected. This usually indicates an malformed file or empty data stream.";

        private static int CalculateBitShift(uint mask)
        {
            for (int shift = 0; shift < sizeof(uint) * 8; ++shift)
            {
                if ((mask & (1 << shift)) != 0)
                {
                    return shift;
                }
            }
            throw new NotSupportedException("Invalid Bit Mask");
        }

        public static void ReadHeader(byte* source, int sourceLength, out BITMAP_READ_DETAILS info)
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

            //if (nbits != 32 && nbits != 24 &&  && nbits != 16)
            //    throw new NotSupportedException($"Bitmaps with bpp of '{nbits}' are not supported. Expected 16, 24, or 32.");

            //if (bi.bV5Planes != 1)
            //    throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

            // we don't support linked profiles, custom windows profiles, etc - so default to sRGB instead of throwing...
            bi.bV5CSType = bi.bV5CSType == ColorSpaceType.PROFILE_EMBEDDED ? ColorSpaceType.PROFILE_EMBEDDED : ColorSpaceType.LCS_sRGB;

            //if (bi.bV5CSType != ColorSpaceType.LCS_sRGB && bi.bV5CSType != ColorSpaceType.LCS_WINDOWS_COLOR_SPACE && bi.bV5CSType != ColorSpaceType.PROFILE_EMBEDDED && bi.bV5CSType != ColorSpaceType.LCS_CALIBRATED_RGB)
            //    throw new NotSupportedException($"Bitmap with header size '{header_size}' and color space of '{bi.bV5CSType.ToString()}' is not supported.");

            uint maskR = 0;
            uint maskG = 0;
            uint maskB = 0;
            uint maskA = 0;

            bool hasAlphaChannel = false;
            bool skipVerifyBppAndMasks = false;

            switch (bi.bV5Compression)
            {
                case BitmapCompressionMode.BI_BITFIELDS:
                    // larger headers contain the BI_BITFIELDS masks within them and are not expected to write them after the header
                    if (header_size > 40)
                        break;

                    // 3 x u32 if OS2v1 or WindowsV2
                    // 4 x u32 if any other version??? find anywhere to confirm this?

                    if (is_os21x_)
                    {
                        var rgb = StructUtil.Deserialize<MASKTRIPLE>(ptr);
                        maskR = rgb.rgbRed;
                        maskG = rgb.rgbGreen;
                        maskB = rgb.rgbBlue;
                        offset += Marshal.SizeOf<MASKTRIPLE>();
                    }
                    else
                    {
                        var rgb = StructUtil.Deserialize<MASKQUAD>(ptr);
                        maskR = rgb.rgbRed;
                        maskG = rgb.rgbGreen;
                        maskB = rgb.rgbBlue;
                        // we ignore the alpha mask here as it's invalid with BI_BITFIELDS
                        offset += Marshal.SizeOf<MASKQUAD>();
                    }

                    break;
                case BitmapCompressionMode.BI_ALPHABITFIELDS:

                    // larger headers contain the BI_BITFIELDS masks within them and are not expected to write them after the header
                    if (header_size > 40)
                        break;

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
                            // we could check for transparency in 16b RGB but it is slower and is very uncommon
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

            // lets use the v3/v4/v5 masks if present instead of BITFIELDS or RGB
            if (bi.bV5Size >= 52)
            {
                if (bi.bV5RedMask != 0) maskR = bi.bV5RedMask;
                if (bi.bV5BlueMask != 0) maskB = bi.bV5BlueMask;
                if (bi.bV5GreenMask != 0) maskG = bi.bV5GreenMask;
            }
            if (bi.bV5Size >= 56 && bi.bV5AlphaMask != 0)
            {
                maskA = bi.bV5AlphaMask;
                hasAlphaChannel = true;
            }

            bool smBit = nbits == 1 || nbits == 2 || nbits == 4 || nbits == 8;
            bool lgBit = nbits == 16 || nbits == 24 || nbits == 32;

            if (!skipVerifyBppAndMasks)
            {
                if (!lgBit && !smBit)
                    throw new NotSupportedException($"Bitmap with bits per pixel of '{nbits}' are not valid.");

                if (lgBit && (maskR == 0 || maskB == 0 || maskG == 0))
                    throw new NotSupportedException($"Bitmap (bbp {nbits}) color masks could not be determined, this usually indicates a malformed bitmap file.");
            }

            // The number of entries in the palette is either 2n (where n is the number of bits per pixel) or a smaller number specified in the header
            // always allocate at least 256 entries so we can ignore bad data which seeks past the end of palette data.
            var pallength = nbits < 16 ? (1 << nbits) : 0;
            if (bi.bV5ClrUsed > 0)
                pallength = (int)bi.bV5ClrUsed;

            //var bitsperpal = is_os21x_ ? 3 : 4;
            //var palmax = (data.Length - offset - bi.bV5SizeImage) / bitsperpal;

            if (pallength > 65536) // technically the max is 256..? some bitmaps have invalidly large palettes
                throw new NotSupportedException("Bitmap has an oversized/invalid color palette.");

            RGBQUAD[] palette = new RGBQUAD[pallength];
            var clrSize = is_os21x_ ? Marshal.SizeOf<RGBTRIPLE>() : Marshal.SizeOf<RGBQUAD>();
            for (int i = 0; i < palette.Length; i++)
            {
                if (is_os21x_)
                {
                    var small = StructUtil.Deserialize<RGBTRIPLE>(ptr);
                    palette[i] = new RGBQUAD { rgbBlue = small.rgbBlue, rgbGreen = small.rgbGreen, rgbRed = small.rgbRed };
                    ptr += clrSize;
                }
                else
                {
                    palette[i] = StructUtil.Deserialize<RGBQUAD>(ptr);
                    ptr += clrSize;
                }
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

            uint source_stride = (nbits * (uint)width + 31) / 32 * 4; // = width * (nbits / 8) + (width % 4); // (width * nbits + 7) / 8;
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

            var fmt = BitmapCorePixelFormat2.Formats.SingleOrDefault(f => f.IsMatch(nbits, masks));

            // currently we only support RLE -> Bgra32
            if (bi.bV5Compression == BitmapCompressionMode.BI_RLE4 || bi.bV5Compression == BitmapCompressionMode.BI_RLE8 || bi.bV5Compression == BitmapCompressionMode.OS2_RLE24)
                fmt = null;

            double pixelPerMeterToDpi(int pels)
            {
                if (pels == 0) return 96;
                return pels * 0.0254d;
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
                imgFmt = fmt,

                iccProfileSize = profileSize,
                iccProfileOffset = profileOffset,
                iccProfileType = bi.bV5CSType,
                iccProfileIntent = bi.bV5Intent,
            };
        }

        public static void ReadPixels(ref BITMAP_READ_DETAILS info, BitmapCorePixelFormat2 toFmt, byte* sourceBufferStart, byte* destBufferStart, bool preserveFakeAlpha)
        {
            var compr = info.compression;
            if (compr == BitmapCompressionMode.BI_JPEG || compr == BitmapCompressionMode.BI_PNG)
            {
                throw new NotSupportedException("BI_JPEG and BI_PNG passthrough compression is not supported.");
            }

            if (compr == BitmapCompressionMode.BI_RLE4 || compr == BitmapCompressionMode.BI_RLE8 || compr == BitmapCompressionMode.OS2_RLE24)
            {
                if (toFmt != BitmapCorePixelFormat2.Bgra32)
                    throw new NotSupportedException("RLE only currently supports being translated to Bgra32");

                ReadRLE_32(ref info, sourceBufferStart, destBufferStart);
                return;
            }

            if (compr == BitmapCompressionMode.OS2_HUFFMAN1D)
            {
                ReadHuffmanG31D(ref info, sourceBufferStart, destBufferStart);
                return;
            }

            if (info.imgFmt == toFmt && info.cTrueAlpha == toFmt.HasAlpha)
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
            else
            {
                ReadBGRA_162432(ref info, toFmt.Write, sourceBufferStart, destBufferStart, preserveFakeAlpha);
            }
        }

        private static void ReadBGRA_162432(ref BITMAP_READ_DETAILS info, WritePixelToPtr write, byte* sourceBufferStart, byte* destBufferStart, bool preserveFakeAlpha)
        {
            if (write == null)
                throw new ArgumentNullException(nameof(write));

            var nbits = info.bbp;
            var width = info.imgWidth;
            var height = info.imgHeight;
            var upside_down = info.imgTopDown;
            var hasAlphaChannel = info.cTrueAlpha;

            var maskR = info.cMasks.maskRed;
            var maskG = info.cMasks.maskGreen;
            var maskB = info.cMasks.maskBlue;
            var maskA = info.cMasks.maskAlpha;

            int shiftR = 0, shiftG = 0, shiftB = 0, shiftA = 0;
            uint maxR = 0, maxG = 0, maxB = 0, maxA = 0;
            uint multR = 0, multG = 0, multB = 0, multA = 0;

            if (maskR != 0)
            {
                shiftR = CalculateBitShift(maskR);
                maxR = maskR >> shiftR;
                multR = (uint)(Math.Ceiling(255d / maxR * 65536 * 256)); // bitshift << 24
            }

            if (maskG != 0)
            {
                shiftG = CalculateBitShift(maskG);
                maxG = maskG >> shiftG;
                multG = (uint)(Math.Ceiling(255d / maxG * 65536 * 256));
            }

            if (maskB != 0)
            {
                shiftB = CalculateBitShift(maskB);
                maxB = maskB >> shiftB;
                multB = (uint)(Math.Ceiling(255d / maxB * 65536 * 256));
            }

            if (maskA != 0)
            {
                shiftA = CalculateBitShift(maskA);
                maxA = maskA >> shiftA;
                multA = (uint)(Math.Ceiling(255d / maxA * 65536 * 256)); // bitshift << 24
            }

            int source_stride = (nbits * width + 31) / 32 * 4; // = width * (nbits / 8) + (width % 4); // (width * nbits + 7) / 8;
            int dest_stride = info.imgWidth * 4;

            restartLoop:

            byte b, r, g, a;
            uint i32;
            byte* source, dest;
            int y, w, h = height, nbytes = (nbits / 8);

            if (hasAlphaChannel)
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = destBufferStart + ((upside_down ? y : h) * dest_stride);
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i32 = *(uint*)source;

                        b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                        g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                        r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                        a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);

                        dest = write(dest, b, g, r, a);
                        source += nbytes;
                    }
                }
            }
            else if (maskA != 0 && preserveFakeAlpha) // hasAlpha = false, and maskA != 0 - we might have _fake_ alpha.. need to check for it
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = destBufferStart + ((upside_down ? y : h) * dest_stride);
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i32 = *(uint*)source;

                        b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                        g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                        r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                        a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);

                        if (a != 0)
                        {
                            // this BMP should not have an alpha channel, but windows likes doing this and we need to detect it
                            hasAlphaChannel = true;
                            goto restartLoop;
                        }

                        dest = write(dest, b, g, r, 0xFF);
                        source += nbytes;
                    }
                }
            }
            else  // simple bmp, no transparency
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = destBufferStart + ((upside_down ? y : h) * dest_stride);
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i32 = *(uint*)source;
                        b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                        g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                        r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);

                        dest = write(dest, b, g, r, 0xFF);
                        source += nbytes;
                    }
                }
            }
        }

        private static void ReadRLE_32(ref BITMAP_READ_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
        {
            var is4 = info.compression == BitmapCompressionMode.BI_RLE4 && info.bbp == 4;
            var is8 = info.compression == BitmapCompressionMode.BI_RLE8 && info.bbp == 8;
            var is24 = info.compression == BitmapCompressionMode.OS2_RLE24 && info.bbp == 24;

            if (!is4 && !is8 && !is24)
                throw new NotSupportedException($"RLE invalid bpp. Compression mode was {info.compression} and bpp was {info.bbp}");

            int dest_stride = info.imgWidth * 4;
            int height = info.imgHeight, width = info.imgWidth;

            var sourceBufferEnd = sourceBufferStart + info.imgDataSize;
            var destBufferEnd = destBufferStart + (dest_stride * height);

            byte* ptr = sourceBufferStart;
            var pal = info.imgColorTable.Length;
            var palette = info.imgColorTable;
            ushort nbits = info.bbp;

            uint* dest;
            byte op1, op2;
            int y = 0, x = 0;

            void chksrc(int bytes)
            {
                if ((ptr + bytes) > sourceBufferEnd) throw new InvalidOperationException("Invalid RLE Compression, source outside of bounds.");
            }

            void chkdst(int bytes)
            {
                if ((((byte*)dest) + bytes) > destBufferEnd) throw new InvalidOperationException("Invalid RLE Compression, dest of bounds.");
            }

            void chkspace(int srcBytes, int destBytes)
            {
                chksrc(srcBytes);
                chkdst(destBytes);
            }

            while ((ptr + 2) <= sourceBufferEnd)
            {
                dest = (uint*)(destBufferStart + ((height - y - 1) * dest_stride));
                dest += x;

                op1 = *ptr++;
                if (op1 == 0)
                {
                    op2 = *ptr++;
                    if (op2 == 0) // end of line
                    {
                        y++;
                        x = 0;
                        if (y > height) throw new InvalidOperationException("Invalid RLE Compression");
                    }
                    else if (op2 == 1) // end of file
                    {
                        break;
                    }
                    else if (op2 == 2) // delta offset, next two bytes indicate how to translate the current position
                    {
                        chksrc(2);
                        x += *ptr++;
                        y += *ptr++;
                    }
                    else // absolute mode, op2 indicates how many pixels to read and copy to dest
                    {
                        int read = 0;
                        if (nbits == 4)
                        {
                            byte dec8;
                            uint px1 = 0, px2 = 0;
                            RGBQUAD c1, c2;
                            chkspace(op2 / 2, op2 * 4);

                            for (int k = 0; k < op2; k++)
                            {
                                if ((k % 2) == 0)
                                {
                                    dec8 = *ptr++;
                                    read++;
                                    c1 = palette[((byte)((dec8 & 0xF0) >> 4)) % pal];
                                    c2 = palette[((byte)(dec8 & 0x0F)) % pal];
                                    px1 = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                                    px2 = (uint)((c2.rgbBlue) | (c2.rgbGreen << 8) | (c2.rgbRed << 16) | (0xFF << 24));
                                }
                                else
                                {
                                    px1 = px2;
                                }

                                *dest++ = px1;
                                x++;
                            }
                        }
                        else if (nbits == 8)
                        {
                            byte dec8;
                            RGBQUAD c1;
                            chkspace(op2, op2 * 4);

                            for (int k = 0; k < op2; k++)
                            {
                                dec8 = *ptr++;
                                read++;
                                c1 = palette[dec8 % pal];
                                *dest++ = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                                x++;
                            }
                        }
                        else if (nbits == 24)
                        {
                            uint dec24;
                            chkspace(1 + (op2 * 3), op2 * 4);

                            for (int k = 0; k < op2; k++)
                            {
                                dec24 = *((uint*)ptr);
                                read += 3;
                                ptr += 3;
                                *dest++ = (uint)(dec24 | (0xFF << 24));
                                x++;
                            }
                        }

                        if ((read & 0x01) > 0) ptr++; // padding to WORD
                    }
                }
                else // encoded mode, duplicate op2, op1 times.
                {
                    RGBQUAD c1, c2;
                    uint dec, px1 = 0, px2 = 0;
                    if (nbits == 4)
                    {
                        chksrc(1);
                        op2 = *ptr++;
                        c1 = palette[((op2 & 0xF0) >> 4) % pal];
                        c2 = palette[(op2 & 0x0F) % pal];
                        px1 = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                        px2 = (uint)((c2.rgbBlue) | (c2.rgbGreen << 8) | (c2.rgbRed << 16) | (0xFF << 24));
                    }
                    else if (nbits == 8)
                    {
                        chksrc(1);
                        op2 = *ptr++;
                        c1 = palette[op2 % pal];
                        px1 = px2 = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                    }
                    else if (nbits == 24)
                    {
                        chksrc(4);
                        dec = *((uint*)ptr);
                        ptr += 3;
                        px1 = px2 = (uint)(dec | (0xFF << 24));
                    }

                    chkdst(op1 * 4);
                    for (int l = 0; l < op1 && x < width; l++)
                    {
                        *dest++ = l % 2 == 0 ? px1 : px2;
                        x++;
                    }
                }
            }
        }

        private static void ReadHuffmanG31D(ref BITMAP_READ_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
        {
            int codeWord = 0; // current code word
            int bitsAvailable = 0; // current number of bits available in code word
            uint stride = info.imgStride;
            var bit_width = info.imgWidth * 8;
            var width = info.imgWidth;
            var h = info.imgHeight;
            byte* sourcePtr = sourceBufferStart;
            uint dataAvailable = info.imgDataSize;

            var firstCode = true;
            bool ReadCode(G31DFaxCode[] table, ref G31DFaxCode fc)
            {
                // table should already be ordered by bitlength
                foreach (var c in table)
                {
                    // we need to read more bits
                    while (bitsAvailable < c.bitLength)
                    {
                        // there is no more data available to read
                        if (dataAvailable == 0)
                            return false;

                        // read data and add it to the lower order code word bits
                        codeWord <<= 8;
                        codeWord |= *sourcePtr++;
                        bitsAvailable += 8;
                        dataAvailable--;
                    }

                    if (c.code == codeWord >> (bitsAvailable - c.bitLength))
                    {
                        // we found a match, lets remove bitLength bits from the high side of code word
                        bitsAvailable -= c.bitLength;
                        int mask = (1 << bitsAvailable) - 1;
                        codeWord = codeWord & mask;

                        if (firstCode && c.runLength < 0)
                        {
                            // sometimes the data stream starts with an EOL code, lets skip / ignore it
                            firstCode = false;
                            return ReadCode(table, ref fc);
                        }

                        firstCode = false;
                        fc = c;
                        return true;
                    }
                }

                return false;
            }

            G31DFaxCode code = default;

            while (--h >= 0)
            {
                byte* lineStart = destBufferStart + (h * stride);
                var x = 0;

                while (true)
                {
                    // each line alternates with white and black runs, starting with white. 
                    // according to spec, the line total run length must equal the image width - but we do not enforce this.

                    while (true) // WHITE
                    {
                        if (!ReadCode(CCITTHuffmanG31D.WhiteCodes, ref code))
                            return;

                        x += code.runLength; // white is always zeros, no need to write anything.

                        if (code.runLength < 64) break; // EOL or TERMINATING CODE
                    }

                    if (code.runLength < 0) break; // EOL

                    while (true) // BLACK
                    {
                        if (!ReadCode(CCITTHuffmanG31D.BlackCodes, ref code))
                            return;

                        if (code.runLength > 0)
                        {
                            byte* run = (lineStart + (x / 8));
                            var runLength = code.runLength;

                            // our current position may not be aligned with a byte boundary
                            // if it's not, we need to fill up the end of the current byte
                            var mremain = x % 8;
                            if (mremain > 0)
                            {
                                var mshift = 8 - mremain - 1;
                                while (runLength > 0 && mshift >= 0)
                                {
                                    byte toadd = (byte)(1 << mshift);
                                    *run |= toadd;
                                    runLength--;
                                    mshift--;
                                    x++;
                                }
                                run++;
                            }

                            // write whole / aligned bytes
                            while (runLength >= 8)
                            {
                                *run = 0xFF;
                                x += 8;
                                runLength -= 8;
                                run++;
                            }

                            // write any remaining misaligned bits at the end to the beginning of the last byte.
                            for (; runLength > 0; runLength--)
                            {
                                *run |= (byte)(1 << (8 - runLength));
                                x++;
                            }
                        }

                        if (code.runLength < 64) break; // EOL or TERMINATING CODE
                    }

                    if (code.runLength < 0) break; // EOL
                }
            }
        }

        public static byte[] WriteToBMP(ref BITMAP_WRITE_REQUEST info, byte* sourceBufferStart, BitmapCorePixelFormat2 fmt)
        {
            return WriteToBMP(ref info, sourceBufferStart, fmt.Masks, fmt.BitsPerPixel, fmt.LcmsFormat);
        }

        public static byte[] WriteToBMP(ref BITMAP_WRITE_REQUEST info, byte* sourceBufferStart, BITMASKS masks, ushort nbits, uint lcmspxFmt)
        {
            bool renderFileHeader = info.headerIncludeFile;
            bool iccEmbed = info.iccEmbed;
            byte[] iccProfileData = info.iccProfileData;

            bool hasIcc = iccProfileData != null && iccProfileData.Length > 0;

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
            if (info.headerType != BitmapCoreHeaderType.ForceVINFO && (info.headerType == BitmapCoreHeaderType.ForceV5 || hasAlpha || (iccEmbed && hasIcc)))
            {
                Console.WriteLine("V5");
                var v5Size = (uint)Marshal.SizeOf<BITMAPV5HEADER>();
                // Typical structure:
                // - BITMAPFILEHEADER (Optional)
                // - BITMAPV5HEADER
                // - BI_BITFIELDS (Optional)
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

                uint offset = renderFileHeader ? fhSize : 0;
                offset += v5Size;

                if (compr == BitmapCompressionMode.BI_BITFIELDS)
                    offset += sizeof(uint) * 3;

                offset += paletteSize * quadSize;

                // fh offset points to beginning of pixel data
                fh.bfOffBits = pxOffset = offset;
                pxSize = v5.bV5SizeImage;

                offset += v5.bV5SizeImage;

                if (iccEmbed && hasIcc)
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

                if (renderFileHeader)
                    StructUtil.SerializeTo(fh, buffer, ref offset);

                StructUtil.SerializeTo(v5, buffer, ref offset);

                if (compr == BitmapCompressionMode.BI_BITFIELDS)
                {
                    Buffer.BlockCopy(masks.BITFIELDS(), 0, buffer, (int)offset, sizeof(uint) * 3);
                    offset += sizeof(uint) * 3;
                }

                if (info.imgColorTable != null)
                    foreach (var p in info.imgColorTable)
                        StructUtil.SerializeTo(p, buffer, ref offset);

                Marshal.Copy((IntPtr)sourceBufferStart, buffer, (int)offset, (int)v5.bV5SizeImage);
                offset += v5.bV5SizeImage;

                if (iccEmbed && hasIcc)
                    Buffer.BlockCopy(iccProfileData, 0, buffer, (int)offset, iccProfileData.Length);
            }
            else
            {
                Console.WriteLine("VINFO");
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

                uint offset = renderFileHeader ? fhSize : 0;
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

                if (renderFileHeader)
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

            if (lcmspxFmt > 0 && hasIcc && !iccEmbed) // convert to sRGB if we've been given a color profile and have been instructed not to embed it
            {
                fixed (byte* bufferPtr = buffer)
                fixed (byte* profilePtr = iccProfileData)
                    Lcms.TransformEmbeddedPixelFormat(lcmspxFmt, profilePtr, (uint)iccProfileData.Length, (bufferPtr + pxOffset),
                        info.imgWidth, info.imgHeight, (int)info.imgStride, Bv5Intent.LCS_GM_IMAGES);
            }

            return buffer;
        }
    }

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
        public bool IsIndexed => BitsPerPixel < 16;
        public bool HasAlpha => Masks.maskAlpha != 0;

        public uint LcmsFormat { get; private set; }
        public BITMASKS Masks { get; private set; }
        public WritePixelToPtr Write { get; private set; }
        public ushort BitsPerPixel { get; private set; }

        private BitmapCorePixelFormat2() { }

        public static readonly BitmapCorePixelFormat2 Indexed1 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 1,
        };

        public static readonly BitmapCorePixelFormat2 Indexed2 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 2,
        };

        public static readonly BitmapCorePixelFormat2 Indexed4 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 4,
        };

        public static readonly BitmapCorePixelFormat2 Indexed8 = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 8,
        };

        public static readonly BitmapCorePixelFormat2 Bgr555X = new BitmapCorePixelFormat2
        {
            BitsPerPixel = 16,
            Masks = BitFields.BITFIELDS_BGRA_555X,
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
            Bgr555X,
            Bgr5551,
            Bgr565,
            Rgb24,
            Bgr24,
            Bgra32,
        };

        public bool IsMatch(ushort bits, BITMASKS masks)
        {
            if (bits != BitsPerPixel)
                return false;

            if (IsIndexed)
            {
                return true;
            }
            else
            {
                return masks.Equals(Masks);
            }
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

            return other.Masks.Equals(Masks);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 7) + BitsPerPixel.GetHashCode();
                hash = (hash * 7) + Masks.GetHashCode();
                return hash;
            }
        }
    }

    internal class BitFields
    {
        public static readonly BITMASKS BITFIELDS_RGB_24 = new BITMASKS(0xff, 0xff00, 0xff0000);
        public static readonly BITMASKS BITFIELDS_BGR_24 = new BITMASKS(0xff0000, 0xff00, 0xff);
        public static readonly BITMASKS BITFIELDS_BGRA_32 = new BITMASKS(0xff0000, 0xff00, 0xff, 0xff000000);
        public static readonly BITMASKS BITFIELDS_BGR_565 = new BITMASKS(0xf800, 0x7e0, 0x1f);
        public static readonly BITMASKS BITFIELDS_BGRA_5551 = new BITMASKS(0x7c00, 0x03e0, 0x001f, 0x8000);
        public static readonly BITMASKS BITFIELDS_BGRA_555X = new BITMASKS(0x7c00, 0x03e0, 0x001f);
    }

    //internal class BitFields
    //{
    //public static readonly uint[] BITFIELDS_RGB_24 = new uint[] { 0xff, 0xff00, 0xff0000 };
    //public static readonly uint[] BITFIELDS_BGR_24 = new uint[] { 0xff0000, 0xff00, 0xff };
    //public static readonly uint[] BITFIELDS_BGRA_32 = new uint[] { 0xff0000, 0xff00, 0xff, 0xff000000 };
    ////private static readonly uint[] BITFIELDS_BGR_32 = new uint[] { 0xff0000, 0xff00, 0xff };
    //public static readonly uint[] BITFIELDS_BGR_565 = new uint[] { 0xf800, 0x7e0, 0x1f };
    //public static readonly uint[] BITFIELDS_BGRA_5551 = new uint[] { 0x7c00, 0x03e0, 0x001f, 0x8000 };
    //public static readonly uint[] BITFIELDS_BGRA_555X = new uint[] { 0x7c00, 0x03e0, 0x001f };
    //}

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

    internal struct BITMASKS
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
        //public uint[] GetRGBA() => new uint[] { maskRed, maskGreen, maskBlue, maskAlpha };

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 7) + (int)maskRed;
                hash = (hash * 7) + (int)maskGreen;
                hash = (hash * 7) + (int)maskBlue;
                hash = (hash * 7) + (int)maskAlpha;
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

    internal struct BITMAP_WRITE_REQUEST
    {
        public bool headerIncludeFile;
        public BitmapCoreHeaderType headerType;
        public byte[] iccProfileData;
        public bool iccEmbed;

        public double dpiX;
        public double dpiY;

        public RGBQUAD[] imgColorTable;
        public bool imgTopDown;
        public int imgWidth;
        public int imgHeight;
        public uint imgStride;
    }

    internal struct BITMAP_READ_DETAILS
    {
        public BITMAPV5HEADER dibHeader;
        public ushort bbp;
        public double dpiX;
        public double dpiY;
        public BitmapCompressionMode compression;

        public BITMASKS cMasks;
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct MASKTRIPLE
    {
        public uint rgbRed;
        public uint rgbGreen;
        public uint rgbBlue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MASKQUAD
    {
        public uint rgbRed;
        public uint rgbGreen;
        public uint rgbBlue;
        public uint rgbAlpha;
    }

    internal unsafe class PointerStream : Stream
    {
        private readonly byte* _bufferStart;
        private readonly long _bufferLen;
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _bufferLen;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public PointerStream(byte* buffer, long bufferLen)
        {
            _bufferStart = buffer;
            _bufferLen = bufferLen;
        }

        public override void Flush()
        {
            // nop
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, (int)(_bufferLen - _position));
            Marshal.Copy((IntPtr)(_bufferStart + _position), buffer, offset, count);
            _position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    return _position;
                case SeekOrigin.Current:
                    _position += offset;
                    return _position;
                case SeekOrigin.End:
                    _position = _bufferLen + offset;
                    return _position;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }

    internal struct G31DFaxCode
    {
        public byte bitLength;
        public ushort code;
        public int runLength;

        public G31DFaxCode(byte bl, ushort c, int run)
        {
            bitLength = bl;
            code = c;
            runLength = run;
        }
    }

    internal class CCITTHuffmanG31D
    {
        // 1 = black and 0 = white
        // Run lengths are represented by two types of code words: makeup and terminating
        // The run-length code words are taken from a predefined table of values representing runs of black or white pixels. 
        // This table is part of the T.4 specification and is used to encode and decode all Group 3 data.


        // An encoded pixel run is made up of zero or more makeup code words and a terminating code word
        // Terminating code words represent shorter runs, and makeup codes represent longer runs. There are separate terminating and makeup code words for both black and white runs.


        // Pixel runs with a length of 0 to 63 are encoded using a single terminating code. 
        // Runs of 64 to 2623 pixels are encoded by a single makeup code and a terminating code. 
        // Run lengths greater than 2623 pixels are encoded using one or more makeup codes and a terminating code. 
        // The run length is the sum of the length values represented by each code word.

        // To ensure that the receiver (decompressor) maintains color synchronization, all
        // data lines begin with a white run-length code word set.If the actual scan line
        // begins with a black run, a white run-length of zero is sent (written).


        // m_faxBlackCodes
        // https://github.com/BitMiracle/libtiff.net/blob/4eb982e17da30be0deb5fae6ce794a7a65524c71/LibTiff/Internal/CCITTCodec.cs#L1336 Fax3Decode1D
        // https://github.com/BitMiracle/libtiff.net/blob/4eb982e17da30be0deb5fae6ce794a7a65524c71/LibTiff/Internal/CCITTCodec.cs#L1834 EXPAND1D
        // https://github.com/BitMiracle/libtiff.net/blob/4eb982e17da30be0deb5fae6ce794a7a65524c71/LibTiff/Internal/CCITTCodec_Data.cs tables...
        // https://www.itu.int/itudoc/itu-t/com16/tiff-fx/docs/tiff6.pdf Section10: Modified Huffman Compression
        // http://zig.tgschultz.com/bmp_file_format.txt really good description of Huffman1D in the context of bitmaps

        //internal const short G3CODE_EOL = -1;  /* NB: ACT_EOL - ACT_WRUNT */
        //internal const short G3CODE_INVALID = -2;  /* NB: ACT_INVALID - ACT_WRUNT */
        //internal const short G3CODE_EOF = -3;  /* end of input data */
        //internal const short G3CODE_INCOMP = -4;  /* incomplete run code */

        private static readonly short[] faxWhiteCodes =
        {
            8, 0x35, 0,  /* 0011 0101 */
            6, 0x7, 1,  /* 0001 11 */
            4, 0x7, 2,  /* 0111 */
            4, 0x8, 3,  /* 1000 */
            4, 0xB, 4,  /* 1011 */
            4, 0xC, 5,  /* 1100 */
            4, 0xE, 6,  /* 1110 */
            4, 0xF, 7,  /* 1111 */
            5, 0x13, 8,  /* 1001 1 */
            5, 0x14, 9,  /* 1010 0 */
            5, 0x7, 10,  /* 0011 1 */
            5, 0x8, 11,  /* 0100 0 */
            6, 0x8, 12,  /* 0010 00 */
            6, 0x3, 13,  /* 0000 11 */
            6, 0x34, 14,  /* 1101 00 */
            6, 0x35, 15,  /* 1101 01 */
            6, 0x2A, 16,  /* 1010 10 */
            6, 0x2B, 17,  /* 1010 11 */
            7, 0x27, 18,  /* 0100 111 */
            7, 0xC, 19,  /* 0001 100 */
            7, 0x8, 20,  /* 0001 000 */
            7, 0x17, 21,  /* 0010 111 */
            7, 0x3, 22,  /* 0000 011 */
            7, 0x4, 23,  /* 0000 100 */
            7, 0x28, 24,  /* 0101 000 */
            7, 0x2B, 25,  /* 0101 011 */
            7, 0x13, 26,  /* 0010 011 */
            7, 0x24, 27,  /* 0100 100 */
            7, 0x18, 28,  /* 0011 000 */
            8, 0x2, 29,  /* 0000 0010 */
            8, 0x3, 30,  /* 0000 0011 */
            8, 0x1A, 31,  /* 0001 1010 */
            8, 0x1B, 32,  /* 0001 1011 */
            8, 0x12, 33,  /* 0001 0010 */
            8, 0x13, 34,  /* 0001 0011 */
            8, 0x14, 35,  /* 0001 0100 */
            8, 0x15, 36,  /* 0001 0101 */
            8, 0x16, 37,  /* 0001 0110 */
            8, 0x17, 38,  /* 0001 0111 */
            8, 0x28, 39,  /* 0010 1000 */
            8, 0x29, 40,  /* 0010 1001 */
            8, 0x2A, 41,  /* 0010 1010 */
            8, 0x2B, 42,  /* 0010 1011 */
            8, 0x2C, 43,  /* 0010 1100 */
            8, 0x2D, 44,  /* 0010 1101 */
            8, 0x4, 45,  /* 0000 0100 */
            8, 0x5, 46,  /* 0000 0101 */
            8, 0xA, 47,  /* 0000 1010 */
            8, 0xB, 48,  /* 0000 1011 */
            8, 0x52, 49,  /* 0101 0010 */
            8, 0x53, 50,  /* 0101 0011 */
            8, 0x54, 51,  /* 0101 0100 */
            8, 0x55, 52,  /* 0101 0101 */
            8, 0x24, 53,  /* 0010 0100 */
            8, 0x25, 54,  /* 0010 0101 */
            8, 0x58, 55,  /* 0101 1000 */
            8, 0x59, 56,  /* 0101 1001 */
            8, 0x5A, 57,  /* 0101 1010 */
            8, 0x5B, 58,  /* 0101 1011 */
            8, 0x4A, 59,  /* 0100 1010 */
            8, 0x4B, 60,  /* 0100 1011 */
            8, 0x32, 61,  /* 0011 0010 */
            8, 0x33, 62,  /* 0011 0011 */
            8, 0x34, 63,  /* 0011 0100 */

            // end of terminating codes
            // begin makeup codes

            5, 0x1B, 64,  /* 1101 1 */
            5, 0x12, 128,  /* 1001 0 */
            6, 0x17, 192,  /* 0101 11 */
            7, 0x37, 256,  /* 0110 111 */
            8, 0x36, 320,  /* 0011 0110 */
            8, 0x37, 384,  /* 0011 0111 */
            8, 0x64, 448,  /* 0110 0100 */
            8, 0x65, 512,  /* 0110 0101 */
            8, 0x68, 576,  /* 0110 1000 */
            8, 0x67, 640,  /* 0110 0111 */
            9, 0xCC, 704,  /* 0110 0110 0 */
            9, 0xCD, 768,  /* 0110 0110 1 */
            9, 0xD2, 832,  /* 0110 1001 0 */
            9, 0xD3, 896,  /* 0110 1001 1 */
            9, 0xD4, 960,  /* 0110 1010 0 */
            9, 0xD5, 1024,  /* 0110 1010 1 */
            9, 0xD6, 1088,  /* 0110 1011 0 */
            9, 0xD7, 1152,  /* 0110 1011 1 */
            9, 0xD8, 1216,  /* 0110 1100 0 */
            9, 0xD9, 1280,  /* 0110 1100 1 */
            9, 0xDA, 1344,  /* 0110 1101 0 */
            9, 0xDB, 1408,  /* 0110 1101 1 */
            9, 0x98, 1472,  /* 0100 1100 0 */
            9, 0x99, 1536,  /* 0100 1100 1 */
            9, 0x9A, 1600,  /* 0100 1101 0 */
            6, 0x18, 1664,  /* 0110 00 */
            9, 0x9B, 1728,  /* 0100 1101 1 */

            // begin SHARED makeup codes

            11, 0x8, 1792,  /* 0000 0001 000 */
            11, 0xC, 1856,  /* 0000 0001 100 */
            11, 0xD, 1920,  /* 0000 0001 101 */
            12, 0x12, 1984,  /* 0000 0001 0010 */
            12, 0x13, 2048,  /* 0000 0001 0011 */
            12, 0x14, 2112,  /* 0000 0001 0100 */
            12, 0x15, 2176,  /* 0000 0001 0101 */
            12, 0x16, 2240,  /* 0000 0001 0110 */
            12, 0x17, 2304,  /* 0000 0001 0111 */
            12, 0x1C, 2368,  /* 0000 0001 1100 */
            12, 0x1D, 2432,  /* 0000 0001 1101 */
            12, 0x1E, 2496,  /* 0000 0001 1110 */
            12, 0x1F, 2560,  /* 0000 0001 1111 */

            12, 0b_0000_0000_0001, -1, // EOL

            //12, 0x1, G3CODE_EOL,  /* 0000 0000 0001 */
            //9, 0x1, G3CODE_INVALID,  /* 0000 0000 1 */
            //10, 0x1, G3CODE_INVALID,  /* 0000 0000 01 */
            //11, 0x1, G3CODE_INVALID,  /* 0000 0000 001 */
            //12, 0x0, G3CODE_INVALID,  /* 0000 0000 0000 */
        };

        private static readonly short[] faxBlackCodes =
        {
            10, 0x37, 0,  /* 0000 1101 11 */
            3, 0x2, 1,  /* 010 */
            2, 0x3, 2,  /* 11 */
            2, 0x2, 3,  /* 10 */
            3, 0x3, 4,  /* 011 */
            4, 0x3, 5,  /* 0011 */
            4, 0x2, 6,  /* 0010 */
            5, 0x3, 7,  /* 0001 1 */
            6, 0x5, 8,  /* 0001 01 */
            6, 0x4, 9,  /* 0001 00 */
            7, 0x4, 10,  /* 0000 100 */
            7, 0x5, 11,  /* 0000 101 */
            7, 0x7, 12,  /* 0000 111 */
            8, 0x4, 13,  /* 0000 0100 */
            8, 0x7, 14,  /* 0000 0111 */
            9, 0x18, 15,  /* 0000 1100 0 */
            10, 0x17, 16,  /* 0000 0101 11 */
            10, 0x18, 17,  /* 0000 0110 00 */
            10, 0x8, 18,  /* 0000 0010 00 */
            11, 0x67, 19,  /* 0000 1100 111 */
            11, 0x68, 20,  /* 0000 1101 000 */
            11, 0x6C, 21,  /* 0000 1101 100 */
            11, 0x37, 22,  /* 0000 0110 111 */
            11, 0x28, 23,  /* 0000 0101 000 */
            11, 0x17, 24,  /* 0000 0010 111 */
            11, 0x18, 25,  /* 0000 0011 000 */
            12, 0xCA, 26,  /* 0000 1100 1010 */
            12, 0xCB, 27,  /* 0000 1100 1011 */
            12, 0xCC, 28,  /* 0000 1100 1100 */
            12, 0xCD, 29,  /* 0000 1100 1101 */
            12, 0x68, 30,  /* 0000 0110 1000 */
            12, 0x69, 31,  /* 0000 0110 1001 */
            12, 0x6A, 32,  /* 0000 0110 1010 */
            12, 0x6B, 33,  /* 0000 0110 1011 */
            12, 0xD2, 34,  /* 0000 1101 0010 */
            12, 0xD3, 35,  /* 0000 1101 0011 */
            12, 0xD4, 36,  /* 0000 1101 0100 */
            12, 0xD5, 37,  /* 0000 1101 0101 */
            12, 0xD6, 38,  /* 0000 1101 0110 */
            12, 0xD7, 39,  /* 0000 1101 0111 */
            12, 0x6C, 40,  /* 0000 0110 1100 */
            12, 0x6D, 41,  /* 0000 0110 1101 */
            12, 0xDA, 42,  /* 0000 1101 1010 */
            12, 0xDB, 43,  /* 0000 1101 1011 */
            12, 0x54, 44,  /* 0000 0101 0100 */
            12, 0x55, 45,  /* 0000 0101 0101 */
            12, 0x56, 46,  /* 0000 0101 0110 */
            12, 0x57, 47,  /* 0000 0101 0111 */
            12, 0x64, 48,  /* 0000 0110 0100 */
            12, 0x65, 49,  /* 0000 0110 0101 */
            12, 0x52, 50,  /* 0000 0101 0010 */
            12, 0x53, 51,  /* 0000 0101 0011 */
            12, 0x24, 52,  /* 0000 0010 0100 */
            12, 0x37, 53,  /* 0000 0011 0111 */
            12, 0x38, 54,  /* 0000 0011 1000 */
            12, 0x27, 55,  /* 0000 0010 0111 */
            12, 0x28, 56,  /* 0000 0010 1000 */
            12, 0x58, 57,  /* 0000 0101 1000 */
            12, 0x59, 58,  /* 0000 0101 1001 */
            12, 0x2B, 59,  /* 0000 0010 1011 */
            12, 0x2C, 60,  /* 0000 0010 1100 */
            12, 0x5A, 61,  /* 0000 0101 1010 */
            12, 0x66, 62,  /* 0000 0110 0110 */
            12, 0x67, 63,  /* 0000 0110 0111 */

            // end of terminating codes
            // begin makeup codes

            10, 0xF, 64,     /* 0000 0011 11 */
            12, 0xC8, 128,   /* 0000 1100 1000 */
            12, 0xC9, 192,   /* 0000 1100 1001 */
            12, 0x5B, 256,   /* 0000 0101 1011 */
            12, 0x33, 320,   /* 0000 0011 0011 */
            12, 0x34, 384,   /* 0000 0011 0100 */
            12, 0x35, 448,   /* 0000 0011 0101 */
            13, 0x6C, 512,   /* 0000 0011 0110 0 */
            13, 0x6D, 576,   /* 0000 0011 0110 1 */
            13, 0x4A, 640,   /* 0000 0010 0101 0 */
            13, 0x4B, 704,   /* 0000 0010 0101 1 */
            13, 0x4C, 768,   /* 0000 0010 0110 0 */
            13, 0x4D, 832,   /* 0000 0010 0110 1 */
            13, 0x72, 896,   /* 0000 0011 1001 0 */
            13, 0x73, 960,   /* 0000 0011 1001 1 */
            13, 0x74, 1024,  /* 0000 0011 1010 0 */
            13, 0x75, 1088,  /* 0000 0011 1010 1 */
            13, 0x76, 1152,  /* 0000 0011 1011 0 */
            13, 0x77, 1216,  /* 0000 0011 1011 1 */
            13, 0x52, 1280,  /* 0000 0010 1001 0 */
            13, 0x53, 1344,  /* 0000 0010 1001 1 */
            13, 0x54, 1408,  /* 0000 0010 1010 0 */
            13, 0x55, 1472,  /* 0000 0010 1010 1 */
            13, 0x5A, 1536,  /* 0000 0010 1101 0 */
            13, 0x5B, 1600,  /* 0000 0010 1101 1 */
            13, 0x64, 1664,  /* 0000 0011 0010 0 */
            13, 0x65, 1728,  /* 0000 0011 0010 1 */

            // begin SHARED makeup codes

            11, 0x8, 1792,   /* 0000 0001 000 */
            11, 0xC, 1856,   /* 0000 0001 100 */
            11, 0xD, 1920,   /* 0000 0001 101 */
            12, 0x12, 1984,  /* 0000 0001 0010 */
            12, 0x13, 2048,  /* 0000 0001 0011 */
            12, 0x14, 2112,  /* 0000 0001 0100 */
            12, 0x15, 2176,  /* 0000 0001 0101 */
            12, 0x16, 2240,  /* 0000 0001 0110 */
            12, 0x17, 2304,  /* 0000 0001 0111 */
            12, 0x1C, 2368,  /* 0000 0001 1100 */
            12, 0x1D, 2432,  /* 0000 0001 1101 */
            12, 0x1E, 2496,  /* 0000 0001 1110 */
            12, 0x1F, 2560,  /* 0000 0001 1111 */

            12, 0b_000000000001, -1, // EOL

            //12, 0x1, G3CODE_EOL,  /* 0000 0000 0001 */
            //9, 0x1, G3CODE_INVALID,  /* 0000 0000 1 */
            //10, 0x1, G3CODE_INVALID,  /* 0000 0000 01 */
            //11, 0x1, G3CODE_INVALID,  /* 0000 0000 001 */
            //12, 0x0, G3CODE_INVALID,  /* 0000 0000 0000 */
        };

        private static IEnumerable<G31DFaxCode> FromArray(short[] arr)
        {
            for (int i = 0; i < arr.Length; i += 3)
            {
                yield return new G31DFaxCode((byte)arr[i], (ushort)arr[i + 1], arr[i + 2]);
            }
        }

        public static readonly G31DFaxCode[] WhiteCodes = FromArray(faxWhiteCodes).OrderBy(c => c.bitLength).ToArray();
        public static readonly G31DFaxCode[] BlackCodes = FromArray(faxBlackCodes).OrderBy(c => c.bitLength).ToArray();
    }
}