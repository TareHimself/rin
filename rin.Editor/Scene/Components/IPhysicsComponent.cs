using System.Numerics;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Physics;

namespace rin.Editor.Scene.Components;

public interface IPhysicsComponent : ISceneComponent
{
    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; }
    public bool IsSimulating { get; set; }

    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}