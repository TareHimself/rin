namespace Rin.Core.Shared.Video;

/// <summary>
///     A source fed from outside, for a file still arriving or a live stream. Append bytes in file order from any
///     thread, and call <see cref="End" /> once no more will come. The player retries the decoder every frame, so
///     appended data is picked up without further calls.
/// </summary>
/// <param name="dropReleased">
///     Free the bytes the decoder is done with. Seeking back into them then needs them appended again.
/// </param>
public sealed class StreamVideoSource(bool dropReleased = false) : IVideoSource
{
    private readonly SparseByteBuffer _buffer = new();
    private readonly Lock _lock = new();
    private bool _ended;

    public long BufferedBytes
    {
        get
        {
            lock (_lock)
            {
                return _buffer.HeldBytes;
            }
        }
    }

    public long Length
    {
        get
        {
            lock (_lock)
            {
                return _ended ? _buffer.Available : -1;
            }
        }
    }

    public long Available
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Available;
            }
        }
    }

    public void Read(long offset, Span<byte> destination)
    {
        lock (_lock)
        {
            _buffer.Read(offset, destination);
        }
    }

    public long AvailableFrom(long offset)
    {
        lock (_lock)
        {
            return _buffer.AvailableFrom(offset);
        }
    }

    public void ReleaseBefore(long offset)
    {
        if (!dropReleased) return;
        lock (_lock)
        {
            _buffer.ReleaseBefore(offset);
        }
    }

    public void Dispose()
    {
    }

    public void Append(ReadOnlySpan<byte> data)
    {
        lock (_lock)
        {
            if (_ended) throw new InvalidOperationException("The stream has ended");
            _buffer.Write(_buffer.Available, data);
        }
    }

    public void End()
    {
        lock (_lock)
        {
            _ended = true;
        }
    }
}
