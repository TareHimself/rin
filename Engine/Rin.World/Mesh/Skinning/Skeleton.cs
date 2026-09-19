using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Shared.Math;

namespace Rin.World.Mesh.Skinning;

public class Skeleton
{
    [PublicAPI] public readonly Bone[] Bones;

    // -1 for a root bone.
    [PublicAPI] public readonly int[] ParentIndices;

    [PublicAPI] public readonly FrozenDictionary<string, int> BoneNameToIndex;

    public SkeletalPose BasePose;

    [PublicAPI] public Bone Root;

    public Skeleton(Bone[] bones)
    {
        Bones = bones;
        Root = bones.FirstOrDefault(c => c.Parent == null) ?? throw new NullReferenceException();
        BoneNameToIndex = bones.Select((bone, index) => (bone.Name, index))
            .ToFrozenDictionary(pair => pair.Name, pair => pair.index);

        var boneIndex = new Dictionary<Bone, int>(bones.Length);
        for (var i = 0; i < bones.Length; i++) boneIndex[bones[i]] = i;

        ParentIndices = new int[bones.Length];
        for (var i = 0; i < bones.Length; i++)
            ParentIndices[i] = bones[i].Parent is { } parent && boneIndex.TryGetValue(parent, out var parentIndex)
                ? parentIndex
                : -1;

        BasePose = new SkeletalPose(bones.Length);
    }

    public Matrix4x4[] ResolvePose(in SkeletalPose pose)
    {
        var result = new Matrix4x4[Bones.Length];
        var resolved = new bool[Bones.Length];
        for (var i = 0; i < Bones.Length; i++) ResolveBone(i, pose, result, resolved);
        return result;
    }

    private void ResolveBone(int index, in SkeletalPose pose, Matrix4x4[] result, bool[] resolved)
    {
        if (resolved[index]) return;

        var local = (pose.IsSet(index) ? pose.BoneTransforms[index] : Bones[index].LocalTransform).ToMatrix();

        var parentIndex = ParentIndices[index];
        if (parentIndex < 0)
        {
            result[index] = local;
        }
        else
        {
            ResolveBone(parentIndex, pose, result, resolved);
            result[index] = local * result[parentIndex];
        }

        resolved[index] = true;
    }
}
