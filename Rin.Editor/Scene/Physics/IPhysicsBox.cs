using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Physics;

public interface IPhysicsBox : IPhysicsBody
{
    public Vector3 Size { get; set; }
}