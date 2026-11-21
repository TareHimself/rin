using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     One component's values in an <see cref="Archetype" />, one row per entity.
/// </summary>
internal abstract class Column
{
    public abstract void Resize(int capacity);

    /// <summary>
    ///     Copies a row into the same component's column of another archetype.
    /// </summary>
    public abstract void CopyRowTo(int row, Column target, int targetRow);

    public abstract void MoveRow(int from, int to);

    public abstract void ClearRows(int start, int count);

    public Span<T> Span<T>(int count) where T : struct =>
        RuntimeHelpers.IsReferenceOrContainsReferences<T>()
            ? ((ManagedColumn<T>)this).Items.AsSpan(0, count)
            : MemoryMarshal.Cast<byte, T>(((ByteColumn)this).Data.AsSpan(0, count * Unsafe.SizeOf<T>()));

    public ref T Get<T>(int row) where T : struct
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return ref ((ManagedColumn<T>)this).Items[row];
        var data = ((ByteColumn)this).Data;
        return ref Unsafe.As<byte, T>(ref data[row * Unsafe.SizeOf<T>()]);
    }
}

/// <summary>
///     Raw bytes, <see cref="Stride" /> per row, for unmanaged components.
/// </summary>
internal sealed class ByteColumn(int stride) : Column
{
    public byte[] Data { get; private set; } = [];

    public int Stride { get; } = stride;

    public Span<byte> Bytes(int start, int count) => Data.AsSpan(start * Stride, count * Stride);

    public override void Resize(int capacity)
    {
        var data = Data;
        Array.Resize(ref data, capacity * Stride);
        Data = data;
    }

    public override void CopyRowTo(int row, Column target, int targetRow) =>
        Bytes(row, 1).CopyTo(((ByteColumn)target).Bytes(targetRow, 1));

    public override void MoveRow(int from, int to) => Bytes(from, 1).CopyTo(Bytes(to, 1));

    public override void ClearRows(int start, int count) => Bytes(start, count).Clear();
}

/// <summary>
///     A typed array for components that hold managed references, which the GC must see.
/// </summary>
internal sealed class ManagedColumn<T> : Column where T : struct
{
    public T[] Items { get; private set; } = [];

    public override void Resize(int capacity)
    {
        var items = Items;
        Array.Resize(ref items, capacity);
        Items = items;
    }

    public override void CopyRowTo(int row, Column target, int targetRow) =>
        ((ManagedColumn<T>)target).Items[targetRow] = Items[row];

    public override void MoveRow(int from, int to) => Items[to] = Items[from];

    public override void ClearRows(int start, int count) => Items.AsSpan(start, count).Clear();
}
