using System.Buffers;
using Rin.Core.Shared.Math;

namespace Rin.World.Mesh.Skinning;

// Same shape as SkeletalPose, but pooled: copies of this struct alias the same underlying buffers rather
// than owning their own, so disposing one copy invalidates every other - dispose exactly once, right after
// this pose is consumed (blended into a parent, or materialized via ToSkeletalPose).
public struct PooledSkeletalPose : IDisposable
{
    private IMemoryOwner<Transform>? _transformsOwner;
    private IMemoryOwner<ulong>? _maskOwner;

    public Memory<Transform> BoneTransforms;
    public Memory<ulong> Mask;

    public PooledSkeletalPose(int boneCount)
    {
        _transformsOwner = MemoryPool<Transform>.Shared.Rent(boneCount);
        BoneTransforms = _transformsOwner.Memory[..boneCount];

        var maskLength = (boneCount + 63) / 64;
        _maskOwner = MemoryPool<ulong>.Shared.Rent(maskLength);
        Mask = _maskOwner.Memory[..maskLength];
        Mask.Span.Clear(); // rented memory is not zero-initialized, unlike `new ulong[n]`
    }

    public readonly bool IsSet(int boneIndex)
    {
        return (Mask.Span[boneIndex >> 6] & (1UL << (boneIndex & 63))) != 0;
    }

    public readonly void Set(int boneIndex, in Transform transform)
    {
        BoneTransforms.Span[boneIndex] = transform;
        Mask.Span[boneIndex >> 6] |= 1UL << (boneIndex & 63);
    }

    public static PooledSkeletalPose Blend(in PooledSkeletalPose a, in PooledSkeletalPose b, float alpha)
    {
        var result = new PooledSkeletalPose(a.BoneTransforms.Length);
        var aSpan = a.BoneTransforms.Span;
        var bSpan = b.BoneTransforms.Span;

        for (var i = 0; i < result.BoneTransforms.Length; i++)
        {
            var inA = a.IsSet(i);
            var inB = b.IsSet(i);

            if (inA && inB) result.Set(i, MathR.Interpolate(aSpan[i], bSpan[i], alpha));
            else if (inA) result.Set(i, aSpan[i]);
            else if (inB) result.Set(i, bSpan[i]);
        }

        return result;
    }

    public readonly SkeletalPose ToSkeletalPose()
    {
        var result = new SkeletalPose(BoneTransforms.Length);
        BoneTransforms.Span.CopyTo(result.BoneTransforms);
        Mask.Span.CopyTo(result.Mask);
        return result;
    }

    public void Dispose()
    {
        _transformsOwner?.Dispose();
        _transformsOwner = null;
        _maskOwner?.Dispose();
        _maskOwner = null;
    }
}
