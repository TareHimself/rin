namespace Rin.World;

/// <summary>
///     A component type's index in one <see cref="ComponentSchema" />. Worlds that share a schema share its ids.
/// </summary>
public readonly record struct ComponentId(int Value) : IComparable<ComponentId>
{
    public int CompareTo(ComponentId other) => Value.CompareTo(other.Value);
}
