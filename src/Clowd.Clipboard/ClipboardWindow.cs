using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clowd.Clipboard;

/// <summary>
/// Represents a static HWND window responsible for owning the clipboard when it is open.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClipboardWindow
{
    /// <summary>
    /// The window handle.
    /// </summary>
    public static IntPtr Handle => _hWindow;

    static readonly WindowProcedureHandler _wndProc;
    static readonly IntPtr _hWindow;
    static readonly short _clsAtom;
    static readonly string _clsName;

    static ClipboardWindow()
    {
        _wndProc = NativeMethods.DefWindowProc;
        _clsName = "ClowdClipboardLib_" + DateTime.Now.Ticks;

        WindowClass wc;
        wc.style = 0;
        wc.lpfnWndProc = _wndProc;
        wc.cbClsExtra = 0;
        wc.cbWndExtra = 0;
        wc.hInstance = IntPtr.Zero;
        wc.hIcon = IntPtr.Zero;
        wc.hCursor = IntPtr.Zero;
        wc.hbrBackground = IntPtr.Zero;
        wc.lpszMenuName = "";
        wc.lpszClassName = _clsName;

        // we just create one window for this process, it will be used for all future clipboard handles.
        // this class will be unregistered when the process exits.
        _clsAtom = NativeMethods.RegisterClass(ref wc);
        if (_clsAtom == 0)
            throw new Win32Exception();

        _hWindow = NativeMethods.CreateWindowEx(0, _clsName, "", 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_hWindow == IntPtr.Zero)
            throw new Win32Exception();
    }
}
