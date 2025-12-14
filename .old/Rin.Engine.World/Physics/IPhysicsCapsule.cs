namespace Rin.Engine.World.Physics;

public interface IPhysicsCapsule : IPhysicsBody
{
    public float GetRadius();
    public float GetHalfHeight();
    public float GetScaledRadius();
    public float GetScaledHalfHeight();
    public void SetRadius(float radius);
    public void SetHalfHeight(float halfHeight);
    public void SetScaledRadius(float radius);
    public void SetScaledHalfHeight(float halfHeight);
}