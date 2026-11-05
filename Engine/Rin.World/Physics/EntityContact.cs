
namespace Rin.World.Physics;

/// <summary>
///     A <see cref="PhysicsContactEvent" /> mapped to entities. An entity is <c>default</c> when its body has no entity,
///     for example a body created directly on the physics system.
/// </summary>
public readonly record struct EntityContact(Entity A, Entity B, PhysicsContactKind Kind, bool IsTrigger)
{
    public Entity Other(Entity self) => self == A ? B : A;

    public bool Involves(Entity entity) => entity == A || entity == B;
}
