using Rin.Core.Shared;
using Rin.Core.Shared.Math;

namespace Rin.World.Mesh.Skinning;

// Same shape as SkeletalPose, but pooled: copies of this struct alias the same underlying buffers rather
// than owning their own, so disposing one copy invalidates every other - dispose exactly once, right after
// this pose is consumed (blended into a parent, or copied out via CopyTo).
public struct PooledSkeletalPose : IDisposable
{
    private PooledMemory<Transform> _transforms;
    private PooledMemory<ulong> _mask;
    private bool _disposed;

    public PooledSkeletalPose(int boneCount)
    {
        _transforms = new PooledMemory<Transform>(boneCount);
        // Rented memory is not zero-initialized, unlike `new ulong[n]`.
        _mask = new PooledMemory<ulong>((boneCount + 63) / 64).Clear();
    }

    public readonly Span<Transform> BoneTransforms => _transforms.AsSpan();

    public readonly Span<ulong> Mask => _mask.AsSpan();

    public readonly bool IsSet(int boneIndex)
    {
        return (Mask[boneIndex >> 6] & (1UL << (boneIndex & 63))) != 0;
    }

    public readonly void Set(int boneIndex, in Transform transform)
    {
        BoneTransforms[boneIndex] = transform;
        Mask[boneIndex >> 6] |= 1UL << (boneIndex & 63);
    }

    public static PooledSkeletalPose Blend(in PooledSkeletalPose a, in PooledSkeletalPose b, float alpha)
    {
        var aSpan = a.BoneTransforms;
        var bSpan = b.BoneTransforms;
        var result = new PooledSkeletalPose(aSpan.Length);

        for (var i = 0; i < aSpan.Length; i++)
        {
            var inA = a.IsSet(i);
            var inB = b.IsSet(i);

            if (inA && inB) result.Set(i, MathR.Interpolate(aSpan[i], bSpan[i], alpha));
            else if (inA) result.Set(i, aSpan[i]);
            else if (inB) result.Set(i, bSpan[i]);
        }

        return result;
    }

    public readonly void CopyTo(in SkeletalPose into)
    {
        BoneTransforms.CopyTo(into.BoneTransforms);
        Mask.CopyTo(into.Mask);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _transforms.Dispose();
        _mask.Dispose();
    }
}
