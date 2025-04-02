using System.Numerics;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Physics.Jolt;

public class JoltPhysics : IPhysicsSystem
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Vector3 Gravity { get; set; }

    public IPhysicsBox CreateBox(IPhysicsComponent owner, Vector3 size)
    {
        throw new NotImplementedException();
    }

    public IPhysicsSphere CreateSphere(IPhysicsComponent owner, float radius)
    {
        throw new NotImplementedException();
    }

    public IPhysicsCapsule CreateCapsule(IPhysicsComponent owner, float radius, float halfHeight)
    {
        throw new NotImplementedException();
    }

    public void Update(double deltaTime)
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public RayCastResult? RayCast(Vector3 begin, Vector3 direction, float distance, int channel)
    {
        throw new NotImplementedException();
    }
}