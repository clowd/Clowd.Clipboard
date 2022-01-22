using Clowd.Clipboard.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Clowd.Clipboard.Bitmaps.Core;

namespace Clowd.Clipboard
{
    //public interface IClipboardHandle : IDisposable
    //{
    //    // MISC
    //    IEnumerable<ClipboardFormat> GetPresentFormats();
    //    void Empty();

    //    // TEXT
    //    string GetText();
    //    void SetText(string text);

    //    // IMAGE
    //    BitmapSource GetImage();
    //    void SetImage(BitmapSource bitmap);

    //    // FILE DROP
    //    string[] GetFileDropList();
    //    void SetFileDropList(string[] files);

    //    // CUSTOM
    //    void SetFormat<T>(ClipboardFormat<T> format, T obj);
    //    void SetFormat(ClipboardFormat format, byte[] bytes);
    //    byte[] GetFormat(ClipboardFormat format);
    //    T GetFormat<T>(ClipboardFormat<T> format);
    //}

    public class ClipboardHandle : IDisposable
    {
        public bool IsDisposed { get; private set; }

        static readonly WindowProcedureHandler _wndProc;
        static readonly IntPtr _hWindow;
        static readonly short _clsAtom;
        static readonly string _clsName;
        bool _cleared;
        bool _isOpen;

        private const int RETRY_COUNT = 10;
        private const int RETRY_DELAY = 100;

        static ClipboardHandle()
        {
            _wndProc = NativeMethods.DefWindowProc;
            _clsName = "ClipboardGap_" + DateTime.Now.Ticks;

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

        protected void ThrowOpenFailed()
        {
            var hr = Marshal.GetLastWin32Error();
            var mex = Marshal.GetExceptionForHR(hr);

            if (hr == 5)  // ACCESS DENIED
            {
                IntPtr hwnd = NativeMethods.GetOpenClipboardWindow();
                if (hwnd != IntPtr.Zero)
                {
                    uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
                    string processName = "Unknown";
                    try
                    {
                        var p = Process.GetProcessById((int)processId);
                        processName = p.ProcessName;
                    }
                    catch { }

                    throw new ClipboardBusyException((int)processId, processName, mex);

                }
                else
                {
                    throw new ClipboardBusyException(mex);
                }
            }

            throw mex;
        }

        public void Open()
        {
            int i = RETRY_COUNT;
            while (true)
            {
                var success = NativeMethods.OpenClipboard(_hWindow);
                if (success) break;
                if (--i == 0) ThrowOpenFailed();
                Thread.Sleep(RETRY_DELAY);
            }
            _isOpen = true;
        }

        public async Task OpenAsync()
        {
            int i = RETRY_COUNT;
            while (true)
            {
                var success = NativeMethods.OpenClipboard(_hWindow);
                if (success) break;
                if (--i == 0) ThrowOpenFailed();
                await Task.Delay(RETRY_DELAY);
            }
            _isOpen = true;
        }

        public virtual void Empty()
        {
            ThrowIfDisposed();

            if (!NativeMethods.EmptyClipboard())
                throw new Win32Exception();

            _cleared = true;
        }

        public virtual IEnumerable<ClipboardFormat> GetPresentFormats()
        {
            ThrowIfDisposed();

            uint next = NativeMethods.EnumClipboardFormats(0);
            while (next != 0)
            {
                yield return ClipboardFormat.GetFormatById(next);
                next = NativeMethods.EnumClipboardFormats(next);
            }

            // If there are no more clipboard formats to enumerate, the return value is zero. 
            // In this case, the GetLastError function returns the value ERROR_SUCCESS.
            var err = Marshal.GetLastWin32Error();
            if (err != 0)
                Marshal.ThrowExceptionForHR(err);
        }

        public virtual string GetText()
        {
            // windows doesn't screw up text like it does with images, so we're OK with windows
            // giving us a synthesized result here in our preferred format.

            var fmtUni = ClipboardFormat.UnicodeText;
            if (TryGetFormatObject(fmtUni.Id, fmtUni.TypeObjectReader, out var unicodeText))
                return unicodeText;

            return null;
        }

        public virtual void SetText(string text)
        {
            SetFormat(ClipboardFormat.UnicodeText, text);
        }

        public virtual void SetImage(BitmapSource bitmap)
        {
            // Write PNG format as some applications do not support alpha in DIB's and
            // also often will attempt to read PNG format first.
            SetFormat(ClipboardFormat.Png, bitmap);
            SetFormat(ClipboardFormat.DibV5, bitmap);
        }

        public virtual BitmapSource GetImage()
        {
            var fmtPng = ClipboardFormat.Png;
            if (TryGetFormatObject(fmtPng.Id, fmtPng.TypeObjectReader, out var png))
                if (png != null)
                    return png;

            // Windows has "Synthesized Formats", if you ask for a CF_DIBV5 when there is only a CF_DIB, it will transparently convert
            // from one format to the other. The issue is, if you ask for a CF_DIBV5 before you ask for a CF_DIB, and the CF_DIB is 
            // the only real format on the clipboard, windows can corrupt the CF_DIB!!! 
            // One quirk is that windows deterministically puts real formats in the list of present formats before it puts synthesized formats
            // so even though we can't really tell what is synthesized or not, we can make a guess based on which comes first.

            foreach (var fmt in GetPresentFormats().OfType<ClipboardFormat<BitmapSource>>())
                if (fmt == ClipboardFormat.Bitmap || fmt == ClipboardFormat.Dib || fmt == ClipboardFormat.DibV5)
                    if (TryGetFormatObject(fmt.Id, fmt.TypeObjectReader, out var dib))
                        if (dib != null)
                            return dib;

            var fmtDrop = ClipboardFormat.FileDrop;
            if (TryGetFormatObject(fmtDrop.Id, new ImageWpfFileDrop(), out var drop))
                if (drop != null)
                    return drop;

            return null;
        }

        public virtual string[] GetFileDropList()
        {
            var fmtDrop = ClipboardFormat.FileDrop;
            if (TryGetFormatObject(fmtDrop.Id, fmtDrop.TypeObjectReader, out var drop))
                if (drop != null)
                    return drop;

            // some native applications still use these, but they are deprecated/discouraged.

#pragma warning disable CS0612 // Type or member is obsolete
            var fmtLegacyW = ClipboardFormat.FileNameW;
            var fmtLegacyA = ClipboardFormat.FileName;
#pragma warning restore CS0612 // Type or member is obsolete

            if (TryGetFormatObject(fmtLegacyW.Id, fmtLegacyW.TypeObjectReader, out var legacyW))
                if (legacyW != null)
                    return new[] { legacyW };

            if (TryGetFormatObject(fmtLegacyA.Id, fmtLegacyA.TypeObjectReader, out var legacyA))
                if (legacyA != null)
                    return new[] { legacyA };

            return null;
        }

        public virtual void SetFileDropList(string[] files)
        {
            SetFormat(ClipboardFormat.FileDrop, files);
        }

        public virtual bool TryGetFormatType<T>(ClipboardFormat<T> format, out T value)
        {
            return TryGetFormatObject(format.Id, format.TypeObjectReader, out value);
        }

        public virtual T GetFormatType<T>(ClipboardFormat<T> format)
        {
            return GetFormatObject(format.Id, format.TypeObjectReader);
        }

        public virtual bool TryGetFormatBytes(ClipboardFormat format, out byte[] bytes)
        {
            return TryGetFormatObject(format.Id, new BytesDataConverter(), out bytes);
        }

        public virtual byte[] GetFormatBytes(ClipboardFormat format)
        {
            return GetFormatObject(format.Id, new BytesDataConverter());
        }

        public virtual bool TryGetFormatStream(ClipboardFormat format, out Stream stream)
        {
            if (TryGetFormatObject(format.Id, new BytesDataConverter(), out var bytes))
            {
                if (bytes != null)
                {
                    stream = new MemoryStream(bytes);
                    return true;
                }
            }

            stream = default;
            return false;
        }

        public virtual Stream GetFormatStream(ClipboardFormat format)
        {
            return new MemoryStream(GetFormatBytes(format));
        }

        public virtual void SetFormat<T>(ClipboardFormat<T> format, T obj)
        {
            SetFormatObject(format.Id, obj, format.TypeObjectReader);
        }

        public virtual void SetFormat(ClipboardFormat format, byte[] bytes)
        {
            SetFormatObject(format.Id, bytes, new BytesDataConverter());
        }

        public virtual void SetFormat(ClipboardFormat format, Stream stream)
        {
            var bytes = StructUtil.ReadBytes(stream);
            SetFormatObject(format.Id, bytes, new BytesDataConverter());
        }

        protected virtual bool TryGetFormatObject<T>(uint format, IDataConverter<T> converter, out T value)
        {
            try
            {
                value = GetFormatObject(format, converter);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        protected virtual T GetFormatObject<T>(uint format, IDataConverter<T> converter)
        {
            ThrowIfDisposed();

            var hglobal = NativeMethods.GetClipboardData(format);

            if (hglobal == IntPtr.Zero)
            {
                var err = Marshal.GetLastWin32Error();
                if (err == 0)
                {
                    throw new Exception("Clipboard data could not be retrieved for this format, is it currently present?");
                }
                else
                {
                    throw new Win32Exception(err);
                }
            }

            return converter.ReadFromHGlobal(hglobal);
        }

        protected virtual void SetFormatObject<T>(uint cfFormat, T obj, IDataConverter<T> converter)
        {
            ThrowIfDisposed();

            // EmptyClipboard must be called to update the current clipboard owner before setting data
            if (!_cleared)
                Empty();

            var hglobal = converter.WriteToHGlobal(obj);
            if (hglobal == IntPtr.Zero)
                throw new Exception("Unable to copy data into global memory");

            try
            {
                var hdata = NativeMethods.SetClipboardData(cfFormat, hglobal);
                if (hdata == IntPtr.Zero)
                    throw new Win32Exception();
            }
            catch
            {
                // free hglobal only if error, if success - ownership of hglobal has transferred to system
                NativeMethods.GlobalFree(hglobal);
            }
        }

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            NativeMethods.CloseClipboard();
        }

        protected virtual void ThrowIfDisposed()
        {
            if (!_isOpen)
                throw new InvalidOperationException("The clipboard is not yet open, please call Open() or OpenAsync() first.");

            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ClipboardHandle));
        }
    }
}
