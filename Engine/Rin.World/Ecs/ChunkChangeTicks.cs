namespace Rin.World.Ecs;

/// <summary>
///     What a <see cref="QueryChunk{T1}" /> stamps its writes with and compares its rows against.
/// </summary>
/// <param name="WriteTick">
///     The clock's tick when the chunk was handed out.
/// </param>
/// <param name="Since">
///     The query's last run, from its <see cref="ChangeWindow" />.
/// </param>
internal readonly record struct ChunkChangeTicks(uint WriteTick, uint Since, bool TracksChanges)
{
    public ref T Stamp<T>(Span<T> components, Column column, int index) where T : unmanaged
    {
        ref var component = ref components[index];
        column.Stamp(index, WriteTick);
        return ref component;
    }

    public bool IsNewer(Column column, int index)
    {
        if (!TracksChanges)
            throw new InvalidOperationException("HasChanged needs a query built with Changed<T>(), which keeps its own last run.");
        return column.ChangeTickAt(index) > Since;
    }

    public static InvalidOperationException NotQueried<T>() => new($"{typeof(T).Name} is not one of the query's components.");
}
