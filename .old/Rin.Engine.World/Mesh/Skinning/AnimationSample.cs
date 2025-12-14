using System.Collections.Frozen;

namespace Rin.Engine.World.Mesh.Skinning;

public class AnimationSample
{
    public Dictionary<string, BoneCurve> BoneCurves = [];

    public BoneCurve? this[string boneName]
    {
        get
        {
            {
                if (BoneCurves.TryGetValue(boneName, out var boneCurve)) return boneCurve;
            }

            return null;
        }
    }

    public BoneCurve GetOrCreate(string boneName)
    {
        {
            if (BoneCurves.TryGetValue(boneName, out var boneCurve)) return boneCurve;
        }

        var curve = new BoneCurve
        {
            BoneName = boneName
        };

        BoneCurves.Add(boneName, curve);

        return curve;
    }

    public Pose Evaluate(float time)
    {
        return new Pose
        {
            Transforms = BoneCurves.ToFrozenDictionary(c => c.Key, c => c.Value.Sample(time))
        };
    }
}