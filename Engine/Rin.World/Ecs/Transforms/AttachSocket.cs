using System.Numerics;

namespace Rin.World.Ecs.Transforms;

/// <summary>
///     Makes the entity follow bone <see cref="BoneIndex" /> of its parent's skinned pose. Propagation computes
///     local * <see cref="SocketMatrix" /> * parent world.
/// </summary>
public struct AttachSocket(int boneIndex) : IComponentData
{
    public int BoneIndex = boneIndex;

    /// <summary>
    ///     The bone's matrix in the parent's space as of the last socket sync.
    /// </summary>
    public Matrix4x4 SocketMatrix { get; internal set; } = Matrix4x4.Identity;
}
