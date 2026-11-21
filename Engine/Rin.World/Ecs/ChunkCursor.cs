namespace Rin.World.Ecs;

/// <summary>
///     Walks the archetypes a query visits and holds the store's structural change guard until disposed. Disposing the
///     same cursor again does nothing, but disposing a copy ends the loop a second time, which throws.
/// </summary>
internal struct ChunkCursor
{
    private readonly QueryState _state;
    private readonly List<Archetype> _archetypes;
    private readonly ChunkChangeTicks _ticks;
    private int _index;
    private bool _ended;

    public ChunkCursor(QueryState state)
    {
        _state = state;
        _archetypes = state.Matched;
        _index = -1;
        var since = state.BeginRun();
        _ticks = new ChunkChangeTicks(state.Store.Clock.Current, since, state.TracksChanges);
        state.Store.Phase.BeginIteration();
    }

    public ChunkView Current
    {
        get
        {
            var archetype = _archetypes[_index];
            var entities = new ChunkEntities(archetype.EntityIdArray, archetype.Count, _state.Store.Entities);
            return new ChunkView(archetype, entities, _ticks);
        }
    }

    public bool MoveNext()
    {
        while (++_index < _archetypes.Count)
            if (_state.Visits(_archetypes[_index], _ticks.Since)) return true;
        return false;
    }

    public void Dispose()
    {
        if (_ended) return;
        _ended = true;
        _state.Store.Phase.EndIteration();
    }
}
