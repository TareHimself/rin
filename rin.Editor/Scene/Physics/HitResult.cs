using System.Numerics;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Components;

namespace rin.Editor.Scene.Physics;

public class HitResult
{
    public required Vector3 Location { get; set; }
    public required Vector3 Normal { get; set; }
    public required IPhysicsComponent Component { get; set; }
}