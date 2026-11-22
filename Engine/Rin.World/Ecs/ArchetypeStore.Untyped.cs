namespace Rin.World.Ecs;

/// <summary>
///     Component access by <see cref="ComponentId" /> and raw bytes, for tools that only know a component from its
///     <see cref="ComponentInfo" />, like an inspector or a scene loader.
/// </summary>
internal sealed partial class ArchetypeStore
{
    public bool Has(Entity entity, ComponentId id) => Resolve(entity).Archetype.Has(id);

    /// <summary>
    ///     The ids of every component and tag on the entity, in id order. Valid until its next structural change.
    /// </summary>
    public ReadOnlySpan<ComponentId> ComponentsOf(Entity entity) => Resolve(entity).Archetype.Key;

    /// <summary>
    ///     The component's bytes, empty for a tag.
    /// </summary>
    public ReadOnlySpan<byte> Read(Entity entity, ComponentId id) => BytesAt(ResolveWith(entity, id), id);

    /// <summary>
    ///     The component's bytes, with its row stamped as changed at the clock's current tick.
    /// </summary>
    public Span<byte> Write(Entity entity, ComponentId id)
    {
        var location = ResolveWith(entity, id);
        StampAt(location, id);
        return BytesAt(location, id);
    }

    /// <summary>
    ///     Adds the component with <paramref name="value" /> as its bytes, or overwrites it when present. Returns whether
    ///     it was new.
    /// </summary>
    public bool Add(Entity entity, ComponentId id, ReadOnlySpan<byte> value)
    {
        var size = Schema[id].Size;
        if (value.Length != size)
            throw new ArgumentException($"{Schema[id].Name} is {size} bytes, but the value is {value.Length}.", nameof(value));

        var location = Resolve(entity);
        var isNew = !location.Archetype.Has(id);
        if (isNew) location = MoveToAdd(entity, location, id);
        StampAt(location, id);
        value.CopyTo(BytesAt(location, id));
        if (isNew) FireAddHooks(entity, id);
        return isNew;
    }

    public bool Remove(Entity entity, ComponentId id)
    {
        var location = Resolve(entity);
        if (!location.Archetype.Has(id) || IsBeingRemoved(entity, id)) return false;
        Phase.EnsureCanMoveRows();
        if (HooksOf(id) is not null)
        {
            FireRemoveHooks(entity, id);
            // A hook may have moved the entity, removed the component or despawned it.
            if (!_entities.TryLocate(entity, out location) || !location.Archetype.Has(id)) return false;
        }

        Move(entity.Id, location.Row, WithoutComponent(location.Archetype, id));
        return true;
    }

    private EntityLocation MoveToAdd(Entity entity, EntityLocation location, ComponentId id)
    {
        EnsureCanAdd(entity);
        return Move(entity.Id, location.Row, WithComponent(location.Archetype, id));
    }

    private void EnsureCanAdd(Entity entity)
    {
        Phase.EnsureCanMoveRows();
        EnsureCanGainComponents(entity);
    }

    private EntityLocation ResolveWith(Entity entity, ComponentId id)
    {
        var location = Resolve(entity);
        if (!location.Archetype.Has(id)) throw new InvalidOperationException($"{entity} has no {Schema[id].Name}.");
        return location;
    }

    private static Span<byte> BytesAt(EntityLocation location, ComponentId id)
    {
        var column = location.Archetype.ColumnOf(id);
        return column >= 0 ? location.Archetype.Columns[column].Bytes(location.Row, 1) : [];
    }

    private void StampAt(EntityLocation location, ComponentId id)
    {
        var column = location.Archetype.ColumnOf(id);
        if (column >= 0) location.Archetype.Columns[column].Stamp(location.Row, Clock.Current);
    }
}
