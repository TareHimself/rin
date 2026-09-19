using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Rin.World.Tests.Skinning.Animation;

internal sealed class FakePoseNode(int boneCount, params (int Index, Transform Value)[] overrides) : IPoseNode
{
    public int EvaluateCount { get; private set; }
    public List<AnimationNotifyStateRange> ActiveStates { get; } = [];

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        EvaluateCount++;
        if (ctx.ActiveNotifyStates is { } active)
            foreach (var range in ActiveStates)
                active.Add(range);

        var pose = new PooledSkeletalPose(boneCount);
        foreach (var (index, value) in overrides) pose.Set(index, value);
        return pose;
    }
}

internal sealed class RecordingNotify(string name) : IAnimationNotify
{
    public string Name => name;
    public int ResetCount { get; private set; }
    public int NotifyCount { get; private set; }

    public void Reset()
    {
        ResetCount++;
    }

    public void Notify(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
        NotifyCount++;
    }
}

internal sealed class SingleInstanceNotifyFactory(IAnimationNotify instance) : IAnimationNotifyFactory
{
    public IAnimationNotify Create()
    {
        return instance;
    }

    public void Release(IAnimationNotify released)
    {
    }
}

internal sealed class RecordingNotifyState(string name) : IAnimationNotifyState
{
    public string Name => name;
    public int ResetCount { get; private set; }
    public int BeginCount { get; private set; }
    public int EndCount { get; private set; }

    public void Reset()
    {
        ResetCount++;
    }

    public void NotifyBegin(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
        BeginCount++;
    }

    public void NotifyEnd(SkinnedMeshComponent meshComponent, AnimationGraph graph)
    {
        EndCount++;
    }
}

internal sealed class SingleInstanceNotifyStateFactory(IAnimationNotifyState instance) : IAnimationNotifyStateFactory
{
    public int CreateCount { get; private set; }
    public int ReleaseCount { get; private set; }

    public IAnimationNotifyState Create()
    {
        CreateCount++;
        return instance;
    }

    public void Release(IAnimationNotifyState released)
    {
        ReleaseCount++;
    }
}

internal static class TestNotify
{
    public static (AnimationNotify Notify, RecordingNotify Handler) At(float time, string name)
    {
        var handler = new RecordingNotify(name);
        return (new AnimationNotify(time, new SingleInstanceNotifyFactory(handler)), handler);
    }

    public static (AnimationNotifyStateRange Range, RecordingNotifyState Handler, SingleInstanceNotifyStateFactory Factory) StateAt(
        float start, float end, string name)
    {
        var handler = new RecordingNotifyState(name);
        var factory = new SingleInstanceNotifyStateFactory(handler);
        return (new AnimationNotifyStateRange(start, end, factory), handler, factory);
    }
}
