using System.Numerics;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Components;

namespace rin.Editor.Scene.Physics;

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