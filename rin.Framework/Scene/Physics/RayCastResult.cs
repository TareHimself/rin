using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics;

public class RayCastResult
{
    public required Vec3<float> Location { get; set; }
    public required Vec3<float> Normal { get; set; }
    public required float Distance { get; set; }
    public required IPhysicsComponent Component { get; set; }
}