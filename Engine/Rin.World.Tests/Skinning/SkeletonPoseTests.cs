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

    [Test]
    public void SkinningMatricesAreIdentityAtBindPose()
    {
        var skeleton = BuildChain();
        foreach (var bone in skeleton.Bones)
        {
            Matrix4x4.Invert(bone.WorldTransform.ToMatrix(), out var inverseBind);
            bone.Bind = inverseBind;
        }

        var global = skeleton.ResolvePose(new SkeletalPose(skeleton.Bones.Length));
        var skinning = new Matrix4x4[skeleton.Bones.Length];
        skeleton.ComputeSkinningMatrices(global, skinning);

        foreach (var matrix in skinning) Assert.That(matrix.IsIdentity, Is.True);
    }

    [Test]
    public void SkinningMovesAVertexWithItsBoneOffTheBindPose()
    {
        // Bind pose alone can't catch a flipped multiply - both orders are identity there - so this
        // rotates the root and checks where a vertex bound to "mid" actually lands.
        var skeleton = BuildChain();
        foreach (var bone in skeleton.Bones)
        {
            Matrix4x4.Invert(bone.WorldTransform.ToMatrix(), out var inverseBind);
            bone.Bind = inverseBind;
        }

        var pose = new SkeletalPose(skeleton.Bones.Length);
        pose.Set(skeleton.BoneNameToIndex["root"],
            new Transform { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) });
        var global = skeleton.ResolvePose(pose);
        var skinning = new Matrix4x4[skeleton.Bones.Length];
        skeleton.ComputeSkinningMatrices(global, skinning);

        var skinned = Vector3.Transform(new Vector3(1.5f, 0, 0), skinning[skeleton.BoneNameToIndex["mid"]]);

        Assert.That(Vector3.Distance(skinned, new Vector3(0, 1.5f, 0)), Is.LessThan(1e-4f),
            $"a vertex half a unit down mid should swing to (0, 1.5, 0), got {skinned}");
    }
}
