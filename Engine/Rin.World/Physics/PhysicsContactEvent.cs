namespace Rin.World.Physics;

public enum PhysicsContactKind
{
    Begin,
    End
}

/// <summary>
///     A pair of bodies starting or stopping touching during the last physics step. <see cref="IsTrigger" /> is true
///     when either body is a trigger, in which case the pair produced no contact constraint.
/// </summary>
public readonly record struct PhysicsContactEvent(PhysicsBodyHandle A, PhysicsBodyHandle B, PhysicsContactKind Kind, bool IsTrigger)
{
    public PhysicsBodyHandle Other(PhysicsBodyHandle self) => self == A ? B : A;
}
