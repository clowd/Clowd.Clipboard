using System.Windows.Media.Imaging;

namespace Clowd.Clipboard
{
    /// <summary>
    /// Provides static methods for easy access to some of the most basic functionality of <see cref="ClipboardHandle"/>.
    /// </summary>
    public static class Clipboard
    {
        /// <summary>
        /// Opens a clipboard handle and returns it. You are responsible for disposing of it when done.
        /// </summary>
        public static ClipboardHandle Open()
        {
            var ch = new ClipboardHandle();
            ch.Open();
            return ch;
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.Empty"/>
        /// </summary>
        public static void Empty()
        {
            using var ch = Open();
            ch.Empty();
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.GetText"/>
        /// </summary>
        public static string GetText()
        {
            using var ch = Open();
            return ch.GetText();
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.SetText"/>
        /// </summary>
        public static void SetText(string text)
        {
            using var ch = Open();
            ch.SetText(text);
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.SetImage"/>
        /// </summary>
        public static void SetImage(BitmapSource bitmap)
        {
            using var ch = Open();
            ch.SetImage(bitmap);
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.GetImage"/>
        /// </summary>
        public static BitmapSource GetImage()
        {
            using var ch = Open();
            return ch.GetImage();
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.GetFileDropList"/>
        /// </summary>
        public static string[] GetFileDropList()
        {
            using var ch = Open();
            return ch.GetFileDropList();
        }

        /// <summary>
        /// <inheritdoc cref="ClipboardHandle.SetFileDropList"/>
        /// </summary>
        public static void SetFileDropList(string[] files)
        {
            using var ch = Open();
            ch.SetFileDropList(files);
        }
    }
}
