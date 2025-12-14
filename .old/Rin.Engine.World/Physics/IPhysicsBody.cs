using System.Numerics;

namespace Rin.Engine.World.Physics;

public interface IPhysicsBody : IDisposable
{
    public int CollisionChannel { get; set; }
    public void ProcessHit(RayCastResult result);
    public Vector3 GetLinearVelocity();
    public Vector3 GetAngularVelocity();
    public Vector3 GetPosition();
    public Quaternion GetOrientation();
    public Vector3 GetScale();
    public PhysicsState GetState();
    public float GetMass();
    public void SetLinearVelocity(in Vector3 velocity);
    public void SetAngularVelocity(in Vector3 angularVelocity);
    public void SetPosition(in Vector3 position);
    public void SetOrientation(in Quaternion orientation);
    public void SetScale(in Vector3 scale);
    public void SetState(PhysicsState state);
    public void SetMass(float mass);
}