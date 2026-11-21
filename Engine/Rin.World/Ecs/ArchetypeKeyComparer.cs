namespace Rin.World.Ecs;

/// <summary>
///     Compares archetype keys by content, and lets a span be looked up without allocating a key array.
/// </summary>
internal sealed class ArchetypeKeyComparer
    : IEqualityComparer<ComponentId[]>, IAlternateEqualityComparer<ReadOnlySpan<ComponentId>, ComponentId[]>
{
    public static readonly ArchetypeKeyComparer Instance = new();

    public bool Equals(ComponentId[]? x, ComponentId[]? y) => x.AsSpan().SequenceEqual(y);

    public int GetHashCode(ComponentId[] key) => Hash(key);

    public bool Equals(ReadOnlySpan<ComponentId> alternate, ComponentId[] other) => alternate.SequenceEqual(other);

    public int GetHashCode(ReadOnlySpan<ComponentId> alternate) => Hash(alternate);

    public ComponentId[] Create(ReadOnlySpan<ComponentId> alternate) => alternate.ToArray();

    private static int Hash(ReadOnlySpan<ComponentId> key)
    {
        var hash = new HashCode();
        foreach (var id in key) hash.Add(id.Value);
        return hash.ToHashCode();
    }
}
