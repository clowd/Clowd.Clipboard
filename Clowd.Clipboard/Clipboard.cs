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
        /// Clears everything on the clipboard. This is also done automatically before
        /// setting any data to the clipboard, so it does not need to be called explicitly
        /// unless you wish to completely empty the clipboard and leave it that way.
        /// </summary>
        public static void Empty()
        {
            using var ch = Open();
            ch.Empty();
        }

        /// <summary>
        /// Retrieves any text stored on the clipboard.
        /// </summary>
        public static string GetText()
        {
            using var ch = Open();
            return ch.GetText();
        }

        /// <summary>
        /// Sets the text on the clipboard to the specified string.
        /// </summary>
        public static void SetText(string text)
        {
            using var ch = Open();
            ch.SetText(text);
        }

        /// <summary>
        /// Sets the image on the clipboard to the specified bitmap.
        /// </summary>
        public static void SetImage(BitmapSource bitmap)
        {
            using var ch = Open();
            ch.SetImage(bitmap);
        }

        /// <summary>
        /// Retrieves any detectable bitmap stored on the clipboard.
        /// </summary>
        public static BitmapSource GetImage()
        {
            using var ch = Open();
            return ch.GetImage();
        }

        /// <summary>
        /// Retrieves the current file drop list on the clipboard, or returns null if there is none.
        /// </summary>
        public static string[] GetFileDropList()
        {
            using var ch = Open();
            return ch.GetFileDropList();
        }

        /// <summary>
        /// Set the file drop list on the clipboard to the specified list of strings.
        /// </summary>
        public static void SetFileDropList(string[] files)
        {
            using var ch = Open();
            ch.SetFileDropList(files);
        }
    }
}
