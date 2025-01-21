using rin.Framework.Core.Math;
using rin.Framework.Scene.Physics;

namespace rin.Framework.Scene.Components;

public interface IPhysicsComponent : ISceneComponent
{
    public Vec3<float> Velocity { get; set; }
    public Vec3<float> AngularVelocity { get; set; }
    public float Mass { get; set; }
    public bool IsSimulating { get; set; }

    public void ProcessHit(IPhysicsBody body, RayCastResult result);
}