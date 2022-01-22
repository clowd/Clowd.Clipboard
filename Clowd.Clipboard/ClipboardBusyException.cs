namespace Clowd.Clipboard
{
    public class ClipboardBusyException : Exception
    {
        public int ProcessId { get; }
        public string ProcessName { get; }

        public ClipboardBusyException() : base("Failed to open clipboard. Try again later.")
        {

        }

        public ClipboardBusyException(Exception inner) : base("Failed to open clipboard. Try again later.", inner)
        {

        }

        public ClipboardBusyException(int processId, string processName) : base($"Failed to open clipboard. It is currently locked by '{processName}' (pid.{processId}).")
        {
            ProcessId = processId;
            ProcessName = processName;
        }

        public ClipboardBusyException(int processId, string processName, Exception inner) : base($"Failed to open clipboard. It is currently locked by '{processName}' (pid.{processId}).", inner)
        {
            ProcessId = processId;
            ProcessName = processName;
        }
    }
}
