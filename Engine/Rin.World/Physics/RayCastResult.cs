using System.Numerics;

namespace Rin.World.Physics;

public readonly record struct RayCastResult
{
    public required PhysicsBodyHandle Body { get; init; }
    public required Vector3 Location { get; init; }
    public required Vector3 Normal { get; init; }
    public required float Distance { get; init; }
}
