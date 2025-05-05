using System.Numerics;

namespace Rin.Engine.World.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vector3 GetSize();
    public Vector3 GetScaledSize();
    public void SetSize(in Vector3 size);
    public void SetScaledSize(in Vector3 size);
}