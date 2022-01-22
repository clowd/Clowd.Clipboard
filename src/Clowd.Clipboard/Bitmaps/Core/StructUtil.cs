using System.Runtime.InteropServices;

namespace Clowd.Clipboard.Bitmaps.Core;

internal unsafe class StructUtil
{
    public static T Deserialize<T>(byte* ptr)
    {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
    }

    public static void SerializeTo<T>(T s, byte[] buffer, ref uint destOffset) where T : struct
    {
        var size = Marshal.SizeOf(typeof(T));
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(s, ptr, true);
        Marshal.Copy(ptr, buffer, (int)destOffset, size);
        Marshal.FreeHGlobal(ptr);
        destOffset += (uint)size;
    }

    public static ushort ReadU16(byte* ptr)
    {
        var arr = new byte[] { *ptr, *(ptr + 1) };
        return BitConverter.ToUInt16(arr, 0);
    }

    public static uint ReadU24(byte* ptr)
    {
        var arr = new byte[] { *ptr, *(ptr + 1), *(ptr + 2) };
        return BitConverter.ToUInt32(arr, 0);
    }

    public static uint ReadU32(byte* ptr)
    {
        var arr = new byte[] { *ptr, *(ptr + 1), *(ptr + 2), *(ptr + 3) };
        return BitConverter.ToUInt32(arr, 0);
    }

    public static byte[] ReadBytes(Stream stream)
    {
        if (stream is MemoryStream mem)
        {
            return mem.ToArray();
        }
        else
        {
            byte[] buffer = new byte[4096];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
    }

    public static int CalcShift(uint mask)
    {
        for (int shift = 0; shift < sizeof(uint) * 8; ++shift)
        {
            if ((mask & (1 << shift)) != 0)
            {
                return shift;
            }
        }
        throw new NotSupportedException("Invalid Bitmask");
    }

    public static uint CalcStride(ushort bbp, int width) => (bbp * (uint)width + 31) / 32 * 4;
}
