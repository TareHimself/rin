using System.Buffers;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Rin.Core.Shared;

public struct PooledMemory<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    private readonly T[] _rentedBuffer;
    [PublicAPI]
    public int Count { get; private set; }
    public PooledMemory(int count) : this(count, ArrayPool<T>.Shared)
    {
    }
    
    public PooledMemory(int count,ArrayPool<T> pool)
    {
        _pool = pool;
        _rentedBuffer = _pool.Rent(count);
        Count = count;
    }

    public PooledMemory<T> Clear()
    {
        _rentedBuffer.AsSpan(0, Count).Clear();
        return this;
    }

    public void Dispose()
    {
        // References are cleared so the pool does not keep them alive.
        _pool.Return(_rentedBuffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    /// <summary>
    ///     Shortens the visible length; the rented buffer is unchanged.
    /// </summary>
    public void Truncate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);
        Count = count;
    }

    public ref T this[int index] => ref _rentedBuffer[index];

    public Span<T> AsSpan() => _rentedBuffer.AsSpan(0, Count);

    public Memory<T> AsMemory() => _rentedBuffer.AsMemory(0, Count);

    /// <summary>
    ///     Rents a buffer and copies <paramref name="source" /> into it.
    /// </summary>
    public static PooledMemory<T> CopyFrom(ReadOnlySpan<T> source)
    {
        var result = new PooledMemory<T>(source.Length);
        source.CopyTo(result.AsSpan());
        return result;
    }
}