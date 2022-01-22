using Clowd.Clipboard.Bitmaps.Core;
namespace Clowd.Clipboard.Bitmaps.Core;

internal class BitFields
{
    public static readonly BITMASKS BITFIELDS_RGB_24 = new BITMASKS(0xff, 0xff00, 0xff0000);
    public static readonly BITMASKS BITFIELDS_BGR_24 = new BITMASKS(0xff0000, 0xff00, 0xff);
    public static readonly BITMASKS BITFIELDS_BGRA_32 = new BITMASKS(0xff0000, 0xff00, 0xff, 0xff000000);
    public static readonly BITMASKS BITFIELDS_BGR_565 = new BITMASKS(0xf800, 0x7e0, 0x1f);
    public static readonly BITMASKS BITFIELDS_BGRA_5551 = new BITMASKS(0x7c00, 0x03e0, 0x001f, 0x8000);
    public static readonly BITMASKS BITFIELDS_BGRA_555X = new BITMASKS(0x7c00, 0x03e0, 0x001f);
}

internal unsafe delegate byte* WritePixelToPtr(byte* ptr, byte b, byte g, byte r, byte a);

internal unsafe class BitmapCorePixelFormat : IEquatable<BitmapCorePixelFormat>
{
    public bool IsIndexed => BitsPerPixel < 16;
    public bool HasAlpha => Masks.maskAlpha != 0;

    public mscms.mscmsPxFormat? MscmsFormat { get; private set; }
    public BITMASKS Masks { get; private set; }
    public WritePixelToPtr Write { get; private set; }
    public ushort BitsPerPixel { get; private set; }

    private BitmapCorePixelFormat() { }

    public static readonly BitmapCorePixelFormat Indexed1 = new BitmapCorePixelFormat
    {
        BitsPerPixel = 1,
    };

    public static readonly BitmapCorePixelFormat Indexed2 = new BitmapCorePixelFormat
    {
        BitsPerPixel = 2,
    };

    public static readonly BitmapCorePixelFormat Indexed4 = new BitmapCorePixelFormat
    {
        BitsPerPixel = 4,
    };

    public static readonly BitmapCorePixelFormat Indexed8 = new BitmapCorePixelFormat
    {
        BitsPerPixel = 8,
    };

    public static readonly BitmapCorePixelFormat Bgr555X = new BitmapCorePixelFormat
    {
        MscmsFormat = mscms.mscmsPxFormat.BM_x555RGB,
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

    public static readonly BitmapCorePixelFormat Bgr5551 = new BitmapCorePixelFormat
    {
        MscmsFormat =  mscms.mscmsPxFormat.BM_x555RGB,
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

    public static readonly BitmapCorePixelFormat Bgr565 = new BitmapCorePixelFormat
    {
        MscmsFormat =  mscms.mscmsPxFormat.BM_565RGB,
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

    public static readonly BitmapCorePixelFormat Rgb24 = new BitmapCorePixelFormat
    {
        MscmsFormat =  mscms.mscmsPxFormat.BM_BGRTRIPLETS,
        BitsPerPixel = 24,
        Masks = BitFields.BITFIELDS_RGB_24,
        Write = (ptr, b, g, r, a) =>
        {
            *ptr++ = r;
            *ptr++ = g;
            *ptr++ = b;
            return ptr;
        },
    };

    public static readonly BitmapCorePixelFormat Bgr24 = new BitmapCorePixelFormat
    {
        MscmsFormat =  mscms.mscmsPxFormat.BM_RGBTRIPLETS,
        BitsPerPixel = 24,
        Masks = BitFields.BITFIELDS_BGR_24,
        Write = (ptr, b, g, r, a) =>
        {
            *ptr++ = b;
            *ptr++ = g;
            *ptr++ = r;
            return ptr;
        },
    };

    public static readonly BitmapCorePixelFormat Bgra32 = new BitmapCorePixelFormat
    {
        MscmsFormat =  mscms.mscmsPxFormat.BM_xRGBQUADS,
        BitsPerPixel = 32,
        Masks = BitFields.BITFIELDS_BGRA_32,
        Write = (ptr, b, g, r, a) =>
        {
            uint* dest = (uint*)ptr;
            *dest++ = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
            return (byte*)dest;
        },
    };

    public static readonly BitmapCorePixelFormat[] Formats = new BitmapCorePixelFormat[]
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
        if (obj is BitmapCorePixelFormat fmt) return fmt.Equals(this);
        return false;
    }

    public bool Equals(BitmapCorePixelFormat other)
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
