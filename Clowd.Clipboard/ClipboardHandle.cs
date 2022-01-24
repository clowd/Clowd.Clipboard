using Clowd.Clipboard.Formats;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Clowd.Clipboard.Bitmaps.Core;

namespace Clowd.Clipboard
{
    /// <summary>
    /// Represents a handle to the clipboard. Open the handle via <see cref="Open"/>, read or 
    /// set the clipboard, and then dispose this class as quickly as possible. Leaving this handle
    /// open for too long will prevent other applications from accessing the clipboard, and may 
    /// even cause them to freeze for a time.
    /// </summary>
    public class ClipboardHandle : IDisposable
    {
        private const int RETRY_COUNT = 10;
        private const int RETRY_DELAY = 100;

        static readonly WindowProcedureHandler _wndProc;
        static readonly IntPtr _hWindow;
        static readonly short _clsAtom;
        static readonly string _clsName;

        bool _disposed;
        bool _cleared;
        bool _isOpen;

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

        /// <summary>
        /// Reads the last Win32 error and throws a new <see cref="ClipboardBusyException"/>.
        /// </summary>
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

        /// <summary>
        /// Opens the system clipboard for reading or writing.
        /// </summary>
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

        /// <summary>
        /// Opens the system clipboard asynchronously for reading or writing. 
        /// </summary>
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

        /// <summary>
        /// Clears everything on the clipboard. This is also done automatically before
        /// setting any data to the clipboard, so it does not need to be called explicitly
        /// unless you wish to completely empty the clipboard and leave it that way.
        /// </summary>
        public virtual void Empty()
        {
            ThrowIfDisposed();

            if (!NativeMethods.EmptyClipboard())
                throw new Win32Exception();

            _cleared = true;
        }

        /// <summary>
        /// Returns a list of all the formats currently stored in the current clipboard object.
        /// </summary>
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

        /// <summary>
        /// Retrieves any text stored on the clipboard.
        /// </summary>
        public virtual string GetText()
        {
            // windows doesn't screw up text like it does with images, so we're OK with windows
            // giving us a synthesized result here in our preferred format.

            var fmtUni = ClipboardFormat.UnicodeText;
            if (TryGetFormatObject(fmtUni.Id, fmtUni.TypeObjectReader, out var unicodeText))
                return unicodeText;

            return null;
        }

        /// <summary>
        /// Sets the text on the clipboard to the specified string.
        /// </summary>
        public virtual void SetText(string text)
        {
            SetFormat(ClipboardFormat.UnicodeText, text);
        }

        /// <summary>
        /// Sets the image on the clipboard to the specified bitmap.
        /// </summary>
        public virtual void SetImage(BitmapSource bitmap)
        {
            // Write PNG format as some applications do not support alpha in DIB's and
            // also often will attempt to read PNG format first.
            SetFormat(ClipboardFormat.Png, bitmap);
            SetFormat(ClipboardFormat.DibV5, bitmap);
        }

        /// <summary>
        /// Retrieves any detectable bitmap stored on the clipboard.
        /// </summary>
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

        /// <summary>
        /// Retrieves the current file drop list on the clipboard, or returns null if there is none.
        /// </summary>
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

        /// <summary>
        /// Set the file drop list on the clipboard to the specified list of strings.
        /// </summary>
        public virtual void SetFileDropList(string[] files)
        {
            SetFormat(ClipboardFormat.FileDrop, files);
        }

        /// <summary>
        /// Tries to get a typed clipboard format object from the current clipboard. Returns false
        /// if the format does not exist, or fails for another reason.
        /// </summary>
        public virtual bool TryGetFormatType<T>(ClipboardFormat<T> format, out T value)
        {
            return TryGetFormatObject(format.Id, format.TypeObjectReader, out value);
        }

        /// <summary>
        /// Retrieves a typed clipboard format object from the current clipboard. Throws exception if
        /// the format is not currently on the clipboard.
        /// </summary>
        public virtual T GetFormatType<T>(ClipboardFormat<T> format)
        {
            return GetFormatObject(format.Id, format.TypeObjectReader);
        }

        /// <summary>
        /// Tries to get a typed clipboard format as bytes from the current clipboard. Returns false
        /// if the format does not exist, or fails for another reason.
        /// </summary>
        public virtual bool TryGetFormatBytes(ClipboardFormat format, out byte[] bytes)
        {
            return TryGetFormatObject(format.Id, new BytesDataConverter(), out bytes);
        }

        /// <summary>
        /// Retrieves a typed clipboard format as bytes from the current clipboard. Throws exception if
        /// the format is not currently on the clipboard.
        /// </summary>
        public virtual byte[] GetFormatBytes(ClipboardFormat format)
        {
            return GetFormatObject(format.Id, new BytesDataConverter());
        }

        /// <summary>
        /// Tries to get a typed clipboard format as a stream from the current clipboard. Returns false
        /// if the format does not exist, or fails for another reason.
        /// </summary>
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

        /// <summary>
        /// Retrieves a typed clipboard format as a stream from the current clipboard. Throws exception if
        /// the format is not currently on the clipboard.
        /// </summary>
        public virtual Stream GetFormatStream(ClipboardFormat format)
        {
            return new MemoryStream(GetFormatBytes(format));
        }

        /// <summary>
        /// Set clipboard format to the current clipboard. This will clear the clipboard
        /// if this is the first call to "Set" since the clipboard handle was opened.
        /// </summary>
        public virtual void SetFormat<T>(ClipboardFormat<T> format, T obj)
        {
            SetFormatObject(format.Id, obj, format.TypeObjectReader);
        }

        /// <summary>
        /// Set a clipboard format to the current clipboard. This will clear the clipboard
        /// if this is the first call to "Set" since the clipboard handle was opened.
        /// </summary>
        public virtual void SetFormat(ClipboardFormat format, byte[] bytes)
        {
            SetFormatObject(format.Id, bytes, new BytesDataConverter());
        }

        /// <summary>
        /// Set clipboard format to the current clipboard. This will clear the clipboard
        /// if this is the first call to "Set" since the clipboard handle was opened.
        /// </summary>
        public virtual void SetFormat(ClipboardFormat format, Stream stream)
        {
            var bytes = StructUtil.ReadBytes(stream);
            SetFormatObject(format.Id, bytes, new BytesDataConverter());
        }

        /// <summary>
        /// Tries to retrieve data from the clipboard (see <see cref="GetFormatObject{T}(uint, IDataConverter{T})"/>)
        /// but will not throw an exception if this was not possible.
        /// </summary>
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

        /// <summary>
        /// Retrieves data at the specified clipboard format Id, and converts it into a managed object.
        /// </summary>
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

        /// <summary>
        /// Writes an object to a specific clipboard format. 
        /// </summary>
        /// <typeparam name="T">Type of object to store on the clipboard.</typeparam>
        /// <param name="cfFormat">The Id of the format to write.</param>
        /// <param name="obj">The object to write to the clipboard.</param>
        /// <param name="converter">The coverter responsible for writing the object to an HGlobal.</param>
        /// <exception cref="Exception">If the object was not written successfully.</exception>
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

        /// <summary>
        /// Closes the currently open clipboard handle. 
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            NativeMethods.CloseClipboard();
        }

        /// <summary>
        /// Throws exception if this class is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (!_isOpen)
                throw new InvalidOperationException("The clipboard is not yet open, please call Open() or OpenAsync() first.");

            if (_disposed)
                throw new ObjectDisposedException(nameof(ClipboardHandle));
        }
    }
}
