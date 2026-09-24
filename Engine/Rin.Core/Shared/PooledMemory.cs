using System.Buffers;
using JetBrains.Annotations;

namespace Rin.Core.Shared;

public struct PooledMemory<T> : IDisposable where T : unmanaged
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
        _pool.Return(_rentedBuffer);
    }

    public ref T this[int index] => ref _rentedBuffer[index];

    public Span<T> AsSpan() => _rentedBuffer.AsSpan(0, Count);

    public Memory<T> AsMemory() => _rentedBuffer.AsMemory(0, Count);

    /// <summary>Rents a buffer and copies <paramref name="source" /> into it.</summary>
    public static PooledMemory<T> CopyFrom(ReadOnlySpan<T> source)
    {
        var result = new PooledMemory<T>(source.Length);
        source.CopyTo(result.AsSpan());
        return result;
    }
}