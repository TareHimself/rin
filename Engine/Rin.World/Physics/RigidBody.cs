
using Rin.World.Transforms;

namespace Rin.World.Physics;

/// <summary>
///     A physics body for the entity, shaped by its collider component (<see cref="BoxCollider" /> and the others).
///     <see cref="PhysicsFeature" /> builds the body once the entity has both, and reads them only then: overwriting
///     either does not rebuild the body, so remove and add it again to change the shape or state.
/// </summary>
/// <remarks>
///     <see cref="PhysicsState.Static" /> bodies are placed once, <see cref="PhysicsState.Controlled" /> bodies follow the
///     entity's world pose every tick, and <see cref="PhysicsState.Simulated" /> bodies write their pose back into
///     <see cref="Transforms.LocalTransform" />.
/// </remarks>
[EcsRequires<LocalTransform>]
public struct RigidBody() : IComponentData
{
    public PhysicsState State;
    public float Mass = 1f;

    /// <summary>
    ///     Other bodies pass through a trigger, and touching it is reported as a contact with
    ///     <see cref="EntityContact.IsTrigger" /> set.
    /// </summary>
    public bool IsTrigger;

    public int CollisionChannel;
}
