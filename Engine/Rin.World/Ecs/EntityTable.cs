using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     One slot per entity id with its generation, lifecycle state and archetype row, plus a LIFO free list. Ids depend
///     only on the order of spawns and despawns, so the whole table can be saved and restored.
/// </summary>
/// <remarks>
///     The location lives in the same slot as the state, so a live id always has a row and a free one never does.
/// </remarks>
internal sealed class EntityTable
{
    /// <summary>
    ///     Never handed out, so it means "no entity" wherever an entity id is stored.
    /// </summary>
    public const int NoId = 0;

    private const int FirstId = NoId + 1;

    private Slot[] _slots = new Slot[64];
    private int[] _free = new int[64];
    private int _freeCount;
    private int _nextId = FirstId;

    /// <summary>
    ///     Takes the next id and gives it a row in <paramref name="archetype" />.
    /// </summary>
    public Entity Allocate(Archetype archetype)
    {
        var id = _freeCount > 0 ? _free[--_freeCount] : _nextId++;
        EnsureSlot(id);
        ref var slot = ref _slots[id];
        slot.State = EntityState.Alive;
        slot.Location = new EntityLocation(archetype, archetype.AddRow(id));
        return new Entity(id, slot.Generation);
    }

    /// <summary>
    ///     Frees an id whose row is already gone.
    /// </summary>
    public void Free(int id)
    {
        ref var slot = ref _slots[id];
        slot.State = EntityState.Free;
        slot.Location = default;
        slot.Generation = unchecked((short)(slot.Generation + 1));
        if (_freeCount == _free.Length) Array.Resize(ref _free, _free.Length * 2);
        _free[_freeCount++] = id;
    }

    /// <summary>
    ///     <see cref="EntityState.Free" /> for a stale handle.
    /// </summary>
    public EntityState StateOf(Entity entity) => IsCurrent(entity) ? _slots[entity.Id].State : EntityState.Free;

    public bool IsAlive(Entity entity) => StateOf(entity) != EntityState.Free;

    public bool TryLocate(Entity entity, out EntityLocation location)
    {
        var isAlive = IsAlive(entity);
        location = isAlive ? _slots[entity.Id].Location : default;
        return isAlive;
    }

    public EntityLocation LocationOf(int id) => _slots[id].Location;

    public void SetLocation(int id, EntityLocation location) => _slots[id].Location = location;

    public void MarkDespawning(int id) => _slots[id].State = EntityState.Despawning;

    /// <summary>
    ///     Makes a <see cref="EntityState.Despawning" /> entity plain alive again, for a despawn that threw.
    /// </summary>
    public void CancelDespawning(Entity entity)
    {
        if (StateOf(entity) == EntityState.Despawning) _slots[entity.Id].State = EntityState.Alive;
    }

    public Entity HandleOf(int id) => new(id, _slots[id].Generation);

    /// <summary>
    ///     Writes the next id, the free list, then every slot's generation and every slot's state. Locations are not
    ///     saved: the archetypes put them back.
    /// </summary>
    public void Save(WorldSnapshot snapshot)
    {
        snapshot.Write(_nextId);
        snapshot.Write(_freeCount);
        snapshot.WriteSpan<int>(_free.AsSpan(0, _freeCount));

        var generations = snapshot.Reserve<short>(_nextId);
        var states = snapshot.Reserve<EntityState>(_nextId);
        for (var id = 0; id < _nextId; id++)
        {
            generations[id] = _slots[id].Generation;
            states[id] = _slots[id].State;
        }
    }

    /// <summary>
    ///     Reads past what <see cref="Save" /> wrote.
    /// </summary>
    public static void Skip(ref SnapshotReader reader)
    {
        var nextId = reader.Read<int>();
        var freeCount = reader.Read<int>();
        reader.ReadBytes(freeCount * sizeof(int) + nextId * (sizeof(short) + sizeof(EntityState)));
    }

    /// <summary>
    ///     Restores every slot without a location, so each live id must get one from <see cref="SetLocation" /> next.
    /// </summary>
    public void Restore(ref SnapshotReader reader)
    {
        _nextId = reader.Read<int>();
        _freeCount = reader.Read<int>();
        if (_freeCount > _free.Length) Array.Resize(ref _free, int.Max(_freeCount, _free.Length * 2));
        EnsureSlot(_nextId - 1);
        reader.ReadSpan(_free.AsSpan(0, _freeCount));

        var generations = MemoryMarshal.Cast<byte, short>(reader.ReadBytes(_nextId * sizeof(short)));
        var states = MemoryMarshal.Cast<byte, EntityState>(reader.ReadBytes(_nextId * sizeof(EntityState)));
        for (var id = 0; id < _nextId; id++) _slots[id] = new Slot { Generation = generations[id], State = states[id] };
        _slots.AsSpan(_nextId).Clear();
    }

    private bool IsCurrent(Entity entity) =>
        entity.Id >= FirstId && entity.Id < _nextId && _slots[entity.Id].Generation == entity.Generation;

    private void EnsureSlot(int id)
    {
        if (id >= _slots.Length) Array.Resize(ref _slots, int.Max(id + 1, _slots.Length * 2));
    }

    private struct Slot
    {
        public EntityLocation Location;
        public short Generation;
        public EntityState State;
    }
}

/// <summary>
///     Where an entity's components are.
/// </summary>
internal readonly record struct EntityLocation(Archetype Archetype, int Row);
