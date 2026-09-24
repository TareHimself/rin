using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Skinning.Animation;

public class AnimationGraphTests
{
    [Test]
    public void GetPoseBeforeAnyTickReturnsSkeletonBasePose()
    {
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var graph = new AnimationGraph(skeleton);

        var pose = graph.GetPose();

        Assert.That(pose.IsSet(0), Is.False);
    }

    [Test]
    public void TickEvaluatesTheRootNodeAndCachesAPlainPose()
    {
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var graph = new AnimationGraph(skeleton)
        {
            Root = new FakePoseNode(1, (0, new Transform { Position = new Vector3(3, 4, 5) }))
        };

        graph.Tick(1f / 60f);
        var pose = graph.GetPose();

        Assert.That(pose.IsSet(0), Is.True);
        Assert.That(pose.BoneTransforms[0].Position, Is.EqualTo(new Vector3(3, 4, 5)));
    }

    [Test]
    public void FiresBeginOnceWhenAStateRangeBecomesActive()
    {
        var (range, handler, factory) = TestNotify.StateAt(0f, 10f, "Invulnerable");
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var node = new FakePoseNode(1);
        node.ActiveStates.Add(range);
        var graph = new AnimationGraph(skeleton) { Root = node };

        graph.Tick(1f / 60f);

        Assert.That(graph.BegunNotifyStates, Is.EqualTo(new IAnimationNotifyState[] { handler }));
        Assert.That(factory.CreateCount, Is.EqualTo(1));

        graph.Tick(1f / 60f);

        Assert.That(graph.BegunNotifyStates, Is.Empty, "already active - shouldn't begin again");
        Assert.That(factory.CreateCount, Is.EqualTo(1));
    }

    [Test]
    public void FiresEndWhenTheStateRangeStopsBeingActive()
    {
        var (range, handler, factory) = TestNotify.StateAt(0f, 10f, "Invulnerable");
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var node = new FakePoseNode(1);
        node.ActiveStates.Add(range);
        var graph = new AnimationGraph(skeleton) { Root = node };

        graph.Tick(1f / 60f);
        node.ActiveStates.Clear();
        graph.Tick(1f / 60f);

        Assert.That(graph.EndedNotifyStates, Is.EqualTo(new IAnimationNotifyState[] { handler }));
        Assert.That(factory.ReleaseCount, Is.Zero, "must stay alive until NotifyEnd has been dispatched");

        graph.ReleaseNotifies();

        Assert.That(factory.ReleaseCount, Is.EqualTo(1));
        Assert.That(graph.EndedNotifyStates, Is.Empty);
    }

    [Test]
    public void FiredNotifiesAreReleasedOnlyByReleaseNotifies()
    {
        var (notify, handler) = TestNotify.At(0.5f, "Footstep");
        var factory = (SingleInstanceNotifyFactory)notify.Factory;
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var node = new FakePoseNode(1);
        node.CrossedNotifies.Add(notify);
        var graph = new AnimationGraph(skeleton) { Root = node };

        graph.Tick(1f / 60f);

        Assert.That(graph.FiredNotifies, Is.EqualTo(new IAnimationNotify[] { handler }));
        Assert.That(factory.ReleaseCount, Is.Zero);

        graph.ReleaseNotifies();

        Assert.That(factory.ReleaseCount, Is.EqualTo(1));
        Assert.That(graph.FiredNotifies, Is.Empty);
    }

    [Test]
    public void ActiveStatesAreNotReleasedByReleaseNotifies()
    {
        var (range, _, factory) = TestNotify.StateAt(0f, 10f, "Invulnerable");
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var node = new FakePoseNode(1);
        node.ActiveStates.Add(range);
        var graph = new AnimationGraph(skeleton) { Root = node };

        graph.Tick(1f / 60f);
        graph.ReleaseNotifies();

        Assert.That(factory.ReleaseCount, Is.Zero);
    }

    [Test]
    public void FiresEndOnInterruption()
    {
        // Simulates a state-machine transition swapping branches mid-window - the graph doesn't
        // know or care why a key stopped being active, so this still fires End exactly once.
        var (range, handler, factory) = TestNotify.StateAt(0f, 1000f, "Invulnerable");
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var nodeA = new FakePoseNode(1);
        nodeA.ActiveStates.Add(range);
        var nodeB = new FakePoseNode(1);
        var graph = new AnimationGraph(skeleton) { Root = nodeA };

        graph.Tick(1f / 60f);
        Assert.That(graph.BegunNotifyStates, Has.Count.EqualTo(1));

        graph.Root = nodeB;
        graph.Tick(1f / 60f);

        Assert.That(graph.EndedNotifyStates, Is.EqualTo(new IAnimationNotifyState[] { handler }));

        graph.ReleaseNotifies();

        Assert.That(factory.ReleaseCount, Is.EqualTo(1));
    }
}
