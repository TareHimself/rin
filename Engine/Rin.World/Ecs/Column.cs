using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     One component's values in an <see cref="Archetype" />: raw bytes, <see cref="Stride" /> per row.
/// </summary>
internal sealed class Column(int stride)
{
    public byte[] Data { get; private set; } = [];

    public int Stride { get; } = stride;

    public Span<byte> Bytes(int start, int count) => Data.AsSpan(start * Stride, count * Stride);

    public Span<T> Span<T>(int count) where T : unmanaged => MemoryMarshal.Cast<byte, T>(Data.AsSpan(0, count * Stride));

    public ref T Get<T>(int row) where T : unmanaged => ref Unsafe.As<byte, T>(ref Data[row * Stride]);

    public void Resize(int capacity)
    {
        var data = Data;
        Array.Resize(ref data, capacity * Stride);
        Data = data;
    }

    /// <summary>
    ///     Copies a row into the same component's column of another archetype.
    /// </summary>
    public void CopyRowTo(int row, Column target, int targetRow) =>
        Unsafe.CopyBlockUnaligned(ref target.Data[targetRow * Stride], ref Data[row * Stride], (uint)Stride);

    public void MoveRow(int from, int to) => Unsafe.CopyBlockUnaligned(ref Data[to * Stride], ref Data[from * Stride], (uint)Stride);

    public void ClearRows(int start, int count) => Bytes(start, count).Clear();
}
