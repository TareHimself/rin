namespace Rin.Engine.World.Physics;

public interface IPhysicsSphere : IPhysicsBody
{
    public float GetRadius();
    public float GetScaledRadius();

    public void SetRadius(float radius);
    public void SetScaledRadius(float radius);
}