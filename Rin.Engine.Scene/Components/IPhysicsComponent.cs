using System.Numerics;
using Rin.Engine.Scene.Physics;

namespace Rin.Engine.Scene.Components;

public interface IPhysicsComponent : ISceneComponent
{
    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; }
    public bool IsSimulating { get; set; }

    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}