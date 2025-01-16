using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vec3<float> Size { get; set; }
}