using System.Runtime.CompilerServices;

namespace Rin.World.Ecs;

/// <summary>
///     The entity store behind <see cref="World" />: one <see cref="Archetype" /> table per component set, in creation
///     order, and each entity's archetype and row.
/// </summary>
/// <remarks>
///     Adding or removing a component follows an edge to the next archetype, built on first use, copies the row there
///     and fills the gap with the source's last row. Every step depends only on the order of operations, so the same
///     operations give the same rows.
/// </remarks>
internal sealed partial class ArchetypeStore
{
    private readonly EntityIdAllocator _ids = new();
    private readonly List<Archetype> _archetypes = [];
    private readonly Dictionary<ComponentId[], Archetype> _byKey = new(ArchetypeKeyComparer.Instance);
    private readonly Dictionary<ComponentId[], Archetype>.AlternateLookup<ReadOnlySpan<ComponentId>> _byKeySpan;
    private EntityLocation[] _locations = new EntityLocation[64];

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

    public EntityIdAllocator Ids => _ids;

    public StructuralPhase Phase { get; } = new();

    public Entity Spawn()
    {
        Phase.EnsureCanSpawn();
        var entity = _ids.Allocate();
        EnsureLocation(entity.Id);
        _locations[entity.Id] = new EntityLocation(Root, Root.AddRow(entity.Id));
        EntityCount++;
        return entity;
    }

    public bool IsAlive(Entity entity) => _ids.IsAlive(entity);

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
        if (!_ids.IsAlive(entity) || !Schema.TryGetId<T>(out var id)) return ref Unsafe.NullRef<T>();
        return ref RefAt<T>(_locations[entity.Id], id);
    }

    /// <summary>
    ///     Adds the component, or overwrites it when present. Returns whether it was new.
    /// </summary>
    public bool Add<T>(Entity entity, in T component) where T : unmanaged
    {
        var location = Resolve(entity);
        if (Schema.TryGetId<T>(out var existing) && location.Archetype.Has(existing))
        {
            RefAt<T>(location, existing) = component;
            return false;
        }

        Phase.EnsureCanMoveRows();
        EnsureCanGainComponents(entity);
        var id = Schema.IdOf<T>();
        Move(entity.Id, WithComponent(location.Archetype, id));
        RefAt<T>(_locations[entity.Id], id) = component;
        FireAddHooks(entity, id);
        return true;
    }

    public void Set<T>(Entity entity, in T component) where T : unmanaged => Get<T>(entity) = component;

    public bool Remove<T>(Entity entity) where T : unmanaged
    {
        var location = Resolve(entity);
        if (!Schema.TryGetId<T>(out var id) || !location.Archetype.Has(id) || IsBeingRemoved(entity, id)) return false;
        Phase.EnsureCanMoveRows();
        FireRemoveHooks(entity, id);

        if (!IsAlive(entity)) return false;
        location = _locations[entity.Id];
        if (!location.Archetype.Has(id)) return false;
        Move(entity.Id, WithoutComponent(location.Archetype, id));
        return true;
    }

    public Archetype GetOrCreateArchetype(ReadOnlySpan<ComponentId> key)
    {
        if (_byKeySpan.TryGetValue(key, out var existing)) return existing;
        var archetype = new Archetype(key.ToArray(), Schema);
        _archetypes.Add(archetype);
        _byKey.Add(archetype.Key, archetype);
        return archetype;
    }

    private void EnsureLocation(int entityId)
    {
        if (entityId >= _locations.Length) Array.Resize(ref _locations, int.Max(entityId + 1, _locations.Length * 2));
    }

    private void Delete(int entityId)
    {
        var location = _locations[entityId];
        _hierarchy.Remove(entityId);
        RemoveRow(location.Archetype, location.Row);
        _ids.Free(entityId);
        EntityCount--;
    }

    private void Move(int entityId, Archetype target)
    {
        var source = _locations[entityId];
        var targetRow = target.AddRow(entityId);
        var columns = source.Archetype.Columns;
        var columnIds = source.Archetype.ColumnIds;
        for (var i = 0; i < columns.Length; i++)
        {
            var targetColumn = target.ColumnOf(columnIds[i]);
            if (targetColumn >= 0) columns[i].CopyRowTo(source.Row, target.Columns[targetColumn], targetRow);
        }

        RemoveRow(source.Archetype, source.Row);
        _locations[entityId] = new EntityLocation(target, targetRow);
    }

    private void RemoveRow(Archetype archetype, int row)
    {
        var moved = archetype.RemoveRow(row);
        if (moved != EntityIdAllocator.NoId) _locations[moved] = new EntityLocation(archetype, row);
    }

    private Archetype WithComponent(Archetype source, ComponentId id)
    {
        if (source.AddEdge(id) is { } cached) return cached;

        var key = source.Key;
        var insertAt = ~Array.BinarySearch(key, id);
        ComponentId[] targetKey = [.. key.AsSpan(0, insertAt), id, .. key.AsSpan(insertAt)];
        var target = GetOrCreateArchetype(targetKey);
        source.SetAddEdge(id, target);
        target.SetRemoveEdge(id, source);
        return target;
    }

    private Archetype WithoutComponent(Archetype source, ComponentId id)
    {
        if (source.RemoveEdge(id) is { } cached) return cached;

        var key = source.Key;
        var removeAt = Array.BinarySearch(key, id);
        ComponentId[] targetKey = [.. key.AsSpan(0, removeAt), .. key.AsSpan(removeAt + 1)];
        var target = GetOrCreateArchetype(targetKey);
        source.SetRemoveEdge(id, target);
        target.SetAddEdge(id, source);
        return target;
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

    private EntityLocation Resolve(Entity entity)
    {
        if (!_ids.IsAlive(entity)) throw new InvalidOperationException($"{entity} is not alive.");
        return _locations[entity.Id];
    }

    private static InvalidOperationException MissingComponent<T>(Entity entity) =>
        new($"{entity} has no {typeof(T).Name}.");
}

/// <summary>
///     Where an entity's components are.
/// </summary>
internal readonly record struct EntityLocation(Archetype Archetype, int Row);

/// <summary>
///     What a tag's reference points at, since a tag has no column. Tags have no fields, so sharing it loses nothing.
/// </summary>
internal static class TagValue<T> where T : unmanaged
{
    public static T Shared;
}
