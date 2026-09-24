namespace Rin.World.Mesh.Skinning;

public class AnimationClip
{
    public Dictionary<string, BoneCurve> BoneCurves = [];
    public float Duration;
    public List<AnimationNotify> Notifies = [];
    public List<AnimationNotifyStateRange> NotifyStates = [];

    public BoneCurve? this[string boneName] => BoneCurves.GetValueOrDefault(boneName);

    public BoneCurve GetOrCreate(string boneName)
    {
        if (BoneCurves.TryGetValue(boneName, out var boneCurve)) return boneCurve;

        var curve = new BoneCurve
        {
            BoneName = boneName
        };

        BoneCurves.Add(boneName, curve);

        return curve;
    }

    public BoundAnimationClip Bind(Skeleton skeleton)
    {
        var curvesByIndex = new BoneCurve?[skeleton.Bones.Length];
        foreach (var (name, curve) in BoneCurves)
            if (skeleton.BoneNameToIndex.TryGetValue(name, out var index))
                curvesByIndex[index] = curve;

        return new BoundAnimationClip(curvesByIndex, Duration, Notifies, NotifyStates, skeleton);
    }
}
