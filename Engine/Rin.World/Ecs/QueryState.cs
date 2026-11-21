namespace Rin.World.Ecs;

/// <summary>
///     The archetypes that have every required and no excluded component. Archetypes are only ever appended, so each
///     update checks only the ones created since the last.
/// </summary>
/// <remarks>
///     A query with <see cref="Changed" /> components keeps its own <see cref="ChangeWindow" /> and skips archetypes
///     where none of them changed since its last run.
/// </remarks>
internal sealed class QueryState
{
    private readonly ComponentId[] _required;
    private readonly ComponentId[] _excluded;
    private readonly ComponentId[] _changed;
    private readonly List<Archetype> _matched = [];
    private int _archetypesSeen;
    private ChangeWindow _window;

    public QueryState(ArchetypeStore store, ComponentId[] required, ComponentId[] excluded)
        : this(store, required, excluded, [])
    {
    }

    private QueryState(ArchetypeStore store, ComponentId[] required, ComponentId[] excluded, ComponentId[] changed)
    {
        Store = store;
        _required = required;
        _excluded = excluded;
        _changed = changed;
    }

    public ArchetypeStore Store { get; }

    /// <summary>
    ///     The queried components first, in order, then the <see cref="With" /> filters.
    /// </summary>
    public ReadOnlySpan<ComponentId> Required => _required;

    public ReadOnlySpan<ComponentId> Excluded => _excluded;

    /// <summary>
    ///     Every matching archetype in creation order, empty ones included.
    /// </summary>
    public List<Archetype> Matched
    {
        get
        {
            Update();
            return _matched;
        }
    }

    public int EntityCount
    {
        get
        {
            var count = 0;
            foreach (var archetype in Matched) count += archetype.Count;
            return count;
        }
    }

    public bool TracksChanges => _changed.Length > 0;

    public QueryState With<T>() where T : unmanaged => new(Store, [.. _required, Store.Schema.IdOf<T>()], _excluded, _changed);

    public QueryState Without<T>() where T : unmanaged => new(Store, _required, [.. _excluded, Store.Schema.IdOf<T>()], _changed);

    /// <summary>
    ///     A new query that also requires <typeparamref name="T" /> and visits only archetypes where one of its changed
    ///     components was written since its last run.
    /// </summary>
    public QueryState Changed<T>() where T : unmanaged
    {
        var id = Store.Schema.IdOf<T>();
        if (Store.Schema[id].IsTag)
            throw new ArgumentException($"{Store.Schema[id].Name} is a tag, so it has no change ticks.", nameof(id));
        ComponentId[] required = _required.AsSpan().Contains(id) ? _required : [.. _required, id];
        return new QueryState(Store, required, _excluded, [.. _changed, id]);
    }

    public bool Matches(Archetype archetype) => archetype.HasAll(_required) && !archetype.HasAny(_excluded);

    /// <summary>
    ///     Starts a loop over the query and returns the tick its rows are compared against. Throws for a change query
    ///     during the tick.
    /// </summary>
    public uint BeginRun()
    {
        if (!TracksChanges) return 0;
        Store.Clock.EnsureCanRunChangeQuery();
        _window.BeginRun(Store.Clock.Current);
        return _window.Since;
    }

    /// <summary>
    ///     Whether a loop that started at <paramref name="since" /> visits <paramref name="archetype" />.
    /// </summary>
    public bool Visits(Archetype archetype, uint since)
    {
        if (archetype.Count == 0) return false;
        if (!TracksChanges) return true;
        foreach (var id in _changed)
            if (archetype.Columns[archetype.ColumnOf(id)].LastChangeTick > since) return true;
        return false;
    }

    private void Update()
    {
        var archetypes = Store.Archetypes;
        for (; _archetypesSeen < archetypes.Count; _archetypesSeen++)
            if (Matches(archetypes[_archetypesSeen])) _matched.Add(archetypes[_archetypesSeen]);
    }
}

/// <summary>
///     A process-wide slot per queried type combination, so each store caches its unfiltered queries in an array.
/// </summary>
internal static class QuerySlot
{
    private static int _count;

    public static int Next() => Interlocked.Increment(ref _count) - 1;
}

internal static class QuerySlot<T1>
{
    public static readonly int Value = QuerySlot.Next();
}

internal static class QuerySlot<T1, T2>
{
    public static readonly int Value = QuerySlot.Next();
}

internal static class QuerySlot<T1, T2, T3>
{
    public static readonly int Value = QuerySlot.Next();
}

internal static class QuerySlot<T1, T2, T3, T4>
{
    public static readonly int Value = QuerySlot.Next();
}
