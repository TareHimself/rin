using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Jolt;

public class JoltPhysics : IPhysicsSystem
{

    public JoltPhysics()
    {
       // JoltPhysicsSharp.Foundation.Init()
    }
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Vec3<float> Gravity { get; set; }
    public IPhysicsBox CreateBox(IPhysicsComponent owner, Vec3<float> size)
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

    public RayCastResult? RayCast(Vec3<float> begin, Vec3<float> direction, float distance, int channel)
    {
        throw new NotImplementedException();
    }
}