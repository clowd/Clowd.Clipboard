using System.Windows.Media.Imaging;

namespace Clowd.Clipboard.Formats
{
    /// <summary>
    /// Base class for encoding images to/from bytes using the WPF/WIC encoder classes.
    /// </summary>
    public abstract class ImageWpfBasicEncoder : BytesDataConverterBase<BitmapSource>
    {
        /// <inheritdoc/>
        public override BitmapSource ReadFromBytes(byte[] data)
        {
            var decoder = GetDecoder(new MemoryStream(data));
            BitmapSource bitmapSource = decoder.Frames[0];
            return bitmapSource;
        }

        /// <inheritdoc/>
        public override byte[] WriteToBytes(BitmapSource obj)
        {
            var stream = new MemoryStream();
            var encoder = GetEncoder();
            if (obj is BitmapFrame frame) encoder.Frames.Add(frame);
            else encoder.Frames.Add(BitmapFrame.Create(obj));
            encoder.Save(stream);
            return stream.GetBuffer();
        }

        /// <summary>
        /// Creates an image decoder from the specified stream.
        /// </summary>
        protected abstract BitmapDecoder GetDecoder(Stream stream);

        /// <summary>
        /// Creates an image encoder.
        /// </summary>
        protected abstract BitmapEncoder GetEncoder();
    }

    /// <summary>
    /// Converts an image using <see cref="PngBitmapDecoder"/> and <see cref="PngBitmapEncoder"/>.
    /// </summary>
    public class ImageWpfBasicEncoderPng : ImageWpfBasicEncoder
    {
        /// <inheritdoc/>
        protected override BitmapDecoder GetDecoder(Stream stream) => new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        /// <inheritdoc/>
        protected override BitmapEncoder GetEncoder() => new PngBitmapEncoder();
    }

    /// <summary>
    /// Converts an image using <see cref="JpegBitmapDecoder"/> and <see cref="JpegBitmapEncoder"/>.
    /// </summary>
    public class ImageWpfBasicEncoderJpeg : ImageWpfBasicEncoder
    {
        /// <inheritdoc/>
        protected override BitmapDecoder GetDecoder(Stream stream) => new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        /// <inheritdoc/>
        protected override BitmapEncoder GetEncoder() => new JpegBitmapEncoder();
    }

    /// <summary>
    /// Converts an image using <see cref="GifBitmapDecoder"/> and <see cref="GifBitmapEncoder"/>.
    /// </summary>
    public class ImageWpfBasicEncoderGif : ImageWpfBasicEncoder
    {
        /// <inheritdoc/>
        protected override BitmapDecoder GetDecoder(Stream stream) => new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        /// <inheritdoc/>
        protected override BitmapEncoder GetEncoder() => new GifBitmapEncoder();
    }

    /// <summary>
    /// Converts an image using <see cref="TiffBitmapDecoder"/> and <see cref="TiffBitmapEncoder"/>.
    /// </summary>
    public class ImageWpfBasicEncoderTiff : ImageWpfBasicEncoder
    {
        /// <inheritdoc/>
        protected override BitmapDecoder GetDecoder(Stream stream) => new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        /// <inheritdoc/>
        protected override BitmapEncoder GetEncoder() => new TiffBitmapEncoder();
    }
}
