using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     Entities with every queried component. <c>world.Query&lt;...&gt;()</c> is cached, so it can be called every
///     frame. <see cref="With{T}" /> and <see cref="Without{T}" /> make a new query, so build a filtered query once
///     and keep it.
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

    public QueryEnumerator<T1> GetEnumerator() => new(_state);
}

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
            return new QueryChunk<T1>(archetype.Span<T1>(_id1), _cursor.Entities);
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

    public QueryEnumerator<T1, T2> GetEnumerator() => new(_state);
}

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
            return new QueryChunk<T1, T2>(archetype.Span<T1>(_id1), archetype.Span<T2>(_id2), _cursor.Entities);
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

    public QueryEnumerator<T1, T2, T3> GetEnumerator() => new(_state);
}

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
            return new QueryChunk<T1, T2, T3>(archetype.Span<T1>(_id1), archetype.Span<T2>(_id2), archetype.Span<T3>(_id3), _cursor.Entities);
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

    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator() => new(_state);
}

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
            return new QueryChunk<T1, T2, T3, T4>(archetype.Span<T1>(_id1), archetype.Span<T2>(_id2), archetype.Span<T3>(_id3), archetype.Span<T4>(_id4), _cursor.Entities);
        }
    }

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}

/// <summary>
///     Walks a query's non-empty archetypes and holds the store's structural change guard until disposed.
/// </summary>
internal struct ChunkCursor
{
    private readonly ArchetypeStore _store;
    private readonly List<Archetype> _archetypes;
    private int _index;

    public ChunkCursor(QueryState state)
    {
        _store = state.Store;
        _archetypes = state.Matched;
        _index = -1;
        _store.BeginIteration();
    }

    public Archetype Current => _archetypes[_index];

    public ChunkEntities Entities => new(Current.EntityIdArray, Current.Count, _store.Ids);

    public bool MoveNext()
    {
        while (++_index < _archetypes.Count)
            if (_archetypes[_index].Count > 0) return true;
        return false;
    }

    public void Dispose() => _store.EndIteration();
}
