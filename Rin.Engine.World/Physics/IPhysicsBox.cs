using System.Numerics;

namespace Rin.Engine.World.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vector3 Size { get; set; }
}