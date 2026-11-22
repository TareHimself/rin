namespace Rin.World.Ecs;

/// <summary>
///     One queried component's rows in a chunk, with the column that holds their change ticks.
/// </summary>
internal readonly ref struct ChunkColumn<T>
    where T : unmanaged
{
    private readonly Column _column;

    public ChunkColumn(Column column, int length)
    {
        _column = column;
        Components = column.Span<T>(length);
    }

    public Span<T> Components { get; }

    public ref T Write(int index, ChunkChangeTicks ticks) => ref ticks.Stamp(Components, _column, index);

    public Span<T> WriteAll(ChunkChangeTicks ticks) => ticks.StampAll(Components, _column);

    public bool HasChanged(int index, ChunkChangeTicks ticks) => ticks.IsNewer(_column, index);
}
