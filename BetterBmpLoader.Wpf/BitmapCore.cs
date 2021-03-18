using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BetterBmpLoader
{
    internal unsafe class BitmapCore
    {
        private const ushort BFH_BM = 0x4D42;

        private const string
            ERR_HEOF = "Bitmap stream ended while parsing header, but more data was expected. This usually indicates an malformed file or empty data stream.";

        // http://paulbourke.net/dataformats/bitmaps/
        // http://www.libertybasicuniversity.com/lbnews/nl100/format.htm
        // https://www.displayfusion.com/Discussions/View/converting-c-data-types-to-c/?ID=38db6001-45e5-41a3-ab39-8004450204b3
        // https://github.com/FlyingPumba/tp2-orga2/blob/master/entregable/src/bmp/bmp.c

        private static int calc_shift(uint mask)
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
                throw new NotSupportedException($"Bitmap header size '{header_size}' not supported.");
            }

            ptr += header_size;
            offset += (int)header_size;

            ushort nbits = bi.bV5BitCount;

            //if (nbits != 32 && nbits != 24 &&  && nbits != 16)
            //    throw new NotSupportedException($"Bitmaps with bpp of '{nbits}' are not supported. Expected 16, 24, or 32.");

            if (bi.bV5Planes != 1)
                throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

            if (bi.bV5CSType != ColorSpaceType.LCS_sRGB && bi.bV5CSType != ColorSpaceType.LCS_WINDOWS_COLOR_SPACE && bi.bV5CSType != ColorSpaceType.PROFILE_EMBEDDED && bi.bV5CSType != ColorSpaceType.LCS_CALIBRATED_RGB)
                throw new NotSupportedException($"Bitmap with header size '{header_size}' and color space of '{bi.bV5CSType.ToString()}' is not supported.");

            uint maskR = 0;
            uint maskG = 0;
            uint maskB = 0;
            uint maskA = 0;

            bool hasAlphaChannel = false;
            bool skipVerifyBppAndMasks = false;

            switch (bi.bV5Compression)
            {
                case BitmapCompressionMode.BI_BITFIELDS:
                    // seems that v5 bitmaps sometimes do not have a color table, even if BI_BITFIELDS is set
                    // we read/skip them here anyways, if we have a file header we can correct the offset later
                    // whether or not these follow the header depends entirely on the application that created the bitmap..
                    // if (header_size <= 40)
                    // {

                    // OS/2 bitmaps are only 9 bytes here instead of 12 as they are not aligned, 
                    // is that true? it is for color table.. not sure about bitfields

                    //if (is_os21x_)
                    //{
                    //    maskR = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                    //    offset += 3;
                    //    maskG = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                    //    offset += 3;
                    //    maskB = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                    //    offset += 3;
                    //}
                    //else

                    var btfiSize = (sizeof(uint) * 3);
                    if (offset + btfiSize > sourceLength)
                        throw new InvalidOperationException(ERR_HEOF);

                    maskR = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskG = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskB = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    offset += btfiSize;

                    // maskR | maskG | maskB == 1 << nbits - 1
                    // do these overlap, and do they add up to 0xFFFFFF
                    // }
                    break;
                case BitmapCompressionMode.BI_ALPHABITFIELDS:
                    var btfiaSize = (sizeof(uint) * 4);
                    if (offset + btfiaSize > sourceLength)
                        throw new InvalidOperationException(ERR_HEOF);

                    maskR = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskG = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskB = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskA = StructUtil.ReadU32(ptr);
                    ptr += sizeof(uint);
                    offset += btfiaSize;
                    hasAlphaChannel = true;
                    break;
                case BitmapCompressionMode.BI_RGB:
                    switch (nbits)
                    {
                        case 32:
                            // windows wrongly uses the 4th byte of BI_RGB 32bit dibs as alpha
                            // but we need to do it too if we have any hope of reading alpha data
                            maskR = 0xff0000;
                            maskG = 0xff00;
                            maskB = 0xff;
                            maskA = 0xff000000; // fake transparency?
                            break;
                        case 24:
                            maskR = 0xff0000;
                            maskG = 0xff00;
                            maskB = 0xff;
                            break;
                        case 16:
                            maskR = 0x7c00;
                            maskG = 0x03e0;
                            maskB = 0x001f;
                            // we could check for transparency in 16b RGB but it is slower and is very uncommon
                            // maskA = 0x8000; // fake transparency?
                            break;
                    }
                    break;
                case BitmapCompressionMode.BI_RLE4:
                case BitmapCompressionMode.BI_RLE8:
                case BitmapCompressionMode.OS2_RLE24:
                case BitmapCompressionMode.BI_JPEG:
                case BitmapCompressionMode.BI_PNG:
                    if (bi.bV5Height < 0) throw new NotSupportedException("Top-down bitmaps are not supported with RLE/JPEG/PNG compression.");
                    skipVerifyBppAndMasks = true;
                    break;
                default:
                    throw new NotSupportedException($"Bitmap with bV5Compression of '{bi.bV5Compression.ToString()}' is not supported.");
            }

            // lets use the v3/v4/v5 masks if present instead of BITFIELDS or RGB
            if (bi.bV5RedMask != 0) maskR = bi.bV5RedMask;
            if (bi.bV5BlueMask != 0) maskB = bi.bV5BlueMask;
            if (bi.bV5GreenMask != 0) maskG = bi.bV5GreenMask;
            if (bi.bV5AlphaMask != 0)
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

            if (pallength > 65536)
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

            var fmt = BitmapCorePixelFormat2.Formats.SingleOrDefault(f => f.IsMatch(nbits, maskB, maskG, maskR, maskA));

            // currently we only support RLE -> Bgra32
            if (bi.bV5Compression == BitmapCompressionMode.BI_RLE4 || bi.bV5Compression == BitmapCompressionMode.BI_RLE8 || bi.bV5Compression == BitmapCompressionMode.OS2_RLE24)
                fmt = BitmapCorePixelFormat2.Bgra32;

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

                cMaskAlpha = maskA,
                cMaskBlue = maskB,
                cMaskGreen = maskG,
                cMaskRed = maskR,
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

            var maskR = info.cMaskRed;
            var maskG = info.cMaskGreen;
            var maskB = info.cMaskBlue;
            var maskA = info.cMaskAlpha;

            int shiftR = 0, shiftG = 0, shiftB = 0, shiftA = 0;
            uint maxR = 0, maxG = 0, maxB = 0, maxA = 0;
            uint multR = 0, multG = 0, multB = 0, multA = 0;

            if (maskR != 0)
            {
                shiftR = calc_shift(maskR);
                maxR = maskR >> shiftR;
                multR = (uint)(Math.Ceiling(255d / maxR * 65536 * 256)); // bitshift << 24
            }

            if (maskG != 0)
            {
                shiftG = calc_shift(maskG);
                maxG = maskG >> shiftG;
                multG = (uint)(Math.Ceiling(255d / maxG * 65536 * 256));
            }

            if (maskB != 0)
            {
                shiftB = calc_shift(maskB);
                maxB = maskB >> shiftB;
                multB = (uint)(Math.Ceiling(255d / maxB * 65536 * 256));
            }

            if (maskA != 0)
            {
                shiftA = calc_shift(maskA);
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

        public static byte[] WriteToBMP(ref BITMAP_WRITE_REQUEST info, bool renderFileHeader, byte* sourceBufferStart, byte[] iccProfileData, bool iccEmbed)
        {
            bool hasIcc = iccProfileData != null && iccProfileData.Length > 0;

            var toFmt = info.fmt;
            var nbits = toFmt.BitsPerPixel;
            var hasAlpha = toFmt.HasAlpha;
            var lcmspxFmt = toFmt.LcmsFormat;

            if (nbits < 16 && (info.imgColorTable == null || info.imgColorTable.Length == 0))
                throw new InvalidOperationException("A 1bbp indexed bitmap must have a color table / palette.");

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

            if (hasAlpha || (iccEmbed && hasIcc)) // write V5 header if embedded color profile or has alpha data
            {
                var v5Size = (uint)Marshal.SizeOf<BITMAPV5HEADER>();
                // Typical structure:
                // - BITMAPFILEHEADER (Optional)
                // - BITMAPV5HEADER
                // - BI_BITFIELDS (Optional)
                // - Color Table (Optional)
                // - Pixel Data
                // - Embedded Color Profile (Optional)

                var bitmasks = toFmt.IsIndexed ? new uint[] { 0, 0, 0 } : toFmt.Masks;

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
                    bV5Compression = BitmapCompressionMode.BI_RGB, // always RGB for v5, we can specify masks in header instead of BITFIELDS
                    bV5XPelsPerMeter = 0,
                    bV5YPelsPerMeter = 0,

                    bV5RedMask = bitmasks[0],
                    bV5GreenMask = bitmasks[1],
                    bV5BlueMask = bitmasks[2],
                    bV5AlphaMask = bitmasks.Length > 3 ? bitmasks[3] : 0,

                    bV5ClrImportant = paletteSize,
                    bV5ClrUsed = paletteSize,
                    bV5SizeImage = (uint)(info.imgStride * info.imgHeight),

                    bV5CSType = ColorSpaceType.LCS_sRGB,
                    bV5Intent = Bv5Intent.LCS_GM_IMAGES,
                };

                uint offset = renderFileHeader ? fhSize : 0;
                offset += v5Size;

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
                var infoSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
                // Typical structure:
                // - BITMAPFILEHEADER (Optional)
                // - BITMAPINFOHEADER
                // - BI_BITFIELDS (Optional)
                // - Color Table (Optional)
                // - Pixel Data

                BitmapCompressionMode compr = (toFmt == BitmapCorePixelFormat2.Bgr24 || toFmt.IsIndexed) ? BitmapCompressionMode.BI_RGB : BitmapCompressionMode.BI_BITFIELDS;

                var bitmasks = toFmt.IsIndexed ? new uint[] { 0, 0, 0 } : toFmt.Masks;

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
                    var fields = BitConverter.GetBytes(bitmasks[0]).Concat(BitConverter.GetBytes(bitmasks[1])).Concat(BitConverter.GetBytes(bitmasks[2])).ToArray();
                    Buffer.BlockCopy(fields, 0, buffer, (int)offset, fields.Length);
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
}
