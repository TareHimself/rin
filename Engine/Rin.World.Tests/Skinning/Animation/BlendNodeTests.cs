using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Skinning.Animation;

public class BlendNodeTests
{
    [Test]
    public void WeightZeroIsFullyA()
    {
        var a = new FakePoseNode(1, (0, new Transform { Position = new Vector3(1, 0, 0) }));
        var b = new FakePoseNode(1, (0, new Transform { Position = new Vector3(9, 0, 0) }));
        var node = new BlendNode(a, b) { Weight = 0f };

        using var pose = node.Evaluate(new AnimationEvalContext(0f));

        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void WeightOneIsFullyB()
    {
        var a = new FakePoseNode(1, (0, new Transform { Position = new Vector3(1, 0, 0) }));
        var b = new FakePoseNode(1, (0, new Transform { Position = new Vector3(9, 0, 0) }));
        var node = new BlendNode(a, b) { Weight = 1f };

        using var pose = node.Evaluate(new AnimationEvalContext(0f));

        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(9, 0, 0)));
    }

    [Test]
    public void WeightHalfInterpolates()
    {
        var a = new FakePoseNode(1, (0, new Transform { Position = new Vector3(0, 0, 0) }));
        var b = new FakePoseNode(1, (0, new Transform { Position = new Vector3(10, 0, 0) }));
        var node = new BlendNode(a, b) { Weight = 0.5f };

        using var pose = node.Evaluate(new AnimationEvalContext(0f));

        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(5, 0, 0)));
    }

    [Test]
    public void EvaluatesBothChildrenEveryTick()
    {
        var a = new FakePoseNode(1);
        var b = new FakePoseNode(1);
        var node = new BlendNode(a, b) { Weight = 0.5f };

        using (node.Evaluate(new AnimationEvalContext(0f)))
        {
        }

        Assert.That(a.EvaluateCount, Is.EqualTo(1));
        Assert.That(b.EvaluateCount, Is.EqualTo(1));
    }
}
