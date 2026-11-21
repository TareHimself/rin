using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     One component's values in an <see cref="Archetype" />: raw bytes, <see cref="Stride" /> per row, plus the
///     <see cref="ChangeClock" /> tick each row was last written at.
/// </summary>
internal sealed class Column(int stride)
{
    private uint[] _changeTicks = [];

    public byte[] Data { get; private set; } = [];

    public int Stride { get; } = stride;

    /// <summary>
    ///     At least the newest tick of any row, so a change query can skip the column. Removing rows never lowers it.
    /// </summary>
    public uint LastChangeTick { get; private set; }

    public Span<byte> Bytes(int start, int count) => Data.AsSpan(start * Stride, count * Stride);

    public Span<T> Span<T>(int count) where T : unmanaged => MemoryMarshal.Cast<byte, T>(Data.AsSpan(0, count * Stride));

    public ReadOnlySpan<uint> ChangeTicks(int count) => _changeTicks.AsSpan(0, count);

    public ref T Get<T>(int row) where T : unmanaged => ref Unsafe.As<byte, T>(ref Data[row * Stride]);

    public uint ChangeTickAt(int row) => _changeTicks[row];

    /// <summary>
    ///     Records that <paramref name="row" /> was written at <paramref name="tick" />, the clock's current tick.
    /// </summary>
    public void Stamp(int row, uint tick)
    {
        _changeTicks[row] = tick;
        LastChangeTick = tick;
    }

    public void StampRows(int start, int count, uint tick)
    {
        _changeTicks.AsSpan(start, count).Fill(tick);
        LastChangeTick = tick;
    }

    public void Resize(int capacity)
    {
        var data = Data;
        Array.Resize(ref data, capacity * Stride);
        Data = data;
        Array.Resize(ref _changeTicks, capacity);
    }

    /// <summary>
    ///     Copies a row and its change tick into the same component's column of another archetype.
    /// </summary>
    public void CopyRowTo(int row, Column target, int targetRow)
    {
        Unsafe.CopyBlockUnaligned(ref target.Data[targetRow * Stride], ref Data[row * Stride], (uint)Stride);
        var tick = _changeTicks[row];
        target._changeTicks[targetRow] = tick;
        target.LastChangeTick = uint.Max(target.LastChangeTick, tick);
    }

    public void MoveRow(int from, int to)
    {
        Unsafe.CopyBlockUnaligned(ref Data[to * Stride], ref Data[from * Stride], (uint)Stride);
        _changeTicks[to] = _changeTicks[from];
    }

    public void ClearRows(int start, int count) => Bytes(start, count).Clear();
}
