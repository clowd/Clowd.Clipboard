using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;

namespace Clowd.Clipboard.Formats
{
    /// <summary>
    /// Base class to store and read 32-bit integers on the clipboard and convert them to a managed type (eg. enums).
    /// </summary>
    public abstract class Int32Base<T> : HandleDataConverterBase<T>
    {
        /// <summary>
        /// Reads the specified integer and converts it into the target managed object.
        /// </summary>
        public abstract T ReadFromInt32(int val);

        /// <summary>
        /// Convert the specified managed object into it's integer representation.
        /// </summary>
        public abstract int WriteToInt32(T obj);

        /// <inheritdoc/>
        public override int GetDataSize(T obj) => sizeof(int);

        /// <inheritdoc/>
        public override T ReadFromHandle(IntPtr ptr, int memSize) => ReadFromInt32(Marshal.ReadInt32(ptr));

        /// <inheritdoc/>
        public override void WriteToHandle(T obj, IntPtr ptr) => Marshal.WriteInt32(ptr, WriteToInt32(obj));
    }

    /// <summary>
    /// Used by CF_LOCALE, which is stored as an integer (lcid) and is represented by <see cref="CultureInfo"/>.
    /// </summary>
    public class Locale : Int32Base<CultureInfo>
    {
        /// <inheritdoc/>
        public override CultureInfo ReadFromInt32(int val) => new CultureInfo(val);

        /// <inheritdoc/>
        public override int WriteToInt32(CultureInfo obj) => obj.LCID;
    }

    /// <summary>
    /// Converts a clipboard Drop Effect into <see cref="DragDropEffects"/>.
    /// </summary>
    public class DropEffect : Int32Base<DragDropEffects>
    {
        /// <inheritdoc/>
        public override DragDropEffects ReadFromInt32(int val) => (DragDropEffects)val;

        /// <inheritdoc/>
        public override int WriteToInt32(DragDropEffects obj) => (int)obj;
    }
}
