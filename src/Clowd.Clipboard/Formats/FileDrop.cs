using System.Runtime.InteropServices;
using System.Text;

namespace Clowd.Clipboard.Formats;

/// <summary>
/// Converter for native windows file drop lists containing a list of file paths.
/// </summary>
[SupportedOSPlatform("windows")]
public class FileDrop : HandleDataConverterBase<string[]>
{
    const int PATH_MAX_LEN = 260;
    const int PATH_LONG_MAX_LEN = short.MaxValue;
    const int baseStructSize = 4 + 8 + 4 + 4;

    /// <inheritdoc/>
    public override string[] ReadFromHandle(IntPtr hdrop, int memSize)
    {
        string[] files = null;
        StringBuilder sb = new StringBuilder(PATH_MAX_LEN);

        int count = NativeMethods.DragQueryFile(hdrop, unchecked((int)0xFFFFFFFF), null, 0);
        if (count > 0)
        {
            files = new string[count];

            for (int i = 0; i < count; i++)
            {
                int charlen = DragQueryFileLongPath(hdrop, i, sb);
                if (0 == charlen)
                    continue;

                files[i] = sb.ToString(0, charlen);
            }
        }

        return files;
    }

    private static int DragQueryFileLongPath(IntPtr hDrop, int iFile, StringBuilder lpszFile)
    {
        if (null != lpszFile && 0 != lpszFile.Capacity && iFile != unchecked((int)0xFFFFFFFF))
        {
            int resultValue = 0;

            // iterating by allocating chunk of memory each time we find the length is not sufficient.
            // Performance should not be an issue for current MAX_PATH length due to this
            if ((resultValue = NativeMethods.DragQueryFile(hDrop, iFile, lpszFile, lpszFile.Capacity)) == lpszFile.Capacity)
            {
                // passing null for buffer will return actual number of charectors in the file name.
                // So, one extra call would be suffice to avoid while loop in case of long path.
                int capacity = NativeMethods.DragQueryFile(hDrop, iFile, null, 0);
                if (capacity < PATH_LONG_MAX_LEN)
                {
                    lpszFile.EnsureCapacity(capacity);
                    resultValue = NativeMethods.DragQueryFile(hDrop, iFile, lpszFile, capacity);
                }
                else
                {
                    resultValue = 0;
                }
            }

            lpszFile.Length = resultValue;
            return resultValue;  // what ever the result.
        }
        else
        {
            return NativeMethods.DragQueryFile(hDrop, iFile, lpszFile, lpszFile.Capacity);
        }
    }

    /// <inheritdoc/>
    public override int GetDataSize(string[] files)
    {
        bool unicode = (Marshal.SystemDefaultCharSize != 1);
        int sizeInBytes = baseStructSize;

        if (unicode)
        {
            for (int i = 0; i < files.Length; i++)
            {
                sizeInBytes += (files[i].Length + 1) * 2;
            }
            sizeInBytes += 2;
        }
        else
        {
            for (int i = 0; i < files.Length; i++)
            {
                sizeInBytes += GetPInvokeStringLength(files[i]) + 1;
            }
            sizeInBytes++;
        }
        return sizeInBytes;
    }

    int GetPInvokeStringLength(String s)
    {
        if (s == null)
        {
            return 0;
        }

        if (Marshal.SystemDefaultCharSize == 2)
        {
            return s.Length;
        }
        else
        {
            if (s.Length == 0)
            {
                return 0;
            }
            if (s.IndexOf('\0') > -1)
            {
                return GetEmbeddedNullStringLengthAnsi(s);
            }
            else
            {
                return NativeMethods.lstrlen(s);
            }
        }
    }

    int GetEmbeddedNullStringLengthAnsi(String s)
    {
        int n = s.IndexOf('\0');
        if (n > -1)
        {
            String left = s.Substring(0, n);
            String right = s.Substring(n + 1);
            return GetPInvokeStringLength(left) + GetEmbeddedNullStringLengthAnsi(right) + 1;
        }
        else
        {
            return GetPInvokeStringLength(s);
        }
    }

    /// <inheritdoc/>
    public override void WriteToHandle(string[] files, IntPtr currentPtr)
    {
        //if (files == null)
        //{
        //    return NativeMethods.S_OK;
        //}
        //else if (files.Length < 1)
        //{
        //    return NativeMethods.S_OK;
        //}
        //if (handle == IntPtr.Zero)
        //{
        //    return (NativeMethods.E_INVALIDARG);
        //}


        bool unicode = (Marshal.SystemDefaultCharSize != 1);

        //IntPtr currentPtr = IntPtr.Zero;
        //int sizeInBytes = baseStructSize;

        //// First determine the size of the array
        //if (unicode)
        //{
        //    for (int i = 0; i < files.Length; i++)
        //    {
        //        sizeInBytes += (files[i].Length + 1) * 2;
        //    }
        //    sizeInBytes += 2;
        //}
        //else
        //{
        //    for (int i = 0; i < files.Length; i++)
        //    {
        //        sizeInBytes += GetPInvokeStringLength(files[i]) + 1;
        //    }
        //    sizeInBytes++;
        //}

        //// Alloc the Win32 memory
        //IntPtr newHandle = UnsafeNativeMethods.GlobalReAlloc(new HandleRef(null, handle),
        //                                      sizeInBytes,
        //                                      NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_DDESHARE);
        //if (newHandle == IntPtr.Zero)
        //{
        //    return (NativeMethods.E_OUTOFMEMORY);
        //}
        //IntPtr basePtr = UnsafeNativeMethods.GlobalLock(new HandleRef(null, newHandle));
        //if (basePtr == IntPtr.Zero)
        //{
        //    return (NativeMethods.E_OUTOFMEMORY);
        //}
        //currentPtr = basePtr;

        // Write out the struct...
        //
        int[] structData = new int[] { baseStructSize, 0, 0, 0, 0 };

        if (unicode)
        {
            structData[4] = unchecked((int)0xFFFFFFFF);
        }

        Marshal.Copy(structData, 0, currentPtr, structData.Length);
        currentPtr = (IntPtr)((long)currentPtr + baseStructSize);

        for (int i = 0; i < files.Length; i++)
        {
            if (unicode)
            {

                NativeMethods.CopyMemoryW(currentPtr, files[i].ToCharArray(), files[i].Length * 2);
                currentPtr = (IntPtr)((long)currentPtr + (files[i].Length * 2));
                Marshal.Copy(new byte[] { 0, 0 }, 0, currentPtr, 2);
                currentPtr = (IntPtr)((long)currentPtr + 2);
            }
            else
            {
                int pinvokeLen = GetPInvokeStringLength(files[i]);
                NativeMethods.CopyMemoryA(currentPtr, files[i].ToCharArray(), pinvokeLen);
                currentPtr = (IntPtr)((long)currentPtr + pinvokeLen);
                Marshal.Copy(new byte[] { 0 }, 0, currentPtr, 1);
                currentPtr = (IntPtr)((long)currentPtr + 1);
            }
        }

        if (unicode)
        {
            Marshal.Copy(new char[] { '\0' }, 0, currentPtr, 1);
            //currentPtr = (IntPtr)((long)currentPtr + 2);
        }
        else
        {
            Marshal.Copy(new byte[] { 0 }, 0, currentPtr, 1);
            //currentPtr = (IntPtr)((long)currentPtr + 1);
        }

        //UnsafeNativeMethods.GlobalUnlock(new HandleRef(null, newHandle));
        //return NativeMethods.S_OK;
    }
}
