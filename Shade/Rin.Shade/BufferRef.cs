using System;

namespace Rin.Shade;

/// <summary>
/// The C# face of a Slang <c>T*</c>. Only has meaning inside a shader body - the transpiler lowers
/// indexer and Value access directly to pointer arithmetic, these members are never actually
/// invoked on the CPU. The indexer and Value return by ref because that is what a pointer element
/// is: a variable, so a write through a struct element (buffer[i].Field = x) is legal C# and lowers
/// to an in-place store.
/// </summary>
public readonly struct BufferRef<T>(ulong address) where T : unmanaged
{
    public ulong Address { get; } = address;

    public ref T this[int index] =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");

    public ref T this[uint index] =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");

    public ref T Value =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)}.{nameof(Value)} has no CPU-side implementation");
}
