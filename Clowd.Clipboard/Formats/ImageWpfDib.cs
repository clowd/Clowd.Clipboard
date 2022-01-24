using System.Windows.Media.Imaging;
using Clowd.Clipboard.Bitmaps.Core;
using Clowd.Clipboard.Bitmaps;

namespace Clowd.Clipboard.Formats
{
    /// <summary>
    /// Converts a CF_DIB to/from a WPF BitmapSource.
    /// </summary>
    public unsafe class ImageWpfDib : BytesDataConverterBase<BitmapSource>
    {
        /// <inheritdoc/>
        public override BitmapSource ReadFromBytes(byte[] data)
        {
            fixed (byte* dataptr = data)
            {
                uint bcrFlags = BitmapCore.BC_READ_PRESERVE_INVALID_ALPHA;
                BitmapCore.ReadHeader(dataptr, data.Length, out var info, bcrFlags);
                return BitmapWpfInternal.Read(ref info, (dataptr + info.imgDataOffset), bcrFlags);
            }
        }

        /// <inheritdoc/>
        public override byte[] WriteToBytes(BitmapSource obj)
        {
            return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_VINFO);
        }
    }

    /// <summary>
    /// Converts a CF_DIBV5 to/from a WPF BitmapSource.
    /// </summary>
    public unsafe class ImageWpfDibV5 : ImageWpfDib
    {
        /// <inheritdoc/>
        public override byte[] WriteToBytes(BitmapSource obj)
        {
            return BitmapWpfInternal.GetBytes(obj, BitmapCore.BC_WRITE_SKIP_FH | BitmapCore.BC_WRITE_V5);
        }
    }
}
