#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Runtime.InteropServices;

namespace Clowd.Clipboard.Bitmaps;

public unsafe class PointerStream : Stream
{
    private readonly byte* _bufferStart;
    private readonly long _bufferLen;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _bufferLen;

    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    public PointerStream(byte* buffer, long bufferLen)
    {
        _bufferStart = buffer;
        _bufferLen = bufferLen;
    }

    public override void Flush()
    {
        // nop
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        count = Math.Min(count, (int)(_bufferLen - _position));
        Marshal.Copy((IntPtr)(_bufferStart + _position), buffer, offset, count);
        _position += count;
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = offset;
                return _position;
            case SeekOrigin.Current:
                _position += offset;
                return _position;
            case SeekOrigin.End:
                _position = _bufferLen + offset;
                return _position;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
