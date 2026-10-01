using System;

namespace Rin.Shade;

/// <summary>
/// The C# face of a Slang <c>T*</c>. Only meaningful inside a shader body: the transpiler lowers
/// indexer and <c>Value</c> access to pointer arithmetic, so these members never run on the CPU.
/// </summary>
/// <remarks>
/// The indexers and <c>Value</c> return by ref so that writes such as <c>buffer[i].Field = x</c>
/// are legal C# and lower to an in-place store.
/// </remarks>
public readonly struct BufferRef<T>(ulong address) where T : unmanaged
{
    /// <summary>
    /// The device address of the first element.
    /// </summary>
    public ulong Address { get; } = address;

    public ref T this[int index] =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");

    public ref T this[uint index] =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)} indexer has no CPU-side implementation");

    public ref T Value =>
        throw new NotSupportedException($"{nameof(BufferRef<T>)}.{nameof(Value)} has no CPU-side implementation");
}
