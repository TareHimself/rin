using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Editor.Scene.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vector3 Size { get; set; }
}