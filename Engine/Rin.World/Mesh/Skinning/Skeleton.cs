using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Shared;
using Rin.Core.Shared.Math;
using Rin.Core.Shared.Pooling;

namespace Rin.World.Mesh.Skinning;

public class Skeleton
{
    private const int MaxStackResolvedFlags = 1024;
    private const int MaxStackLocalTransforms = 128;

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
        ResolvePose(pose, result);
        return result;
    }

    /// <summary>
    ///     Writes every bone's model-space matrix into <paramref name="into" /> without allocating.
    /// </summary>
    public void ResolvePose(in SkeletalPose pose, Span<Matrix4x4> into)
    {
        using var locals = new ScratchSpan<Transform>(Bones.Length,
            Bones.Length <= MaxStackLocalTransforms ? stackalloc Transform[Bones.Length] : default);
        CompleteLocalPose(pose, locals.Span);
        ResolvePose(locals.Span, into);
    }

    /// <summary>
    ///     Writes every bone's model-space matrix from a local transform per bone.
    /// </summary>
    public void ResolvePose(ReadOnlySpan<Transform> locals, Span<Matrix4x4> into)
    {
        using var scratch = new ScratchSpan<bool>(Bones.Length,
            Bones.Length <= MaxStackResolvedFlags ? stackalloc bool[Bones.Length] : default);
        for (var i = 0; i < Bones.Length; i++) ResolveBone(i, locals, into, scratch.Span);
    }

    /// <summary>
    ///     Writes a local transform per bone: the pose's where it sets the bone, the skeleton's own elsewhere.
    /// </summary>
    public void CompleteLocalPose(in SkeletalPose pose, Span<Transform> into)
    {
        for (var i = 0; i < Bones.Length; i++)
            into[i] = pose.IsSet(i) ? pose.BoneTransforms[i] : Bones[i].LocalTransform;
    }

    public void ComputeSkinningMatrices(ReadOnlySpan<Matrix4x4> globalPose, Span<Matrix4x4> into)
    {
        for (var i = 0; i < Bones.Length; i++) into[i] = globalPose[i].ApplyBefore(Bones[i].Bind);
    }

    private void ResolveBone(int index, ReadOnlySpan<Transform> locals, Span<Matrix4x4> result,
        Span<bool> resolved)
    {
        if (resolved[index]) return;

        var local = locals[index].ToMatrix();

        var parentIndex = ParentIndices[index];
        if (parentIndex < 0)
        {
            result[index] = local;
        }
        else
        {
            ResolveBone(parentIndex, locals, result, resolved);
            result[index] = local.ChildOf(result[parentIndex]);
        }

        resolved[index] = true;
    }
}
