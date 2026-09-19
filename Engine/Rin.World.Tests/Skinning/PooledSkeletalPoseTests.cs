using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Skinning;

public class PooledSkeletalPoseTests
{
    [Test]
    public void BlendPrefersWhicheverSideHasTheBoneSet()
    {
        using var a = new PooledSkeletalPose(2);
        a.Set(0, new Transform { Position = new Vector3(1, 0, 0) });

        using var b = new PooledSkeletalPose(2);
        b.Set(1, new Transform { Position = new Vector3(0, 2, 0) });

        using var blended = PooledSkeletalPose.Blend(a, b, 0.5f);

        Assert.That(blended.IsSet(0), Is.True);
        Assert.That(blended.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(1, 0, 0)));
        Assert.That(blended.IsSet(1), Is.True);
        Assert.That(blended.BoneTransforms.Span[1].Position, Is.EqualTo(new Vector3(0, 2, 0)));
    }

    [Test]
    public void BlendInterpolatesWhenBothSidesSetTheBone()
    {
        using var a = new PooledSkeletalPose(1);
        a.Set(0, new Transform { Position = new Vector3(2, 0, 0) });

        using var b = new PooledSkeletalPose(1);
        b.Set(0, new Transform { Position = new Vector3(4, 0, 0) });

        using var blended = PooledSkeletalPose.Blend(a, b, 0.5f);

        Assert.That(blended.IsSet(0), Is.True);
        Assert.That(blended.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(3, 0, 0)));
    }

    [Test]
    public void BlendLeavesBoneUnsetWhenNeitherSideSetsIt()
    {
        using var a = new PooledSkeletalPose(1);
        using var b = new PooledSkeletalPose(1);

        using var blended = PooledSkeletalPose.Blend(a, b, 0.5f);

        Assert.That(blended.IsSet(0), Is.False);
    }

    [Test]
    public void CopiesDataIntoAPlainPose()
    {
        using var pooled = new PooledSkeletalPose(1);
        pooled.Set(0, new Transform { Position = new Vector3(7, 0, 0) });

        var plain = pooled.ToSkeletalPose();

        Assert.That(plain.IsSet(0), Is.True);
        Assert.That(plain.BoneTransforms[0].Position, Is.EqualTo(new Vector3(7, 0, 0)));
    }
}
