using System.Numerics;
using Rin.Engine.Scene.Components;

namespace Rin.Engine.Scene.Physics;

public interface IPhysicsSystem : IDisposable
{
    public Vector3 Gravity { get; set; }
    public IPhysicsBox CreateBox(IPhysicsComponent owner,Vector3 size);
    public IPhysicsSphere CreateSphere(IPhysicsComponent owner,float radius);
    
    public IPhysicsCapsule CreateCapsule(IPhysicsComponent owner,float radius, float halfHeight);
    
    public void Update(double deltaTime);

    public void Start();
    
    public RayCastResult? RayCast(Vector3 begin, Vector3 direction,float distance, int channel);
    
}