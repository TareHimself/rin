using System.Numerics;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Skinning.Animation;

public class ClipPlayerNodeTests
{
    private static BoundAnimationClip BuildOneBoneClip(float duration, params AnimationNotify[] notifies)
    {
        var root = new Bone { Name = "root" };
        var skeleton = new Skeleton([root]);

        var clip = new AnimationClip { Duration = duration };
        clip.Notifies.AddRange(notifies);
        var curve = clip.GetOrCreate("root");
        curve.AddPositionLinear(0f, new Vector3(0, 0, 0));
        curve.AddPositionLinear(duration, new Vector3(10, 0, 0));
        curve.AddRotationLinear(0f, Quaternion.Identity);
        curve.AddScaleLinear(0f, Vector3.One);

        return clip.Bind(skeleton);
    }

    [Test]
    public void EvaluateAdvancesTimeByDeltaSecondsTimesRate()
    {
        var node = new ClipPlayerNode(BuildOneBoneClip(2f)) { Rate = 2f, Loop = false };

        using var pose = node.Evaluate(new AnimationEvalContext(0.5f));

        Assert.That(node.Time, Is.EqualTo(1f).Within(1e-4f));
        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(5, 0, 0)));
    }

    [Test]
    public void LoopingWrapsTimeAtDuration()
    {
        var node = new ClipPlayerNode(BuildOneBoneClip(2f)) { Rate = 1f, Loop = true, Time = 1.5f };

        using var pose = node.Evaluate(new AnimationEvalContext(1f));

        Assert.That(node.Time, Is.EqualTo(0.5f).Within(1e-4f),
            "1.5 + 1 = 2.5, which wraps to 0.5 for a 2-second clip");
    }

    [Test]
    public void FiresANotifyCrossedDuringTheTick()
    {
        var (notify, _) = TestNotify.At(0.75f, "Footstep");
        var node = new ClipPlayerNode(BuildOneBoneClip(2f, notify)) { Rate = 1f, Loop = false, Time = 0.5f };
        var sink = new NotifySink();

        using var pose = node.Evaluate(new AnimationEvalContext(0.5f, sink));
        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);

        Assert.That(fired, Is.EqualTo(new[] { notify }));
    }

    [Test]
    public void DoesNotFireANotifyOutsideTheTicksInterval()
    {
        var (notify, _) = TestNotify.At(1.5f, "Footstep");
        var node = new ClipPlayerNode(BuildOneBoneClip(2f, notify)) { Rate = 1f, Loop = false, Time = 0.5f };
        var sink = new NotifySink();

        using var pose = node.Evaluate(new AnimationEvalContext(0.5f, sink));
        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);

        Assert.That(fired, Is.Empty);
    }

    [Test]
    public void DoesNothingWhenNoSinkIsProvided()
    {
        var (notify, _) = TestNotify.At(0.75f, "Footstep");
        var node = new ClipPlayerNode(BuildOneBoneClip(2f, notify)) { Rate = 1f, Loop = false, Time = 0.5f };

        Assert.That(() =>
        {
            using var pose = node.Evaluate(new AnimationEvalContext(0.5f));
        }, Throws.Nothing);
    }

    [Test]
    public void FiresNotifiesOnBothSidesOfALoopWrap()
    {
        var (nearEnd, _) = TestNotify.At(1.9f, "NearEnd");
        var (nearStart, _) = TestNotify.At(0.1f, "NearStart");
        var node = new ClipPlayerNode(BuildOneBoneClip(2f, nearEnd, nearStart)) { Rate = 1f, Loop = true, Time = 1.8f };
        var sink = new NotifySink();

        using var pose = node.Evaluate(new AnimationEvalContext(0.5f, sink));
        var fired = new List<AnimationNotify>();
        sink.CollectFired(fired);

        Assert.That(fired, Is.EqualTo(new[] { nearEnd, nearStart }),
            "1.8 + 0.5 = 2.3, which wraps once for a 2-second clip - both notifies should fire in order");
    }
}
