using System.Numerics;
using Rin.Engine.Math;

namespace Rin.Engine.World.Physics;

public interface IPhysicsSystem
{
    public Vector3 GetGravity();
    public void SetGravity(in Vector3 gravity);

    public IPhysicsBox CreateBox(in Vector3 size, in Transform transform)
    {
        return CreateBox(size, transform, PhysicsState.Static);
    }

    public IPhysicsSphere CreateSphere(float radius, in Transform transform)
    {
        return CreateSphere(radius, transform, PhysicsState.Static);
    }

    public IPhysicsCapsule CreateCapsule(float radius, float halfHeight, in Transform transform)
    {
        return CreateCapsule(radius, halfHeight, transform, PhysicsState.Static);
    }

    public IPhysicsBox CreateBox(in Vector3 size, in Transform transform, PhysicsState state);
    public IPhysicsSphere CreateSphere(float radius, in Transform transform, PhysicsState state);
    public IPhysicsCapsule CreateCapsule(float radius, float halfHeight, in Transform transform, PhysicsState state);

    public void Update(float deltaTime);

    public void Destroy();
    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel);
}