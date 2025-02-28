using System.Numerics;
using Rin.Editor.Scene.Components;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Physics;

public class RayCastResult
{
    public required Vector3 Location { get; set; }
    public required Vector3 Normal { get; set; }
    public required float Distance { get; set; }
    public required IPhysicsComponent Component { get; set; }
}