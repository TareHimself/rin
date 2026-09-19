using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Skinning;

public class AnimationClipTests
{
    private static Skeleton BuildTwoBoneSkeleton()
    {
        var root = new Bone { Name = "root", LocalTransform = new Transform() };
        var tip = new Bone { Name = "tip", Parent = root, LocalTransform = new Transform() };
        root.Children = [tip];
        return new Skeleton([root, tip]);
    }

    [Test]
    public void BoundClipEvaluatesOnlyBonesItAnimatesByIndex()
    {
        var skeleton = BuildTwoBoneSkeleton();

        var clip = new AnimationClip();
        var rootCurve = clip.GetOrCreate("root");
        rootCurve.AddPositionLinear(0f, new Vector3(0, 0, 0));
        rootCurve.AddPositionLinear(1f, new Vector3(10, 0, 0));
        rootCurve.AddRotationLinear(0f, Quaternion.Identity);
        rootCurve.AddScaleLinear(0f, Vector3.One);

        var bound = clip.Bind(skeleton);
        using var pose = bound.Evaluate(0.5f);

        var rootIndex = skeleton.BoneNameToIndex["root"];
        var tipIndex = skeleton.BoneNameToIndex["tip"];

        Assert.That(pose.IsSet(rootIndex), Is.True);
        Assert.That(pose.BoneTransforms.Span[rootIndex].Position, Is.EqualTo(new Vector3(5, 0, 0)));
        Assert.That(pose.IsSet(tipIndex), Is.False, "clip never authored a curve for 'tip'");
    }
}
