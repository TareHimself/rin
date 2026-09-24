using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Skinning.Animation;

public class StateMachineNodeTests
{
    private static (StateMachineNode Node, FakePoseNode A, FakePoseNode B, Action Trigger) Build(float blendDuration)
    {
        var a = new FakePoseNode(1, (0, new Transform { Position = new Vector3(0, 0, 0) }));
        var b = new FakePoseNode(1, (0, new Transform { Position = new Vector3(10, 0, 0) }));

        var shouldTransition = false;

        var stateA = new AnimationState { Name = "A", Node = a };
        var stateB = new AnimationState { Name = "B", Node = b };
        stateA.Transitions.Add(new AnimationTransition
        {
            ToState = "B", Condition = () => shouldTransition, BlendDuration = blendDuration
        });

        var states = new Dictionary<string, AnimationState> { ["A"] = stateA, ["B"] = stateB };
        var node = new StateMachineNode(states, "A");

        return (node, a, b, () => shouldTransition = true);
    }

    [Test]
    public void StaysOnInitialStateUntilConditionIsMet()
    {
        var (node, _, _, _) = Build(0.5f);

        using var pose = node.Evaluate(new AnimationEvalContext(0.1f));

        Assert.That(node.CurrentStateName, Is.EqualTo("A"));
        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(0, 0, 0)));
    }

    [Test]
    public void BlendsTowardTargetStateDuringTheTransitionWindow()
    {
        var (node, _, _, trigger) = Build(1f);
        trigger();

        using var pose = node.Evaluate(new AnimationEvalContext(0.5f));

        Assert.That(node.CurrentStateName, Is.EqualTo("A"), "still mid-transition, hasn't fully switched yet");
        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(5, 0, 0)));
    }

    [Test]
    public void FullySwitchesOnceBlendDurationElapses()
    {
        var (node, _, _, trigger) = Build(1f);
        trigger();

        using (node.Evaluate(new AnimationEvalContext(1f)))
        {
        }

        Assert.That(node.CurrentStateName, Is.EqualTo("B"));

        using var pose = node.Evaluate(new AnimationEvalContext(0f));
        Assert.That(pose.BoneTransforms.Span[0].Position, Is.EqualTo(new Vector3(10, 0, 0)));
    }
}
