namespace Rin.World.Mesh.Skinning.Animation;

public sealed class NotifySink
{
    private readonly List<(AnimationNotify Notify, object Source)> _crossed = [];
    private object? _dominantSource;
    private float _dominantWeight = float.NegativeInfinity;

    public void ReportSource(object source, float weight)
    {
        if (weight <= _dominantWeight) return;
        _dominantSource = source;
        _dominantWeight = weight;
    }

    public void ReportCrossed(AnimationNotify notify, object source)
    {
        _crossed.Add((notify, source));
    }

    public void CollectFired(List<AnimationNotify> into)
    {
        foreach (var (notify, source) in _crossed)
            if (!notify.DominantClipOnly || ReferenceEquals(source, _dominantSource))
                into.Add(notify);
    }

    public void Clear()
    {
        _crossed.Clear();
        _dominantSource = null;
        _dominantWeight = float.NegativeInfinity;
    }
}
