namespace Clowd.Clipboard;

/// <summary>
/// Thrown when the clipboard can not be opened for reading or writing because it is locked by another thread or application.
/// </summary>
public class ClipboardBusyException : Exception
{
    /// <summary>
    /// The Id of the process currently locking the clipboard.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// The Name of the process currently locking the clipboard
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// Create a new ClipboardBusyException
    /// </summary>
    public ClipboardBusyException() : base("Failed to open clipboard. Try again later.")
    {

    }

    /// <summary>
    /// Create a new ClipboardBusyException with an inner exception
    /// </summary>
    public ClipboardBusyException(Exception inner) : base("Failed to open clipboard. Try again later.", inner)
    {

    }

    /// <summary>
    /// Create a new ClipboardBusyException while also which process is currently locking the clipboard.
    /// </summary>
    public ClipboardBusyException(int processId, string processName) : base($"Failed to open clipboard. It is currently locked by '{processName}' (pid.{processId}).")
    {
        ProcessId = processId;
        ProcessName = processName;
    }

    /// <summary>
    /// Create a new ClipboardBusyException while also which process is currently locking the clipboard and providing an inner exception.
    /// </summary>
    public ClipboardBusyException(int processId, string processName, Exception inner) : base($"Failed to open clipboard. It is currently locked by '{processName}' (pid.{processId}).", inner)
    {
        ProcessId = processId;
        ProcessName = processName;
    }
}
