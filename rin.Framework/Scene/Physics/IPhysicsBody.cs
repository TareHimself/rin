using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics;

public interface IPhysicsBody : IDisposable
{
    public ISceneComponent Owner { get; set; }
    public Vec3<float> Velocity { get; set; }
    public bool IsSimulating { get; }
    
    public float Mass { get; set; }
    public void SetSimulatePhysics(bool newState);
    
    public Transform GetTransform();
}