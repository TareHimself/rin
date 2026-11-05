
namespace Rin.World.Physics;

/// <summary>
///     Added and removed by <see cref="PhysicsFeature" /> alongside a <see cref="RigidBody" />, and rebuilt by it after a
///     snapshot restore.
/// </summary>
public struct PhysicsBody : IDerivedComponentData
{
    public PhysicsBodyHandle Handle { get; internal set; }

    /// <summary>
    ///     The state the body was created with, which push and pull follow even if <see cref="RigidBody" /> is overwritten.
    /// </summary>
    public PhysicsState State { get; internal set; }
}
