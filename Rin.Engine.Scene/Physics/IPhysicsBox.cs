using System.Numerics;

namespace Rin.Engine.Scene.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vector3 Size { get; set; }
}