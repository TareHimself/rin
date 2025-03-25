using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Scene.Components;

namespace Rin.Engine.Scene.Physics;

public interface IPhysicsBody : IDisposable
{
    public IPhysicsComponent Owner { get; set; }
    public Vector3 LinearVelocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    
    public bool IsStatic { get; set; }
    
    public bool IsSimulating { get; }
    
    public float Mass { get; set; }
    
    public int CollisionChannel { get; set; }
    
    public void SetSimulatePhysics(bool simulate);
    
    public Transform GetTransform();
    
    public void ProcessHit(RayCastResult result);
}