using System.Numerics;

namespace Rin.World.Physics;

/// <summary>
///     The body's linear and angular velocity. A value that differs from what the body last exchanged is pushed on the
///     next tick; simulated bodies write the stepped velocity back.
/// </summary>
public struct Velocity : IComponentData
{
    public Vector3 Linear;
    public Vector3 Angular;

    internal readonly bool Matches(in Velocity other) => Linear == other.Linear && Angular == other.Angular;
}
