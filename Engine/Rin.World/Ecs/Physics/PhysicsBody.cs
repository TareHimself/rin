using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Added and removed by <see cref="PhysicsFeature" /> alongside a <see cref="RigidBody" />.
/// </summary>
public struct PhysicsBody : IComponentData
{
    public PhysicsBodyHandle Handle { get; internal set; }

    /// <summary>
    ///     The state the body was created with, which push and pull follow even if <see cref="RigidBody" /> is overwritten.
    /// </summary>
    public PhysicsState State { get; internal set; }
}
