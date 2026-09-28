using System;

namespace Rin.Shade;

/// <summary>
/// The C# face of a Slang <c>T*</c>. Only has meaning inside a shader body - the transpiler lowers
/// indexer and Value access directly to pointer arithmetic, these members are never actually
/// invoked on the CPU.
/// </summary>
public readonly struct BufferRef<T>(ulong address) where T : unmanaged
{
    public ulong Address { get; } = address;

    public T this[int index]
    {
        get => throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");
        set => throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");
    }
    
    public T this[uint index]
    {
        get => throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");
        set => throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");
    }

    public T Value => throw new NotSupportedException($"{nameof(BufferRef<T>)}.{nameof(Value)} has no CPU-side implementation");
}
