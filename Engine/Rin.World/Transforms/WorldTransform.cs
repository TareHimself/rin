using System.Numerics;

namespace Rin.World.Transforms;

/// <summary>
///     The world matrix written by <see cref="TransformPropagation" />, only when it changes, so its change tick tells
///     consumers which entities moved.
/// </summary>
public struct WorldTransform() : IComponentData
{
    public Matrix4x4 Matrix = Matrix4x4.Identity;
}
