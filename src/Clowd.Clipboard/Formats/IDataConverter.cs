using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Clowd.Clipboard.Formats
{
    /// <summary>
    /// Basic interface for converting an object to/from a handle (HGlobal) that can be stored on the clipboard.
    /// </summary>
    public interface IDataConverter<T>
    {
        /// <summary>
        /// Write's the specified managed object into a handle (HGlobal) suitable for storage on the clipboard.
        /// </summary>
        IntPtr WriteToHGlobal(T obj);

        /// <summary>
        /// Reads the data at the specified handle (HGlobal) into a managed object.
        /// </summary>
        T ReadFromHGlobal(IntPtr hGlobal);
    }

    /// <summary>
    /// A class which reads the underlying HGlobal into an array of bytes.
    /// </summary>
    public class BytesDataConverter : BytesDataConverterBase<byte[]>
    {
        /// <inheritdoc/>
        public override byte[] ReadFromBytes(byte[] data) => data;

        /// <inheritdoc/>
        public override byte[] WriteToBytes(byte[] obj) => obj;
    }

    /// <summary>
    /// A base class which reads the underlying HGlobal into an array of bytes for further processing.
    /// </summary>
    public abstract class BytesDataConverterBase<T> : IDataConverter<T>
    {
        /// <summary>
        /// Writes the specified managed object into an array of bytes.
        /// </summary>
        public abstract byte[] WriteToBytes(T obj);

        /// <summary>
        /// Creates a new managed object by reading the array of bytes.
        /// </summary>
        public abstract T ReadFromBytes(byte[] data);

        /// <inheritdoc/>
        public virtual IntPtr WriteToHGlobal(T obj)
        {
            var bytes = WriteToBytes(obj);
            var size = bytes.Length;

            var hglobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, size);
            if (hglobal == IntPtr.Zero)
                throw new Win32Exception();

            var ptr = NativeMethods.GlobalLock(hglobal);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                Marshal.Copy(bytes, 0, ptr, size);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hglobal);
            }

            return hglobal;
        }

        /// <inheritdoc/>
        public virtual T ReadFromHGlobal(IntPtr hglobal)
        {
            var ptr = NativeMethods.GlobalLock(hglobal);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                var size = NativeMethods.GlobalSize(hglobal);
                byte[] bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                return ReadFromBytes(bytes);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hglobal);
            }
        }
    }

    /// <summary>
    /// A base class which locks an HGlobal to retrieve the underlying data pointer for further processing
    /// </summary>
    public abstract class HandleDataConverterBase<T> : IDataConverter<T>
    {
        /// <summary>
        /// Gets the size of the object in bytes. This function is called before 
        /// <see cref="WriteToHandle(T, IntPtr)"/> when determining how much memory to allocate.
        /// </summary>
        public abstract int GetDataSize(T obj);

        /// <summary>
        /// Writes the object to the specified unmanaged pointer.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="ptr"></param>
        public abstract void WriteToHandle(T obj, IntPtr ptr);

        /// <summary>
        /// Reads the data at the specified pointer.
        /// </summary>
        /// <param name="ptr">The unmanaged pointer.</param>
        /// <param name="memSize">The size of the data stored at the pointer.</param>
        /// <returns>The parsed object</returns>
        public abstract T ReadFromHandle(IntPtr ptr, int memSize);

        /// <inheritdoc/>
        public virtual T ReadFromHGlobal(IntPtr hglobal)
        {
            var ptr = NativeMethods.GlobalLock(hglobal);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                var size = NativeMethods.GlobalSize(hglobal);
                return ReadFromHandle(ptr, size);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hglobal);
            }
        }

        /// <inheritdoc/>
        public virtual IntPtr WriteToHGlobal(T obj)
        {
            var size = GetDataSize(obj);

            var hglobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, size);
            if (hglobal == IntPtr.Zero)
                throw new Win32Exception();

            var ptr = NativeMethods.GlobalLock(hglobal);
            if (ptr == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                WriteToHandle(obj, ptr);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hglobal);
            }

            return hglobal;
        }
    }
}
