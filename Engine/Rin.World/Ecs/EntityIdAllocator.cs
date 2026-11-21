namespace Rin.World.Ecs;

/// <summary>
///     Hands out entity ids from a LIFO free list and bumps a slot's generation when it is freed, so ids depend only on
///     the order of spawns and despawns and the whole state can be saved and restored.
/// </summary>
internal sealed class EntityIdAllocator
{
    private const int FirstId = 1;

    private short[] _generations = new short[64];
    private EntityState[] _states = new EntityState[64];
    private int[] _free = new int[64];
    private int _freeCount;
    private int _nextId = FirstId;

    public Entity Allocate()
    {
        var id = _freeCount > 0 ? _free[--_freeCount] : _nextId++;
        EnsureSlot(id);
        _states[id] = EntityState.Alive;
        return new Entity(id, _generations[id]);
    }

    public void Free(int id)
    {
        _states[id] = EntityState.Free;
        _generations[id] = unchecked((short)(_generations[id] + 1));
        if (_freeCount == _free.Length) Array.Resize(ref _free, _free.Length * 2);
        _free[_freeCount++] = id;
    }

    /// <summary>
    ///     <see cref="EntityState.Free" /> for a stale handle.
    /// </summary>
    public EntityState StateOf(Entity entity) =>
        entity.Id >= FirstId && entity.Id < _nextId && _generations[entity.Id] == entity.Generation
            ? _states[entity.Id]
            : EntityState.Free;

    public bool IsAlive(Entity entity) => StateOf(entity) != EntityState.Free;

    public void MarkDespawning(int id) => _states[id] = EntityState.Despawning;

    /// <summary>
    ///     Makes a <see cref="EntityState.Despawning" /> entity plain alive again, for a despawn that threw.
    /// </summary>
    public void CancelDespawning(Entity entity)
    {
        if (StateOf(entity) == EntityState.Despawning) _states[entity.Id] = EntityState.Alive;
    }

    public Entity HandleOf(int id) => new(id, _generations[id]);

    public void Save(WorldSnapshot snapshot)
    {
        snapshot.Write(_nextId);
        snapshot.Write(_freeCount);
        snapshot.WriteSpan<int>(_free.AsSpan(0, _freeCount));
        snapshot.WriteSpan<short>(_generations.AsSpan(0, _nextId));
        snapshot.WriteSpan<EntityState>(_states.AsSpan(0, _nextId));
    }

    public void Restore(ref SnapshotReader reader)
    {
        _nextId = reader.Read<int>();
        _freeCount = reader.Read<int>();
        if (_freeCount > _free.Length) Array.Resize(ref _free, int.Max(_freeCount, _free.Length * 2));
        EnsureSlot(_nextId - 1);

        reader.ReadSpan(_free.AsSpan(0, _freeCount));
        reader.ReadSpan(_generations.AsSpan(0, _nextId));
        reader.ReadSpan(_states.AsSpan(0, _nextId));
        _generations.AsSpan(_nextId).Clear();
        _states.AsSpan(_nextId).Clear();
    }

    private void EnsureSlot(int id)
    {
        if (id < _generations.Length) return;
        var size = int.Max(id + 1, _generations.Length * 2);
        Array.Resize(ref _generations, size);
        Array.Resize(ref _states, size);
    }
}
