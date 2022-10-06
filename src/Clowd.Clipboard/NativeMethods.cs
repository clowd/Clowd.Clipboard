using System.Runtime.InteropServices;
using System.Text;

namespace Clowd.Clipboard;

[SupportedOSPlatform("windows")]
internal class NativeMethods
{
    public const int S_OK = 0x00000000;
    public const int S_FALSE = 0x00000001;

    public const int
        E_NOTIMPL = unchecked((int)0x80004001),
        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        E_INVALIDARG = unchecked((int)0x80070057),
        E_NOINTERFACE = unchecked((int)0x80004002),
        E_FAIL = unchecked((int)0x80004005),
        E_ABORT = unchecked((int)0x80004004),
        E_ACCESSDENIED = unchecked((int)0x80070005),
        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        CO_E_UNINITIALIZED = -2147221008,
        CLIPBRD_E_CANT_OPEN = -2147221040;

    public const int GMEM_MOVEABLE = 0x0002;
    public const int GMEM_ZEROINIT = 0x0040;

    // SHELL32

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern int DragQueryFile(IntPtr hDrop, int iFile, StringBuilder lpszFile, int cch);

    // USER32

    [DllImport("user32.dll")]
    public static extern IntPtr GetOpenClipboardWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
    public static extern int GetClipboardFormatName(uint format, StringBuilder lpString, int cchMax);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
    public static extern uint RegisterClipboardFormat(string format);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
    public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y,
        int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll")]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wparam, IntPtr lparam);

    [DllImport("user32.dll", EntryPoint = "RegisterClassW", SetLastError = true)]
    public static extern short RegisterClass(ref WindowClass lpWndClass);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW")]
    public static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int CountClipboardFormats();

    // KERNEL32

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GlobalAlloc(int uFlags, int dwBytes);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GlobalLock(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern bool GlobalUnlock(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern int GlobalSize(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern IntPtr GlobalFree(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern IntPtr GlobalReAlloc(IntPtr handle, int bytes, int flags);

    [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory", CharSet = CharSet.Unicode)]
    public static extern void CopyMemoryW(IntPtr pdst, char[] psrc, int cb);

    [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
    public static extern void CopyMemory(IntPtr pdst, byte[] psrc, int cb);

    [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory", CharSet = CharSet.Ansi)]
    public static extern void CopyMemoryA(IntPtr pdst, char[] psrc, int cb);

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int WideCharToMultiByte(int codePage, int flags, [MarshalAs(UnmanagedType.LPWStr)]string wideStr, int chars, [In, Out]byte[] pOutBytes, int bufferBytes, IntPtr defaultChar, IntPtr pDefaultUsed);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern int lstrlen(String s);

    // GDI32

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}
