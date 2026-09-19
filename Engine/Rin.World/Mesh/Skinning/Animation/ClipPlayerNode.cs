namespace Rin.World.Mesh.Skinning.Animation;

public class ClipPlayerNode(BoundAnimationClip clip) : IPoseNode
{
    public float Time;
    public float Rate = 1f;
    public bool Loop = true;

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        var previousTime = Time;
        var rawTime = Time + ctx.DeltaSeconds * Rate;

        if (Loop && clip.Duration > 0f && rawTime > clip.Duration)
        {
            ReportPointsInRange(ctx, previousTime, clip.Duration);
            ReportPointsInRange(ctx, 0f, rawTime - clip.Duration);
        }
        else
        {
            ReportPointsInRange(ctx, previousTime, rawTime);
        }

        Time = Loop && clip.Duration > 0f ? rawTime % clip.Duration : rawTime;
        ReportActiveStates(ctx, Time);

        return clip.Evaluate(Time);
    }

    private void ReportPointsInRange(in AnimationEvalContext ctx, float start, float end)
    {
        if (ctx.FiredNotifies is not { } sink) return;

        foreach (var notify in clip.Notifies)
            if (notify.Time >= start && notify.Time < end)
                sink.Add(notify.Factory.Create());
    }

    private void ReportActiveStates(in AnimationEvalContext ctx, float time)
    {
        if (ctx.ActiveNotifyStates is not { } active) return;

        foreach (var range in clip.NotifyStates)
            if (time >= range.StartTime && time < range.EndTime)
                active.Add(range);
    }
}
