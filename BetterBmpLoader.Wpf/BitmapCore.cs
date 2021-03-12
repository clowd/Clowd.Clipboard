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

        private static double pixelPerMeterToDpi(int pels)
        {
            if (pels == 0) return 96;
            return pels * 0.0254d;
        }

        private static ushort ReadU16(byte* ptr)
        {
            var arr = new byte[] { *ptr, *(ptr + 1) };
            return BitConverter.ToUInt16(arr, 0);
        }

        private static uint ReadU32(byte* ptr)
        {
            var arr = new byte[] { *ptr, *(ptr + 1), *(ptr + 2), *(ptr + 3) };
            return BitConverter.ToUInt32(arr, 0);
        }

        public static void GetInfo(byte* source, int sourceLength, out BITMAP_DETAILS info)
        {
            var ptr = source;

            if ((sourceLength) < 12) // min header size
                throw new InvalidOperationException(ERR_HEOF);

            bool hasFileHeader = ReadU16(ptr) == BFH_BM;
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

            var header_size = ReadU32(ptr);
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

                    maskR = ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskG = ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskB = ReadU32(ptr);
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

                    maskR = ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskG = ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskB = ReadU32(ptr);
                    ptr += sizeof(uint);
                    maskA = ReadU32(ptr);
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
                            maskA = 0x8000; // fake transparency?
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

            info = new BITMAP_DETAILS
            {
                dibHeader = bi,
                bbp = nbits,
                cMaskAlpha = maskA,
                cMaskBlue = maskB,
                cMaskGreen = maskG,
                cMaskRed = maskR,
                cIndexed = smBit,
                compression = bi.bV5Compression,
                dpiX = pixelPerMeterToDpi(bi.bV5XPelsPerMeter),
                dpiY = pixelPerMeterToDpi(bi.bV5YPelsPerMeter),
                imgColorTable = palette,
                imgHeight = height,
                imgTopDown = topDown,
                imgWidth = width,
                cTrueAlpha = hasAlphaChannel,
                iccProfileSize = profileSize,
                iccProfileOffset = profileOffset,
                iccProfileType = bi.bV5CSType,
                iccProfileIntent = bi.bV5Intent,
                imgDataOffset = dataOffset,
                imgDataSize = dataSize,
                imgStride = source_stride,
            };
        }

        public static void RunRLEtoBGRA8(ref BITMAP_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
        {
            var is4 = info.compression == BitmapCompressionMode.BI_RLE4 && info.bbp == 4;
            var is8 = info.compression == BitmapCompressionMode.BI_RLE8 && info.bbp == 8;
            var is24 = info.compression == BitmapCompressionMode.OS2_RLE24 && info.bbp == 24;

            if (!is4 && !is8 && !is24)
                throw new NotSupportedException("RLE invalid bpp");

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

        public static void ReadPixelsToBGRA8(ref BITMAP_DETAILS info, byte* sourceBufferStart, byte* destBufferStart, bool preserveFakeAlpha)
        {
            var palette = info.imgColorTable;
            var nbits = info.bbp;
            var width = info.imgWidth;
            var height = info.imgHeight;
            var compr = info.compression;
            var upside_down = info.imgTopDown;
            var hasAlphaChannel = info.cTrueAlpha;

            if (compr == BitmapCompressionMode.BI_JPEG || compr == BitmapCompressionMode.BI_PNG)
            {
                throw new NotSupportedException("BI_JPEG and BI_PNG passthrough compression is not supported.");
            }

            if (compr == BitmapCompressionMode.BI_RLE4 || compr == BitmapCompressionMode.BI_RLE8 || compr == BitmapCompressionMode.OS2_RLE24)
            {
                RunRLEtoBGRA8(ref info, sourceBufferStart, destBufferStart);
                return;
            }

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

            RGBQUAD color;
            int pal = palette.Length;
            byte b, r, g, a;
            uint i32;
            byte i4;
            byte* source;
            uint* dest;
            int y, x, w, h = height, nbytes = (nbits / 8);

            if (nbits == 1)
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    for (x = 0; x < source_stride - 1; x++)
                    {
                        i4 = *source++;
                        for (int bit = 7; bit >= 0; bit--)
                        {
                            color = palette[(i4 & (1 << bit)) >> bit];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                    }

                    // last bits in a row might not make up a whole byte
                    i4 = *source++;
                    for (int bit = 7; bit >= 8 - (width - ((source_stride - 1) * 8)); bit--)
                    {
                        color = palette[(i4 & (1 << bit)) >> bit];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                }
            }
            else if (nbits == 2)
            {
                var px_remain = width % 4;
                if (px_remain == 0) px_remain = 4;

                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    for (x = 0; x < source_stride - 1; x++)
                    {
                        i4 = *source++;

                        color = palette[((i4 & 0b_1100_0000) >> 6) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                        color = palette[((i4 & 0b_0011_0000) >> 4) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                        color = palette[((i4 & 0b_0000_1100) >> 2) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                        color = palette[((i4 & 0b_0000_0011) >> 0) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }

                    i4 = *source++;

                    if (px_remain > 0)
                    {
                        color = palette[((i4 & 0b_1100_0000) >> 6) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                    if (px_remain > 1)
                    {
                        color = palette[((i4 & 0b_0011_0000) >> 4) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                    if (px_remain > 2)
                    {
                        color = palette[((i4 & 0b_0000_1100) >> 2) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                    if (px_remain > 3)
                    {
                        color = palette[((i4 & 0b_0000_0011) >> 0) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                }
            }
            else if (nbits == 4)
            {
                var px_remain = width % 2;
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    for (x = 0; x < source_stride - px_remain; x++)
                    {
                        i4 = *source++;
                        color = palette[((i4 & 0b_1111_0000) >> 4) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        color = palette[((i4 & 0b_0000_1111) >> 0) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }

                    if (px_remain > 0)
                    {
                        i4 = *source++;
                        color = palette[((i4 & 0b_1111_0000) >> 4) % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                }
            }
            else if (nbits == 8)
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i4 = *source++;
                        color = palette[i4 % pal];
                        *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                    }
                }
            }
            else if (nbits >= 16 && hasAlphaChannel)
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i32 = *(uint*)source;

                        b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                        g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                        r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                        a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);

                        *dest++ = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
                        source += nbytes;
                    }
                }
            }
            else if (nbits >= 16 && maskA != 0 && preserveFakeAlpha) // hasAlpha = false, and maskA != 0 - we might have _fake_ alpha.. need to check for it
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
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

                        *dest++ = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                        source += nbytes;
                    }
                }
            }
            else if (nbits >= 16) // simple bmp, no transparency
            {
                while (--h >= 0)
                {
                    y = height - h - 1;
                    dest = (uint*)(destBufferStart + ((upside_down ? y : h) * dest_stride));
                    source = sourceBufferStart + (y * source_stride);
                    w = width;
                    while (--w >= 0)
                    {
                        i32 = *(uint*)source;
                        b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                        g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                        r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                        a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);
                        *dest++ = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                        source += nbytes;
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"Bitmap combination of header length ({info.dibHeader.bV5Size}) compression ({compr.ToString()}), palette length ({pal}), bits-per-pixel ({nbits}) is not supported.");
            }
        }

        public static byte[] WriteBGRA8ToBmp(byte* sourceBufferStart, int width, int height, int stride, ushort nbits, byte[] iccProfileData)
        {

        }
    }

    internal class StructUtil
    {
        public unsafe static T Deserialize<T>(byte* ptr)
        {
            return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
        }
    }

    internal static class lcms
    {
        public enum Intent : uint
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

        // todo: support more of these?
        public static readonly uint TYPE_BGRA_8 = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1);

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

        public static CIExyYTRIPLE GetPrimariesFromBMPEndpoints(uint red_x, uint red_y, uint green_x, uint green_y, uint blue_x, uint blue_y)
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

        public static CIExyY GetWhitePoint_sRGB()
        {
            double kD65x = 0.31271;
            double kD65y = 0.32902;
            return new CIExyY { x = kD65x, y = kD65y, Y = 1, };
        }

        public unsafe static void TransformBGRA8(ref BITMAP_DETAILS info, byte* source, byte* dataBuffer, int dataStride)
        {
            if (info.iccProfileType == ColorSpaceType.LCS_sRGB || info.iccProfileType == ColorSpaceType.LCS_WINDOWS_COLOR_SPACE)
                return; // do nothing

            Intent lcmsIntent;
            switch (info.iccProfileIntent)
            {
                case Bv5Intent.LCS_GM_BUSINESS: lcmsIntent = Intent.Saturation; break;
                case Bv5Intent.LCS_GM_GRAPHICS: lcmsIntent = Intent.RelativeColorimetric; break;
                case Bv5Intent.LCS_GM_IMAGES: lcmsIntent = Intent.Perceptual; break;
                case Bv5Intent.LCS_GM_ABS_COLORIMETRIC: lcmsIntent = Intent.AbsoluteColorimetric; break;
                default: throw new ArgumentOutOfRangeException(nameof(info.iccProfileIntent));
            }

            if (info.iccProfileType == ColorSpaceType.LCS_CALIBRATED_RGB)
            {
                var primaries = lcms.GetPrimariesFromBMPEndpoints(
                      info.dibHeader.bV5Endpoints_1x, info.dibHeader.bV5Endpoints_1y,
                      info.dibHeader.bV5Endpoints_2x, info.dibHeader.bV5Endpoints_2y,
                      info.dibHeader.bV5Endpoints_3x, info.dibHeader.bV5Endpoints_3y);

                var whitepoint = lcms.GetWhitePoint_sRGB();

                TransformCalibratedBGRA8(
                    primaries, whitepoint,
                    info.dibHeader.bV5GammaRed, info.dibHeader.bV5GammaGreen, info.dibHeader.bV5GammaBlue,
                    dataBuffer, info.imgWidth, info.imgHeight, dataStride, lcmsIntent);
            }
            else if (info.iccProfileType == ColorSpaceType.PROFILE_EMBEDDED)
            {
                TransformEmbeddedBGRA8((source + info.iccProfileOffset), info.iccProfileSize, dataBuffer, info.imgWidth, info.imgHeight, dataStride, lcmsIntent);
            }
            else
            {
                throw new NotSupportedException(info.iccProfileType.ToString());
            }
        }

        public unsafe static void TransformEmbeddedBGRA8(void* profilePtr, uint profileSize, void* data, int width, int height, int stride, Intent intent)
        {
            var source = OpenProfileFromMem(profilePtr, (int)profileSize);
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
            }
        }

        public unsafe static void TransformCalibratedBGRA8(CIExyYTRIPLE primaries, CIExyY whitePoint, uint red_gamma, uint green_gamma, uint blue_gamma,
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

    internal enum BmpCorePixelFormat
    {
        // Unsupported WPF formats
        //Rgba128Float,
        //Gray32Float,
        //Gray16,
        //Prgba64,
        //Rgba64,
        //Rgb48,
        //Rgb128Float,
        //Gray8,
        //Gray4,
        //Gray2,
        //BlackWhite,
        //Prgba128Float,
        //Cmyk32,

        // Unsupported GDI formats
        //Undefined,
        //DontCare,
        //Indexed,
        //Gdi,
        //Alpha,
        //PAlpha,
        //Extended,
        //Format16bppGrayScale,
        //Canonical,
        //Format64bppArgb,
        //Format16bppRgb555,
        //Format16bppRgb565,
        //Format24bppRgb,
        //Format32bppRgb,
        //Format1bppIndexed,
        //Format4bppIndexed,
        //Format8bppIndexed,
        //Format16bppArgb1555,
        //Format32bppPArgb,
        //Format64bppPArgb,
        //Format32bppArgb,

        Pbgra32,
        Bgra32,
        Bgr32,
        Bgr101010,
        Rgb24,
        Bgr24,
        Bgr565,
        Bgr555,
        Bgra5551,
        Indexed8,
        Indexed4,
        Indexed2,
        Indexed1,
    }

    internal struct BITMAP_DETAILS
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
