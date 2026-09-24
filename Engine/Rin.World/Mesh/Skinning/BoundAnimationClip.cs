namespace Rin.World.Mesh.Skinning;

public class BoundAnimationClip(BoneCurve?[] curvesByIndex, float duration, IReadOnlyList<AnimationNotify> notifies,
    IReadOnlyList<AnimationNotifyStateRange> notifyStates, Skeleton skeleton)
{
    public float Duration => duration;
    public IReadOnlyList<AnimationNotify> Notifies => notifies;
    public IReadOnlyList<AnimationNotifyStateRange> NotifyStates => notifyStates;

    public PooledSkeletalPose Evaluate(float time)
    {
        var pose = new PooledSkeletalPose(curvesByIndex.Length);
        for (var i = 0; i < curvesByIndex.Length; i++)
            if (curvesByIndex[i] is { } curve)
                pose.Set(i, curve.Sample(skeleton.Bones[i].LocalTransform, time));

        return pose;
    }
}
