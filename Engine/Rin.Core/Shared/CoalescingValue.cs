using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rin.Core.Shared;

/// <summary>
///     A value that can be published any number of times from any thread, where a single consumer
///     (e.g. a render loop) picks up the latest one at its own pace instead of reacting
///     synchronously to every write. Useful for settings a background thread may change repeatedly
///     before the owner is at a safe point to apply the latest one.
/// </summary>
/// <remarks>
///     Lock-free and allocation-free: <typeparamref name="T" /> is bit-reinterpreted into a
///     <see cref="ulong" />, so publishing and consuming are plain <see cref="Interlocked" />/
///     <see cref="Volatile" /> operations on that one machine word. Only unmanaged types up to 8 bytes
///     are supported - there is no safe way to atomically swap anything wider without a lock, so this
///     is checked with <see cref="Debug.Assert" /> in the constructor rather than at compile time.
/// </remarks>
public sealed class CoalescingValue<T> where T : unmanaged
{
    private ulong _current;
    private ulong _pending;

    public CoalescingValue(T initial)
    {
        Debug.Assert(Unsafe.SizeOf<T>() <= sizeof(ulong),
            $"CoalescingValue<{typeof(T)}> only supports types up to 8 bytes, got {Unsafe.SizeOf<T>()}.");

        _current = ToBits(initial);
        _pending = _current;
    }

    private static ulong ToBits(T value)
    {
        ulong bits = 0;
        Unsafe.As<ulong, T>(ref bits) = value;
        return bits;
    }

    private static T FromBits(ulong bits)
    {
        return Unsafe.As<ulong, T>(ref bits);
    }

    /// <summary>
    ///     The value as of the last <see cref="TryConsume" />, or the initial value.
    /// </summary>
    public T Current => FromBits(Volatile.Read(ref _current));

    /// <summary>
    ///     The latest published value, consumed or not.
    /// </summary>
    public T Pending => FromBits(Volatile.Read(ref _pending));

    /// <summary>
    ///     Lets a caller check whether <see cref="TryConsume" /> would do anything, without consuming.
    /// </summary>
    public bool HasPendingChange => Volatile.Read(ref _pending) != Volatile.Read(ref _current);

    /// <summary>
    ///     Publishes a value. Thread-safe; only the latest value as of a <see cref="TryConsume" /> survives.
    /// </summary>
    public void Set(T value)
    {
        Interlocked.Exchange(ref _pending, ToBits(value));
    }

    /// <summary>
    ///     Applies the latest pending value and returns true, or returns false if it already equals
    ///     <see cref="Current" />. Single-consumer only.
    /// </summary>
    public bool TryConsume(out T value)
    {
        var pendingBits = Volatile.Read(ref _pending);
        var previousBits = Interlocked.Exchange(ref _current, pendingBits);
        value = FromBits(pendingBits);
        return previousBits != pendingBits;
    }
}
