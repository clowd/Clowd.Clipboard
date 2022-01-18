using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Clowd.ClipLib.Formats
{
    public interface IDataConverter<T>
    {
        IntPtr WriteToHGlobal(T obj);
        T ReadFromHGlobal(IntPtr hGlobal);
    }

    public class BytesDataConverter : BytesDataConverterBase<byte[]>
    {
        public override byte[] ReadFromBytes(byte[] data) => data;

        public override byte[] WriteToBytes(byte[] obj) => obj;
    }

    public abstract class BytesDataConverterBase<T> : IDataConverter<T>
    {
        public abstract byte[] WriteToBytes(T obj);

        public abstract T ReadFromBytes(byte[] data);

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

    public abstract class HandleDataConverterBase<T> : IDataConverter<T>
    {
        public abstract int GetDataSize(T obj);

        public abstract void WriteToHandle(T obj, IntPtr ptr);

        public abstract T ReadFromHandle(IntPtr ptr, int memSize);

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
