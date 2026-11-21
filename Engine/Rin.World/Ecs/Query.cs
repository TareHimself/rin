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
    public Query<T1> With<T>() where T : unmanaged => new(_state.With<T>());

    /// <summary>
    ///     A new query that skips entities with the component or tag <typeparamref name="T" />.
    /// </summary>
    public Query<T1> Without<T>() where T : unmanaged => new(_state.Without<T>());

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Check rows with
    ///     <see cref="QueryChunk{T1}.HasChanged{T}" />. Throws when iterated during the tick.
    /// </summary>
    public Query<T1> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed<T>());

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

    public QueryChunk<T1> Current => new(_cursor.Current, _id1);

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

    public Query<T1, T2> With<T>() where T : unmanaged => new(_state.With<T>());

    public Query<T1, T2> Without<T>() where T : unmanaged => new(_state.Without<T>());

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed<T>());

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

    public QueryChunk<T1, T2> Current => new(_cursor.Current, _id1, _id2);

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

    public Query<T1, T2, T3> With<T>() where T : unmanaged => new(_state.With<T>());

    public Query<T1, T2, T3> Without<T>() where T : unmanaged => new(_state.Without<T>());

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2, T3> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed<T>());

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

    public QueryChunk<T1, T2, T3> Current => new(_cursor.Current, _id1, _id2, _id3);

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

    public Query<T1, T2, T3, T4> With<T>() where T : unmanaged => new(_state.With<T>());

    public Query<T1, T2, T3, T4> Without<T>() where T : unmanaged => new(_state.Without<T>());

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where a
    ///     <c>Changed</c> component was written since its last run. Throws when iterated during the tick.
    /// </summary>
    public Query<T1, T2, T3, T4> Changed<T>() where T : unmanaged, IComponentData => new(_state.Changed<T>());

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

    public QueryChunk<T1, T2, T3, T4> Current => new(_cursor.Current, _id1, _id2, _id3, _id4);

    public bool MoveNext() => _cursor.MoveNext();

    public void Dispose() => _cursor.Dispose();
}
