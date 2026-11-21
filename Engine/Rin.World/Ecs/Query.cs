using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     Entities with every queried component. <c>world.Query&lt;...&gt;()</c> is cached, so it can be called every
///     frame. <see cref="With{T}" />, <see cref="Without{T}" /> and <see cref="Changed{T}" /> make a new query, so
///     build a filtered query once and keep it.
/// </summary>
public readonly struct Query<T1>
    where T1 : unmanaged, IComponentData
{
    private readonly QueryState _state;

    internal Query(QueryState state) => _state = state;

    public int EntityCount => _state.EntityCount;

    /// <summary>
    ///     A new query that also requires the component or tag <typeparamref name="T" />.
    /// </summary>
    public Query<T1> With<T>() where T : unmanaged => new(_state.With(_state.Store.Schema.IdOf<T>()));

    /// <summary>
    ///     A new query that skips entities with the component or tag <typeparamref name="T" />.
    /// </summary>
    public Query<T1> Without<T>() where T : unmanaged => new(_state.Without(_state.Store.Schema.IdOf<T>()));

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Check rows with
    ///     <see cref="QueryChunk{T1}.HasChanged{T}" />. Throws when iterated during the tick.
    /// </summary>
    public Query<T1> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed(_state.Store.Schema.IdOf<T>()));

    public QueryEnumerator<T1> GetEnumerator() => new(_state);
}

/// <summary>
///     Blocks structural changes until disposed, which <c>foreach</c> does. Dispose one taken by hand exactly once.
/// </summary>
public struct QueryEnumerator<T1> : IDisposable
    where T1 : unmanaged, IComponentData
{
    private ChunkCursor _cursor;
    private readonly ComponentId _id1;

    internal QueryEnumerator(QueryState state)
    {
        _id1 = state.Required[0];
        _cursor = new ChunkCursor(state);
    }

    public QueryChunk<T1> Current
    {
        get
        {
            var archetype = _cursor.Current;
            return new QueryChunk<T1>(archetype.ColumnFor(_id1), _cursor.Entities, _cursor.Ticks);
        }
    }

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}

public readonly struct Query<T1, T2>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
{
    private readonly QueryState _state;

    internal Query(QueryState state) => _state = state;

    public int EntityCount => _state.EntityCount;

    public Query<T1, T2> With<T>() where T : unmanaged => new(_state.With(_state.Store.Schema.IdOf<T>()));

    public Query<T1, T2> Without<T>() where T : unmanaged => new(_state.Without(_state.Store.Schema.IdOf<T>()));

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed(_state.Store.Schema.IdOf<T>()));

    public QueryEnumerator<T1, T2> GetEnumerator() => new(_state);
}

/// <summary>
///     Blocks structural changes until disposed, which <c>foreach</c> does. Dispose one taken by hand exactly once.
/// </summary>
public struct QueryEnumerator<T1, T2> : IDisposable
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
{
    private ChunkCursor _cursor;
    private readonly ComponentId _id1;
    private readonly ComponentId _id2;

    internal QueryEnumerator(QueryState state)
    {
        _id1 = state.Required[0];
        _id2 = state.Required[1];
        _cursor = new ChunkCursor(state);
    }

    public QueryChunk<T1, T2> Current
    {
        get
        {
            var archetype = _cursor.Current;
            return new QueryChunk<T1, T2>(archetype.ColumnFor(_id1), archetype.ColumnFor(_id2), _cursor.Entities, _cursor.Ticks);
        }
    }

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}

public readonly struct Query<T1, T2, T3>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
{
    private readonly QueryState _state;

    internal Query(QueryState state) => _state = state;

    public int EntityCount => _state.EntityCount;

    public Query<T1, T2, T3> With<T>() where T : unmanaged => new(_state.With(_state.Store.Schema.IdOf<T>()));

    public Query<T1, T2, T3> Without<T>() where T : unmanaged => new(_state.Without(_state.Store.Schema.IdOf<T>()));

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2, T3> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed(_state.Store.Schema.IdOf<T>()));

    public QueryEnumerator<T1, T2, T3> GetEnumerator() => new(_state);
}

/// <summary>
///     Blocks structural changes until disposed, which <c>foreach</c> does. Dispose one taken by hand exactly once.
/// </summary>
public struct QueryEnumerator<T1, T2, T3> : IDisposable
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
{
    private ChunkCursor _cursor;
    private readonly ComponentId _id1;
    private readonly ComponentId _id2;
    private readonly ComponentId _id3;

    internal QueryEnumerator(QueryState state)
    {
        _id1 = state.Required[0];
        _id2 = state.Required[1];
        _id3 = state.Required[2];
        _cursor = new ChunkCursor(state);
    }

    public QueryChunk<T1, T2, T3> Current
    {
        get
        {
            var archetype = _cursor.Current;
            return new QueryChunk<T1, T2, T3>(archetype.ColumnFor(_id1), archetype.ColumnFor(_id2), archetype.ColumnFor(_id3),
                _cursor.Entities, _cursor.Ticks);
        }
    }

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}

public readonly struct Query<T1, T2, T3, T4>
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
    where T4 : unmanaged, IComponentData
{
    private readonly QueryState _state;

    internal Query(QueryState state) => _state = state;

    public int EntityCount => _state.EntityCount;

    public Query<T1, T2, T3, T4> With<T>() where T : unmanaged => new(_state.With(_state.Store.Schema.IdOf<T>()));

    public Query<T1, T2, T3, T4> Without<T>() where T : unmanaged => new(_state.Without(_state.Store.Schema.IdOf<T>()));

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2, T3, T4> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed(_state.Store.Schema.IdOf<T>()));

    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator() => new(_state);
}

/// <summary>
///     Blocks structural changes until disposed, which <c>foreach</c> does. Dispose one taken by hand exactly once.
/// </summary>
public struct QueryEnumerator<T1, T2, T3, T4> : IDisposable
    where T1 : unmanaged, IComponentData
    where T2 : unmanaged, IComponentData
    where T3 : unmanaged, IComponentData
    where T4 : unmanaged, IComponentData
{
    private ChunkCursor _cursor;
    private readonly ComponentId _id1;
    private readonly ComponentId _id2;
    private readonly ComponentId _id3;
    private readonly ComponentId _id4;

    internal QueryEnumerator(QueryState state)
    {
        _id1 = state.Required[0];
        _id2 = state.Required[1];
        _id3 = state.Required[2];
        _id4 = state.Required[3];
        _cursor = new ChunkCursor(state);
    }

    public QueryChunk<T1, T2, T3, T4> Current
    {
        get
        {
            var archetype = _cursor.Current;
            return new QueryChunk<T1, T2, T3, T4>(archetype.ColumnFor(_id1), archetype.ColumnFor(_id2), archetype.ColumnFor(_id3),
                archetype.ColumnFor(_id4), _cursor.Entities, _cursor.Ticks);
        }
    }

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}

/// <summary>
///     Walks the archetypes a query visits and holds the store's structural change guard until disposed. Disposing the
///     same cursor again does nothing, but disposing a copy ends the loop a second time, which throws.
/// </summary>
internal struct ChunkCursor
{
    private readonly QueryState _state;
    private readonly List<Archetype> _archetypes;
    private int _index;
    private bool _ended;

    public ChunkCursor(QueryState state)
    {
        _state = state;
        _archetypes = state.Matched;
        _index = -1;
        var since = state.BeginRun();
        Ticks = new ChunkChangeTicks(state.Store.Clock.Current, since, state.TracksChanges);
        state.Store.Phase.BeginIteration();
    }

    public Archetype Current => _archetypes[_index];

    public ChunkEntities Entities => new(Current.EntityIdArray, Current.Count, _state.Store.Entities);

    public ChunkChangeTicks Ticks { get; }

    public bool MoveNext()
    {
        while (++_index < _archetypes.Count)
            if (_state.Visits(_archetypes[_index], Ticks.Since)) return true;
        return false;
    }

    public void Dispose()
    {
        if (_ended) return;
        _ended = true;
        _state.Store.Phase.EndIteration();
    }
}
