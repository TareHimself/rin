namespace Rin.World.Ecs;

internal sealed partial class ArchetypeStore
{
    private QueryState?[] _cachedQueries = [];

    public QueryState Query<T1>()
        where T1 : unmanaged =>
        CachedQuery(QuerySlot<T1>.Value) ?? Cache(QuerySlot<T1>.Value, [Schema.IdOf<T1>()]);

    public QueryState Query<T1, T2>()
        where T1 : unmanaged
        where T2 : unmanaged =>
        CachedQuery(QuerySlot<T1, T2>.Value) ??
        Cache(QuerySlot<T1, T2>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>()]);

    public QueryState Query<T1, T2, T3>()
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged =>
        CachedQuery(QuerySlot<T1, T2, T3>.Value) ??
        Cache(QuerySlot<T1, T2, T3>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>(), Schema.IdOf<T3>()]);

    public QueryState Query<T1, T2, T3, T4>()
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged =>
        CachedQuery(QuerySlot<T1, T2, T3, T4>.Value) ??
        Cache(QuerySlot<T1, T2, T3, T4>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>(), Schema.IdOf<T3>(), Schema.IdOf<T4>()]);

    private QueryState? CachedQuery(int slot) => slot < _cachedQueries.Length ? _cachedQueries[slot] : null;

    private QueryState Cache(int slot, ComponentId[] required)
    {
        if (slot >= _cachedQueries.Length) Array.Resize(ref _cachedQueries, int.Max(slot + 1, _cachedQueries.Length * 2));
        return _cachedQueries[slot] = new QueryState(this, required, []);
    }
}
