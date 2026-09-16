using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.World.Physics;

public interface IPhysicsSystem
{
    public Vector3 GetGravity();
    public void SetGravity(in Vector3 gravity);

    public PhysicsBodyHandle CreateBox(in Vector3 size, in Transform transform)
    {
        return CreateBox(size, transform, PhysicsState.Static);
    }

    public PhysicsBodyHandle CreateSphere(float radius, in Transform transform)
    {
        return CreateSphere(radius, transform, PhysicsState.Static);
    }

    public PhysicsBodyHandle CreateCapsule(float radius, float halfHeight, in Transform transform)
    {
        return CreateCapsule(radius, halfHeight, transform, PhysicsState.Static);
    }

    public PhysicsBodyHandle CreateBox(in Vector3 size, in Transform transform, PhysicsState state);
    public PhysicsBodyHandle CreateSphere(float radius, in Transform transform, PhysicsState state);
    public PhysicsBodyHandle CreateCapsule(float radius, float halfHeight, in Transform transform, PhysicsState state);
    public void DestroyBody(PhysicsBodyHandle handle);

    public Vector3 GetLinearVelocity(PhysicsBodyHandle handle);
    public void SetLinearVelocity(PhysicsBodyHandle handle, in Vector3 velocity);
    public Vector3 GetAngularVelocity(PhysicsBodyHandle handle);
    public void SetAngularVelocity(PhysicsBodyHandle handle, in Vector3 velocity);
    public Vector3 GetPosition(PhysicsBodyHandle handle);
    public void SetPosition(PhysicsBodyHandle handle, in Vector3 position);
    public Quaternion GetOrientation(PhysicsBodyHandle handle);
    public void SetOrientation(PhysicsBodyHandle handle, in Quaternion orientation);
    public Vector3 GetScale(PhysicsBodyHandle handle);
    public void SetScale(PhysicsBodyHandle handle, in Vector3 scale);
    public PhysicsState GetState(PhysicsBodyHandle handle);
    public void SetState(PhysicsBodyHandle handle, PhysicsState state);
    public float GetMass(PhysicsBodyHandle handle);
    public void SetMass(PhysicsBodyHandle handle, float mass);

    public Vector3 GetBoxSize(PhysicsBodyHandle handle);
    public void SetBoxSize(PhysicsBodyHandle handle, in Vector3 size);
    public float GetSphereRadius(PhysicsBodyHandle handle);
    public void SetSphereRadius(PhysicsBodyHandle handle, float radius);
    public float GetCapsuleRadius(PhysicsBodyHandle handle);
    public float GetCapsuleHalfHeight(PhysicsBodyHandle handle);
    public void SetCapsuleRadius(PhysicsBodyHandle handle, float radius);
    public void SetCapsuleHalfHeight(PhysicsBodyHandle handle, float halfHeight);

    /// <summary>
    ///     Scales simulated time for this body only (1 = normal); 0 is a momentum-preserving freeze,
    ///     not a position lock. Only affects <see cref="Physics.PhysicsState.Simulated" /> bodies.
    /// </summary>
    public void SetTimeScale(PhysicsBodyHandle handle, float scale);

    public float GetTimeScale(PhysicsBodyHandle handle);

    public int GetCollisionChannel(PhysicsBodyHandle handle);
    public void SetCollisionChannel(PhysicsBodyHandle handle, int channel);

    public void Update(float deltaTime);

    public void Destroy();

    /// <summary>
    ///     Closest hit, if any. <paramref name="channel" /> of -1 matches every channel; any other value
    ///     only matches bodies whose <see cref="SetCollisionChannel" /> equals it.
    /// </summary>
    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    public RayCastResult[] RayCastAll(in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    public RayCastResult? SphereCast(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    public RayCastResult[] SphereCastAll(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    public RayCastResult? BoxCast(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, int channel = -1);

    public RayCastResult[] BoxCastAll(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, int channel = -1);

    public RayCastResult? CapsuleCast(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, int channel = -1);

    public RayCastResult[] CapsuleCastAll(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, int channel = -1);

    /// <summary>Bodies currently overlapping the shape — zero motion, no <see cref="RayCastResult" /> location/normal to report.</summary>
    public PhysicsBodyHandle[] OverlapSphere(float radius, in Vector3 center, int channel = -1);

    public PhysicsBodyHandle[] OverlapBox(in Vector3 size, in Vector3 center, in Quaternion orientation, int channel = -1);

    public PhysicsBodyHandle[] OverlapCapsule(float radius, float halfHeight, in Vector3 center, in Quaternion orientation,
        int channel = -1);
}
