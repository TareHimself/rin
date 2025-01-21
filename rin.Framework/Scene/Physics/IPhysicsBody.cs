using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics;

public interface IPhysicsBody : IDisposable
{
    public IPhysicsComponent Owner { get; set; }
    public Vec3<float> LinearVelocity { get; set; }
    public Vec3<float> AngularVelocity { get; set; }
    
    public bool IsStatic { get; set; }
    
    public bool IsSimulating { get; }
    
    public float Mass { get; set; }
    
    public int CollisionChannel { get; set; }
    
    public void SetSimulatePhysics(bool simulate);
    
    public Transform GetTransform();
    
    public void ProcessHit(RayCastResult result);
}