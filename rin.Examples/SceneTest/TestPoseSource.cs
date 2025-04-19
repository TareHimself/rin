using System.Collections.Frozen;
using System.Numerics;
using Rin.Engine;
using Rin.Engine.Math;
using Rin.Engine.World.Mesh.Skinning;

namespace rin.Examples.SceneTest;

public class TestPoseSource() : IPoseSource
{
    public required Skeleton Skeleton { get; init; }
    public Pose GetPose()
    {
        return new Pose(new Dictionary<string,Transform>()
        {
            {"root",new Transform()
            {
                Scale = new Vector3(((float.Sin(SEngine.Get().GetTimeSeconds()) + 1f) * 0.5f * 4f) + 1f)
            }}
        }.ToFrozenDictionary());
    }
}