using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Skinning;

public class SkeletonPoseTests
{
    private static Skeleton BuildChain()
    {
        var root = new Bone { Name = "root", LocalTransform = new Transform() };
        var mid = new Bone
        {
            Name = "mid", Parent = root, LocalTransform = new Transform { Position = new Vector3(1, 0, 0) }
        };
        var tip = new Bone
        {
            Name = "tip", Parent = mid, LocalTransform = new Transform { Position = new Vector3(1, 0, 0) }
        };
        root.Children = [mid];
        mid.Children = [tip];

        return new Skeleton([root, mid, tip]);
    }

    [Test]
    public void ResolvePoseWithNoOverridesMatchesBindChain()
    {
        var skeleton = BuildChain();

        var pose = new SkeletalPose(skeleton.Bones.Length);
        var matrices = skeleton.ResolvePose(pose);

        Assert.That(matrices[0].Translation, Is.EqualTo(new Vector3(0, 0, 0)));
        Assert.That(matrices[1].Translation, Is.EqualTo(new Vector3(1, 0, 0)));
        Assert.That(matrices[2].Translation, Is.EqualTo(new Vector3(2, 0, 0)));
    }

    [Test]
    public void OverrideStillComposesWithParentAndChildren()
    {
        var skeleton = BuildChain();
        var midIndex = skeleton.BoneNameToIndex["mid"];

        var pose = new SkeletalPose(skeleton.Bones.Length);
        pose.Set(midIndex, new Transform { Position = new Vector3(5, 0, 0) });

        var matrices = skeleton.ResolvePose(pose);

        Assert.That(matrices[0].Translation, Is.EqualTo(new Vector3(0, 0, 0)), "root has no override, stays at bind");
        Assert.That(matrices[1].Translation, Is.EqualTo(new Vector3(5, 0, 0)), "overridden bone uses the pose transform");
        Assert.That(matrices[2].Translation, Is.EqualTo(new Vector3(6, 0, 0)),
            "un-overridden child still composes with its parent's new (overridden) transform");
    }
}
