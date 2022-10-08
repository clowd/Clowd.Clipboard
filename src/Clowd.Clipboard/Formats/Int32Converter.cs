using System.Globalization;
using System.Runtime.InteropServices;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Base class to store and read 32-bit integers on the clipboard and convert them to a managed type (eg. enums).
/// </summary>
public abstract class Int32DataConverterBase<T> : HandleDataConverterBase<T>
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
/// Reads an Int32 from the clipboard.
/// </summary>
public class Int32DataConverter : Int32DataConverterBase<int>
{
    /// <inheritdoc/>
    public override int ReadFromInt32(int val) => val;

    /// <inheritdoc/>
    public override int WriteToInt32(int obj) => obj;
}
