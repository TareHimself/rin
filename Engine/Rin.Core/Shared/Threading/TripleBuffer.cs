namespace Rin.Core.Shared.Threading;

/// <summary>
///     Lock-free single-producer/single-consumer handoff of the latest value written. <see cref="TryRead" />
///     always returns the most recently completed <see cref="Write" />, never blocks the writer, and
///     <see cref="Write" /> reports a value back if it was overwritten before ever being read, so the
///     caller can release it.
/// </summary>
public sealed class TripleBuffer<T> where T : class
{
    private readonly T?[] _slots = new T?[3];
    private int _readIndex = 2;
    private int _readyState;
    private int _writeIndex = 1;

    /// <summary>
    ///     Snapshot of whatever the three internal slots currently hold. Only meaningful once both the
    ///     writer and reader have stopped calling <see cref="Write" />/<see cref="TryRead" />, e.g. during
    ///     shutdown cleanup.
    /// </summary>
    public IReadOnlyList<T?> RemainingValues => _slots;

    /// <summary>
    ///     Called only from the single writer thread. Returns the previous value if it was never read, so
    ///     the caller can release it; otherwise returns null.
    /// </summary>
    public T? Write(T value)
    {
        _slots[_writeIndex] = value;

        var oldState = Interlocked.Exchange(ref _readyState, (_writeIndex << 1) | 1);
        var wasUnread = (oldState & 1) != 0;
        _writeIndex = oldState >> 1;

        return wasUnread ? _slots[_writeIndex] : null;
    }

    /// <summary>
    ///     Called only from the single reader thread. Returns false when nothing new has been written since
    ///     the last call; <paramref name="value" /> is then the same value returned last time.
    /// </summary>
    public bool TryRead(out T? value)
    {
        if ((Volatile.Read(ref _readyState) & 1) == 0)
        {
            value = _slots[_readIndex];
            return false;
        }

        var oldState = Interlocked.Exchange(ref _readyState, _readIndex << 1);
        _readIndex = oldState >> 1;
        value = _slots[_readIndex];
        return true;
    }
}
