using System.Windows.Media.Imaging;

namespace Clowd.Clipboard.Formats
{
    public abstract class ImageWpfBasicEncoder : BytesDataConverterBase<BitmapSource>
    {
        public override BitmapSource ReadFromBytes(byte[] data)
        {
            var decoder = GetDecoder(new MemoryStream(data));
            BitmapSource bitmapSource = decoder.Frames[0];
            return bitmapSource;
        }

        public override byte[] WriteToBytes(BitmapSource obj)
        {
            var stream = new MemoryStream();
            var encoder = GetEncoder();
            if (obj is BitmapFrame frame) encoder.Frames.Add(frame);
            else encoder.Frames.Add(BitmapFrame.Create(obj));
            encoder.Save(stream);
            return stream.GetBuffer();
        }

        protected abstract BitmapDecoder GetDecoder(Stream stream);
        protected abstract BitmapEncoder GetEncoder();
    }

    public class ImageWpfBasicEncoderPng : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new PngBitmapEncoder();
    }

    public class ImageWpfBasicEncoderJpeg : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new JpegBitmapEncoder();
    }

    public class ImageWpfBasicEncoderGif : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new GifBitmapEncoder();
    }

    public class ImageWpfBasicEncoderTiff : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new TiffBitmapEncoder();
    }
}
