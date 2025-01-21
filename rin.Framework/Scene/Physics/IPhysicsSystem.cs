using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics;

public interface IPhysicsSystem : IDisposable
{
    public Vec3<float> Gravity { get; set; }
    public IPhysicsBox CreateBox(IPhysicsComponent owner,Vec3<float> size);
    public IPhysicsSphere CreateSphere(IPhysicsComponent owner,float radius);
    
    public IPhysicsCapsule CreateCapsule(IPhysicsComponent owner,float radius, float halfHeight);
    
    public void Update(double deltaTime);

    public void Start();
    
    public RayCastResult? RayCast(Vec3<float> begin, Vec3<float> direction,float distance, int channel);
    
}