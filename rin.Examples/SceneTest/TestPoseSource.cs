using System.Collections.Frozen;
using System.Numerics;
using Rin.Framework;
using Rin.Framework.Math;
using Rin.Engine.World.Mesh.Skinning;

namespace rin.Examples.SceneTest;

public class TestPoseSource : IPoseSource
{
    public required Skeleton Skeleton { get; init; }

    public Pose GetPose()
    {
        var rot = Quaternion.Identity.AddYaw(SApplication.Get().GetTimeSeconds() * 20f);
        return new Pose(new Dictionary<string, Transform>
        {
            {
                "root", new Transform
                {
                    Orientation = rot
                }
            }
        }.ToFrozenDictionary());
    }
}