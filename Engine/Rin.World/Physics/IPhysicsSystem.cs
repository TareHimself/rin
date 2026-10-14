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
    /// <summary>
    ///     Triangle mesh collider. Static means it can never be physically simulated, so it cannot move or have its state changed.
    ///     Triangles are one-sided: each only collides from the side opposite cross(b - a, c - a).
    /// </summary>
    public PhysicsBodyHandle CreateStaticMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<uint> indices,
        in Transform transform);

    /// <summary>
    ///     Convex hull collider around <paramref name="points" />. The body origin is the hull's centre, so GetPosition reports the centre.
    /// </summary>
    public PhysicsBodyHandle CreateConvexHull(ReadOnlySpan<Vector3> points, in Transform transform, PhysicsState state);

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

    /// <summary>
    ///     A trigger body never produces contact constraints, so other bodies pass through it, but pairs touching it
    ///     are still reported through <see cref="ContactEvents" />.
    /// </summary>
    public bool GetTrigger(PhysicsBodyHandle handle);

    public void SetTrigger(PhysicsBodyHandle handle, bool trigger);

    /// <summary>
    ///     Pairs that began or stopped touching during the last <see cref="Update" />, valid until the next one. A pair
    ///     whose bodies have all gone to sleep stays touching rather than ending.
    /// </summary>
    public ReadOnlySpan<PhysicsContactEvent> ContactEvents { get; }

    public void Update(float deltaTime);

    public void Destroy();

    /// <summary>
    ///     Closest hit, if any. <paramref name="channel" /> of -1 matches every channel; any other value
    ///     only matches bodies whose <see cref="SetCollisionChannel" /> equals it.
    /// </summary>
    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    /// <summary>
    ///     Reports every hit along the ray to <paramref name="handler" />, in traversal order, until it returns false.
    /// </summary>
    public void RayCastAll<THandler>(in Vector3 begin, in Vector3 direction, float distance, ref THandler handler,
        int channel = -1) where THandler : struct, IPhysicsHitHandler;

    public RayCastResult? SphereCast(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1);

    public void SphereCastAll<THandler>(float radius, in Vector3 begin, in Vector3 direction, float distance,
        ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler;

    public RayCastResult? BoxCast(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, int channel = -1);

    public void BoxCastAll<THandler>(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler;

    public RayCastResult? CapsuleCast(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, int channel = -1);

    public void CapsuleCastAll<THandler>(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler;

    /// <summary>
    ///     Reports every body intersecting the shape right now. There is no motion, so there is no hit location, normal or
    ///     distance to report.
    /// </summary>
    public void OverlapSphere<THandler>(float radius, in Vector3 center, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsOverlapHandler;

    public void OverlapBox<THandler>(in Vector3 size, in Vector3 center, in Quaternion orientation, ref THandler handler,
        int channel = -1) where THandler : struct, IPhysicsOverlapHandler;

    public void OverlapCapsule<THandler>(float radius, float halfHeight, in Vector3 center, in Quaternion orientation,
        ref THandler handler, int channel = -1) where THandler : struct, IPhysicsOverlapHandler;
}
