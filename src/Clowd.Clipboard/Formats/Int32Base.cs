using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;

namespace Clowd.ClipLib.Formats
{
    public abstract class Int32Base<T> : HandleDataConverterBase<T>
    {
        public override int GetDataSize(T obj) => sizeof(int);
        public abstract T ReadFromInt32(int val);
        public abstract int WriteToInt32(T obj);
        public override T ReadFromHandle(IntPtr ptr, int memSize) => ReadFromInt32(Marshal.ReadInt32(ptr));
        public override void WriteToHandle(T obj, IntPtr ptr) => Marshal.WriteInt32(ptr, WriteToInt32(obj));
    }

    public class Locale : Int32Base<CultureInfo>
    {
        public override CultureInfo ReadFromInt32(int val) => new CultureInfo(val);
        public override int WriteToInt32(CultureInfo obj) => obj.LCID;
    }

    public class DropEffect : Int32Base<DragDropEffects>
    {
        public override DragDropEffects ReadFromInt32(int val) => (DragDropEffects)val;
        public override int WriteToInt32(DragDropEffects obj) => (int)obj;
    }
}
