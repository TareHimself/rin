namespace Rin.World.Ecs;

/// <summary>
///     One archetype's rows as a query hands them out, before a typed chunk slices its columns.
/// </summary>
internal readonly struct ChunkView(Archetype archetype, ChunkEntities entities, ChunkChangeTicks ticks)
{
    public ChunkEntities Entities { get; } = entities;

    public ChunkChangeTicks Ticks { get; } = ticks;

    public int Length => Entities.Length;

    public ChunkColumn<T> Column<T>(ComponentId id) where T : unmanaged => new(archetype.ColumnFor(id), Length);
}
