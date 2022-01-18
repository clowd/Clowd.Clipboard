using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Clowd.ClipLib.Formats
{
    public class ImageWpfFileDrop : HandleDataConverterBase<BitmapSource>
    {
        private static string[] _knownImageExt = new[]
        {
            ".png", ".jpg", ".jpeg",".jpe", ".bmp",
            ".gif", ".tif", ".tiff", ".ico"
        };

        public override int GetDataSize(BitmapSource obj)
        {
            throw new NotImplementedException();
        }

        public override BitmapSource ReadFromHandle(IntPtr ptr, int memSize)
        {
            var reader = new FileDrop();
            var fileDropList = reader.ReadFromHandle(ptr, memSize);

            // if - there is a single file in the file drop list
            //    - the file in the file drop list is an image (file name ends with image extension)
            //    - the file exists on disk

            if (fileDropList != null && fileDropList.Length == 1)
            {
                var filePath = fileDropList[0];
                if (File.Exists(filePath) && _knownImageExt.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    return new BitmapImage(new Uri(filePath));
                }
            }

            return null;
        }

        public override void WriteToHandle(BitmapSource obj, IntPtr ptr)
        {
            throw new NotImplementedException();
        }
    }
}
