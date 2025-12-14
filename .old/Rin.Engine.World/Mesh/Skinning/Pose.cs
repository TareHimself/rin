using System.Collections.Frozen;
using System.Collections.Immutable;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Mesh.Skinning;

public struct Pose
{
    public FrozenDictionary<string, Transform> Transforms;

    public Pose()
    {
        Transforms = FrozenDictionary<string, Transform>.Empty;
    }

    public Pose(FrozenDictionary<string, Transform> transforms)
    {
        Transforms = transforms;
    }

    public static Pose Blend(Pose a, Pose b, float alpha)
    {
        var allKeys = a.Transforms.Keys.ToImmutableHashSet().Union(b.Transforms.Keys.ToImmutableHashSet());
        return new Pose
        {
            Transforms = allKeys.ToFrozenDictionary(k => k, k =>
            {
                var inA = a.Transforms.ContainsKey(k);
                var inB = b.Transforms.ContainsKey(k);
                if (inA && inB) return MathR.Interpolate(a.Transforms[k], b.Transforms[k], alpha);

                if (inA) return a.Transforms[k];

                if (inB) return b.Transforms[k];

                throw new NotSupportedException();
            })
        };
    }
}