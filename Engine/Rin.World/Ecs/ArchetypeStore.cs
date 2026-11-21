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

    public Entity Spawn()
    {
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
        var location = Resolve(entity);
        var column = Schema.TryGetId<T>(out var id) ? location.Archetype.ColumnOf(id) : -1;
        if (column < 0) throw MissingComponent<T>(entity);
        return ref location.Archetype.Columns[column].Get<T>(location.Row);
    }

    /// <summary>
    ///     Adds the component, or overwrites it when present. Returns whether it was new.
    /// </summary>
    public bool Add<T>(Entity entity, in T component) where T : unmanaged
    {
        var id = Schema.IdOf<T>();
        var location = Resolve(entity);
        var isNew = !location.Archetype.Has(id);
        if (isNew)
        {
            Move(entity.Id, WithComponent(location.Archetype, id));
            location = _locations[entity.Id];
        }

        location.Archetype.Columns[location.Archetype.ColumnOf(id)].Get<T>(location.Row) = component;
        if (isNew) HooksOf(id)?.FireAdded(this, entity);
        return isNew;
    }

    public void Set<T>(Entity entity, in T component) where T : unmanaged => Get<T>(entity) = component;

    public bool Remove<T>(Entity entity) where T : unmanaged
    {
        var location = Resolve(entity);
        if (!Schema.TryGetId<T>(out var id) || !location.Archetype.Has(id)) return false;
        EnsureNotIterating();
        HooksOf(id)?.FireRemoved(this, entity);

        if (!IsAlive(entity)) return false;
        location = _locations[entity.Id];
        if (!location.Archetype.Has(id)) return false;
        Move(entity.Id, WithoutComponent(location.Archetype, id));
        return true;
    }

    public bool HasTag<T>(Entity entity) where T : unmanaged => Has<T>(entity);

    public void AddTag<T>(Entity entity) where T : unmanaged
    {
        var id = Schema.IdOf<T>();
        var location = Resolve(entity);
        if (!location.Archetype.Has(id)) Move(entity.Id, WithComponent(location.Archetype, id));
    }

    public void RemoveTag<T>(Entity entity) where T : unmanaged => Remove<T>(entity);

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
        EnsureNotIterating();
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
        if (moved != 0) _locations[moved] = new EntityLocation(archetype, row);
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
