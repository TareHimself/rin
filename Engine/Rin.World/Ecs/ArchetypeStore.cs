using System.Runtime.CompilerServices;

namespace Rin.World.Ecs;

/// <summary>
///     The entity store behind <see cref="World" />: one <see cref="Archetype" /> table per component set, in creation
///     order, and the <see cref="EntityTable" /> that says where each entity is.
/// </summary>
/// <remarks>
///     Adding or removing a component follows an <see cref="ArchetypeEdge" /> to the next archetype, built on first use,
///     copies the row there with the edge's column pairs and fills the gap with the source's last row. Every step depends
///     only on the order of operations, so the same operations give the same rows.
/// </remarks>
internal sealed partial class ArchetypeStore
{
    private readonly EntityTable _entities = new();
    private readonly List<Archetype> _archetypes = [];
    private readonly Dictionary<ComponentId[], Archetype> _byKey = new(ArchetypeKeyComparer.Instance);
    private readonly Dictionary<ComponentId[], Archetype>.AlternateLookup<ReadOnlySpan<ComponentId>> _byKeySpan;

    public ArchetypeStore(ComponentSchema schema)
    {
        Schema = schema;
        _byKeySpan = _byKey.GetAlternateLookup<ReadOnlySpan<ComponentId>>();
        Root = GetOrCreateArchetype([]);
    }

    public ComponentSchema Schema { get; }

    /// <summary>
    ///     Every archetype ever created, in creation order. Empty ones stay.
    /// </summary>
    public List<Archetype> Archetypes => _archetypes;

    /// <summary>
    ///     The archetype without components, where spawned entities start.
    /// </summary>
    public Archetype Root { get; }

    public int EntityCount { get; private set; }

    public EntityTable Entities => _entities;

    public StructuralPhase Phase { get; } = new();

    public ChangeClock Clock { get; } = new();

    public Entity Spawn()
    {
        Phase.EnsureCanSpawn();
        var entity = _entities.Allocate(Root);
        EntityCount++;
        return entity;
    }

    public bool IsAlive(Entity entity) => _entities.IsAlive(entity);

    public bool Has<T>(Entity entity) where T : unmanaged
    {
        var location = Resolve(entity);
        return Schema.TryGetId<T>(out var id) && location.Archetype.Has(id);
    }

    public ref T Get<T>(Entity entity) where T : unmanaged
    {
        Resolve(entity);
        ref var component = ref TryGetRef<T>(entity);
        if (Unsafe.IsNullRef(ref component)) throw MissingComponent<T>(entity);
        return ref component;
    }

    /// <summary>
    ///     The component, or a null reference when the entity is not alive or lacks it.
    /// </summary>
    public ref T TryGetRef<T>(Entity entity) where T : unmanaged
    {
        if (!_entities.TryLocate(entity, out var location) || !Schema.TryGetId<T>(out var id)) return ref Unsafe.NullRef<T>();
        return ref RefAt<T>(location, id);
    }

    /// <summary>
    ///     Adds the component, or overwrites it when present. Returns whether it was new.
    /// </summary>
    public bool Add<T>(Entity entity, in T component) where T : unmanaged
    {
        var location = Resolve(entity);
        var hasId = Schema.TryGetId<T>(out var existing);
        if (hasId && location.Archetype.Has(existing))
        {
            WriteAt<T>(location, existing) = component;
            return false;
        }

        EnsureCanAdd(entity);
        // Only after the guards, so a rejected add leaves the schema unchanged.
        var id = hasId ? existing : Schema.IdOf<T>();
        var moved = Move(entity.Id, location.Row, WithComponent(location.Archetype, id));
        WriteAt<T>(moved, id) = component;
        FireAddHooks(entity, id);
        return true;
    }

    public void Set<T>(Entity entity, in T component) where T : unmanaged => Write<T>(entity) = component;

    /// <summary>
    ///     The component, with its row stamped as changed at the clock's current tick.
    /// </summary>
    public ref T Write<T>(Entity entity) where T : unmanaged
    {
        var location = Resolve(entity);
        if (!Schema.TryGetId<T>(out var id) || !location.Archetype.Has(id)) throw MissingComponent<T>(entity);
        return ref WriteAt<T>(location, id);
    }

    /// <summary>
    ///     Whether the component was written after <paramref name="tick" />. Throws for a tag, which has no change ticks.
    /// </summary>
    public bool HasChangedSince<T>(Entity entity, uint tick) where T : unmanaged
    {
        var location = Resolve(entity);
        if (!Schema.TryGetId<T>(out var id) || !location.Archetype.Has(id)) throw MissingComponent<T>(entity);
        var column = location.Archetype.ColumnOf(id);
        if (column < 0) throw new InvalidOperationException($"{typeof(T).Name} is a tag, so it has no change ticks.");
        return location.Archetype.Columns[column].ChangeTickAt(location.Row) > tick;
    }

    /// <summary>
    ///     Whether any of <paramref name="ids" /> that the entity has a column for was written after
    ///     <paramref name="tick" />, with one location lookup for all of them.
    /// </summary>
    public bool HasAnyChangedSince(Entity entity, ReadOnlySpan<ComponentId> ids, uint tick)
    {
        var location = Resolve(entity);
        foreach (var id in ids)
        {
            var column = location.Archetype.ColumnOf(id);
            if (column >= 0 && location.Archetype.Columns[column].ChangeTickAt(location.Row) > tick) return true;
        }

        return false;
    }

    public bool Remove<T>(Entity entity) where T : unmanaged
    {
        Resolve(entity);
        return Schema.TryGetId<T>(out var id) && Remove(entity, id);
    }

    public Archetype GetOrCreateArchetype(ReadOnlySpan<ComponentId> key)
    {
        if (_byKeySpan.TryGetValue(key, out var existing)) return existing;
        var archetype = new Archetype(key.ToArray(), Schema);
        _archetypes.Add(archetype);
        _byKey.Add(archetype.Key, archetype);
        return archetype;
    }

    private void Delete(int entityId)
    {
        var location = _entities.LocationOf(entityId);
        _hierarchy.Remove(entityId);
        RemoveRow(location.Archetype, location.Row);
        _entities.Free(entityId);
        EntityCount--;
    }

    private EntityLocation Move(int entityId, int sourceRow, ArchetypeEdge edge)
    {
        var target = new EntityLocation(edge.Target, edge.Target.AddRow(entityId));
        edge.CopyRow(sourceRow, target.Row);
        RemoveRow(edge.Source, sourceRow);
        _entities.SetLocation(entityId, target);
        return target;
    }

    private void RemoveRow(Archetype archetype, int row)
    {
        var moved = archetype.RemoveRow(row);
        if (moved != EntityTable.NoId) _entities.SetLocation(moved, new EntityLocation(archetype, row));
    }

    private ArchetypeEdge WithComponent(Archetype source, ComponentId id)
    {
        if (source.AddEdge(id) is { } cached) return cached;

        var key = source.Key;
        var insertAt = ~Array.BinarySearch(key, id);
        ComponentId[] targetKey = [.. key.AsSpan(0, insertAt), id, .. key.AsSpan(insertAt)];
        var target = GetOrCreateArchetype(targetKey);
        target.SetRemoveEdge(id, source);
        return source.SetAddEdge(id, target);
    }

    private ArchetypeEdge WithoutComponent(Archetype source, ComponentId id)
    {
        if (source.RemoveEdge(id) is { } cached) return cached;

        var key = source.Key;
        var removeAt = Array.BinarySearch(key, id);
        ComponentId[] targetKey = [.. key.AsSpan(0, removeAt), .. key.AsSpan(removeAt + 1)];
        var target = GetOrCreateArchetype(targetKey);
        target.SetAddEdge(id, source);
        return source.SetRemoveEdge(id, target);
    }

    /// <summary>
    ///     The component in its column, the shared <see cref="TagValue{T}" /> for a tag, or a null reference when the
    ///     archetype lacks it.
    /// </summary>
    private static ref T RefAt<T>(EntityLocation location, ComponentId id) where T : unmanaged
    {
        var archetype = location.Archetype;
        var column = archetype.ColumnOf(id);
        if (column >= 0) return ref archetype.Columns[column].Get<T>(location.Row);
        return ref archetype.Has(id) ? ref TagValue<T>.Shared : ref Unsafe.NullRef<T>();
    }

    private ref T WriteAt<T>(EntityLocation location, ComponentId id) where T : unmanaged
    {
        var column = location.Archetype.ColumnOf(id);
        if (column >= 0) location.Archetype.Columns[column].Stamp(location.Row, Clock.Current);
        return ref RefAt<T>(location, id);
    }

    private EntityLocation Resolve(Entity entity)
    {
        if (!_entities.TryLocate(entity, out var location)) throw new InvalidOperationException($"{entity} is not alive.");
        return location;
    }

    private static InvalidOperationException MissingComponent<T>(Entity entity) =>
        new($"{entity} has no {typeof(T).Name}.");
}

/// <summary>
///     What a tag's reference points at, since a tag has no column. Tags have no fields, so sharing it loses nothing.
/// </summary>
internal static class TagValue<T> where T : unmanaged
{
    public static T Shared;
}
