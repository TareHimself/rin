using System.Numerics;
using Rin.World.Components;

namespace Rin.World.Physics;

public class HitResult
{
    public required Vector3 Location { get; set; }
    public required Vector3 Normal { get; set; }
    public required IPhysicsComponent Component { get; set; }
    public required IPhysicsBody Body { get; set; }
}