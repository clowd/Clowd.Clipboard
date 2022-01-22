using System;
using System.Windows.Media.Imaging;
using Clowd.Clipboard.Bitmaps.Core;
using Clowd.Clipboard.Bitmaps;

namespace Clowd.Clipboard.Formats
{
    public unsafe class ImageWpfDib : BytesDataConverterBase<BitmapSource>
    {
        public override BitmapSource ReadFromBytes(byte[] data)
        {
            fixed (byte* dataptr = data)
            {
                uint bcrFlags = BitmapCore.BC_READ_PRESERVE_INVALID_ALPHA;
                BitmapCore.ReadHeader(dataptr, data.Length, out var info, bcrFlags);
                return BitmapWpfInternal.Read(ref info, (dataptr + info.imgDataOffset), bcrFlags);
            }
        }

        public override byte[] WriteToBytes(BitmapSource obj)
        {
            return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_VINFO);
        }
    }

    public unsafe class ImageWpfDibV5 : ImageWpfDib
    {
        public override byte[] WriteToBytes(BitmapSource obj)
        {
            return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_V5);
        }
    }
}
