namespace Rin.World;

/// <summary>
///     A handle to an entity in an <see cref="World" />. A recycled id gets a new generation, so stale handles stop
///     matching.
/// </summary>
public readonly struct Entity(int id, short generation) : IEquatable<Entity>
{
    public int Id { get; } = id;

    public short Generation { get; } = generation;

    /// <summary>
    ///     False for <c>default</c>. Says nothing about whether the entity is still alive, see <see cref="World.IsAlive" />.
    /// </summary>
    public bool IsValid => Id != 0;

    public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Generation);

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"Entity({Id}:{Generation})" : "Entity(none)";
}
