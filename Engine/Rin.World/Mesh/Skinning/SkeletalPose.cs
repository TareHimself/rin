using Rin.Core.Shared.Math;

namespace Rin.World.Mesh.Skinning;

public struct SkeletalPose(int boneCount)
{
    public Transform[] BoneTransforms = new Transform[boneCount];
    public ulong[] Mask = new ulong[(boneCount + 63) / 64];

    public readonly bool IsSet(int boneIndex)
    {
        return (Mask[boneIndex >> 6] & (1UL << (boneIndex & 63))) != 0;
    }

    public readonly void Set(int boneIndex, in Transform transform)
    {
        BoneTransforms[boneIndex] = transform;
        Mask[boneIndex >> 6] |= 1UL << (boneIndex & 63);
    }
}
