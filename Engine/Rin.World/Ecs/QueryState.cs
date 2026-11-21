namespace Rin.World.Ecs;

/// <summary>
///     The archetypes that have every required and no excluded component. Archetypes are only ever appended, so each
///     update checks only the ones created since the last.
/// </summary>
public sealed class QueryState
{
    private readonly ComponentId[] _required;
    private readonly ComponentId[] _excluded;
    private readonly List<Archetype> _matched = [];
    private int _archetypesSeen;

    internal QueryState(ArchetypeStore store, ComponentId[] required, ComponentId[] excluded)
    {
        Store = store;
        _required = required;
        _excluded = excluded;
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

    public QueryState With(ComponentId id) => new(Store, [.. _required, id], _excluded);

    public QueryState Without(ComponentId id) => new(Store, _required, [.. _excluded, id]);

    public bool Matches(Archetype archetype) => archetype.HasAll(_required) && !archetype.HasAny(_excluded);

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
