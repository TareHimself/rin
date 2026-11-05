namespace Rin.World;

/// <summary>
///     A handle to an entity in an <see cref="World" />. A recycled id gets a new generation, so stale handles stop
///     matching.
/// </summary>
public readonly record struct Entity(int Id, short Generation)
{
    /// <summary>
    ///     False for <c>default</c>. Says nothing about whether the entity is still alive, see <see cref="World.IsAlive" />.
    /// </summary>
    public bool IsValid => Id != 0;

    public override string ToString() => IsValid ? $"Entity({Id}:{Generation})" : "Entity(none)";
}
