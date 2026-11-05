
namespace Rin.World.Physics;

/// <summary>
///     A physics body for the entity. <see cref="PhysicsFeature" /> reads it once, when it is added: overwriting it does
///     not rebuild the body, so remove it and add it again to change the shape or state.
/// </summary>
/// <remarks>
///     <see cref="PhysicsState.Static" /> bodies are placed once, <see cref="PhysicsState.Controlled" /> bodies follow the
///     entity's world pose every tick, and <see cref="PhysicsState.Simulated" /> bodies write their pose back into
///     <see cref="Transforms.LocalTransform" />.
/// </remarks>
public struct RigidBody() : IComponentData
{
    public ColliderShape Shape;
    public PhysicsState State;
    public float Mass = 1f;

    /// <summary>
    ///     Other bodies pass through a trigger, and touching it is reported as a contact with
    ///     <see cref="EntityContact.IsTrigger" /> set.
    /// </summary>
    public bool IsTrigger;

    public int CollisionChannel;
}
