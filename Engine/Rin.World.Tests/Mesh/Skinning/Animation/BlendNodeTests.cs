using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Mesh.Skinning.Animation;

public class BlendNodeTests
{
    [TestCase(0f, 0f)]
    [TestCase(0.5f, 5f)]
    [TestCase(1f, 10f)]
    public void WeightBlendsFromAToB(float weight, float expectedX)
    {
        var a = new FakePoseNode(1, (0, new Transform { Position = new Vector3(0, 0, 0) }));
        var b = new FakePoseNode(1, (0, new Transform { Position = new Vector3(10, 0, 0) }));
        var node = new BlendNode(a, b) { Weight = weight };

        using var pose = node.Evaluate(new AnimationEvalContext(0f));

        Assert.That(pose.BoneTransforms[0].Position, Is.EqualTo(new Vector3(expectedX, 0, 0)));
    }

    [Test]
    public void DominantClipOnlyNotifiesFireOnlyFromTheHeavierChild()
    {
        var (walkStep, _) = TestNotify.At(0f, "WalkStep", dominantClipOnly: true);
        var (runStep, _) = TestNotify.At(0f, "RunStep", dominantClipOnly: true);
        var walk = new FakePoseNode(1);
        walk.CrossedNotifies.Add(walkStep);
        var run = new FakePoseNode(1);
        run.CrossedNotifies.Add(runStep);
        var node = new BlendNode(walk, run) { Weight = 0.7f };
        var sink = new NotifySink();

        using (node.Evaluate(new AnimationEvalContext(0f, sink)))
        {
        }

        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);
        Assert.That(fired, Is.EqualTo(new[] { runStep }));
    }

    [Test]
    public void DominanceCountsChildrenThatCrossedNothing()
    {
        var (walkStep, _) = TestNotify.At(0f, "WalkStep", dominantClipOnly: true);
        var walk = new FakePoseNode(1);
        walk.CrossedNotifies.Add(walkStep);
        var run = new FakePoseNode(1);
        var node = new BlendNode(walk, run) { Weight = 0.7f };
        var sink = new NotifySink();

        using (node.Evaluate(new AnimationEvalContext(0f, sink)))
        {
        }

        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);
        Assert.That(fired, Is.Empty, "run outweighs walk even though it crossed no notify this tick");
    }

    [Test]
    public void UnfilteredNotifiesFireFromEveryChildAtAnyWeight()
    {
        var (walkDust, _) = TestNotify.At(0f, "WalkDust");
        var (runDust, _) = TestNotify.At(0f, "RunDust");
        var walk = new FakePoseNode(1);
        walk.CrossedNotifies.Add(walkDust);
        var run = new FakePoseNode(1);
        run.CrossedNotifies.Add(runDust);
        var node = new BlendNode(walk, run) { Weight = 0.95f };
        var sink = new NotifySink();

        using (node.Evaluate(new AnimationEvalContext(0f, sink)))
        {
        }

        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);
        Assert.That(fired, Is.EqualTo(new[] { walkDust, runDust }));
    }
}
