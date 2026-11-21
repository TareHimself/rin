namespace Rin.World.Ecs;

public sealed partial class ArchetypeStore
{
    private QueryState?[] _cachedQueries = [];
    private int _iterationDepth;

    public bool IsIterating => _iterationDepth > 0;

    public QueryState Query<T1>()
        where T1 : struct =>
        CachedQuery(QuerySlot<T1>.Value) ?? Cache(QuerySlot<T1>.Value, [Schema.IdOf<T1>()]);

    public QueryState Query<T1, T2>()
        where T1 : struct
        where T2 : struct =>
        CachedQuery(QuerySlot<T1, T2>.Value) ??
        Cache(QuerySlot<T1, T2>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>()]);

    public QueryState Query<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct =>
        CachedQuery(QuerySlot<T1, T2, T3>.Value) ??
        Cache(QuerySlot<T1, T2, T3>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>(), Schema.IdOf<T3>()]);

    public QueryState Query<T1, T2, T3, T4>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct =>
        CachedQuery(QuerySlot<T1, T2, T3, T4>.Value) ??
        Cache(QuerySlot<T1, T2, T3, T4>.Value, [Schema.IdOf<T1>(), Schema.IdOf<T2>(), Schema.IdOf<T3>(), Schema.IdOf<T4>()]);

    /// <summary>
    ///     Structural changes throw until the matching <see cref="EndIteration" />, because they move rows under the loop.
    /// </summary>
    public void BeginIteration() => _iterationDepth++;

    public void EndIteration() => _iterationDepth--;

    private QueryState? CachedQuery(int slot) => slot < _cachedQueries.Length ? _cachedQueries[slot] : null;

    private QueryState Cache(int slot, ComponentId[] required)
    {
        if (slot >= _cachedQueries.Length) Array.Resize(ref _cachedQueries, int.Max(slot + 1, _cachedQueries.Length * 2));
        return _cachedQueries[slot] = new QueryState(this, required, []);
    }

    private void EnsureNotIterating()
    {
        if (_iterationDepth > 0)
            throw new InvalidOperationException("Entities cannot change structurally inside a query loop. Use World.Commands.");
    }
}
