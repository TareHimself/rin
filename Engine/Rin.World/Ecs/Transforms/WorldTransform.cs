using System.Numerics;

namespace Rin.World.Ecs.Transforms;

/// <summary>
///     The world matrix written by <see cref="TransformPropagation" />. <see cref="Version" /> changes only when the
///     matrix does, so consumers can skip unchanged entities.
/// </summary>
public struct WorldTransform() : IComponentData
{
    public Matrix4x4 Matrix = Matrix4x4.Identity;
    public uint Version;
}
