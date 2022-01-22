namespace Clowd.Clipboard.Bitmaps.Core;

internal unsafe class BitmapCorePixelReader
{
    public static void ConvertChannelBGRA(ref BITMAP_READ_DETAILS info, BitmapCorePixelFormat convertToFmt, byte* sourceBufferStart, byte* destBufferStart, bool preserveFakeAlpha)
    {
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
            shiftR = StructUtil.CalcShift(maskR);
            maxR = maskR >> shiftR;
            multR = (uint)(Math.Ceiling(255d / maxR * 65536 * 256)); // bitshift << 24
        }

        if (maskG != 0)
        {
            shiftG = StructUtil.CalcShift(maskG);
            maxG = maskG >> shiftG;
            multG = (uint)(Math.Ceiling(255d / maxG * 65536 * 256));
        }

        if (maskB != 0)
        {
            shiftB = StructUtil.CalcShift(maskB);
            maxB = maskB >> shiftB;
            multB = (uint)(Math.Ceiling(255d / maxB * 65536 * 256));
        }

        if (maskA != 0)
        {
            shiftA = StructUtil.CalcShift(maskA);
            maxA = maskA >> shiftA;
            multA = (uint)(Math.Ceiling(255d / maxA * 65536 * 256)); // bitshift << 24
        }

        var write = convertToFmt.Write;
        uint source_stride = StructUtil.CalcStride(nbits, width);
        uint dest_stride = StructUtil.CalcStride(convertToFmt.BitsPerPixel, width);

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

    public static void ReadIndexedTo32(ref BITMAP_READ_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
    {
        var palette = info.imgColorTable;
        var nbits = info.bbp;
        var width = info.imgWidth;
        var height = info.imgHeight;
        var upside_down = info.imgTopDown;

        uint source_stride = StructUtil.CalcStride(nbits, width);
        uint dest_stride = StructUtil.CalcStride(32, width);

        RGBQUAD color;
        int pal = palette.Length;
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
        else
        {
            throw new NotSupportedException($"Bitmap bits-per-pixel ({nbits}) is not supported.");
        }
    }

    public static void ReadRLE_32(ref BITMAP_READ_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
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

    public static void ReadHuffmanG31D(ref BITMAP_READ_DETAILS info, byte* sourceBufferStart, byte* destBufferStart)
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
}
