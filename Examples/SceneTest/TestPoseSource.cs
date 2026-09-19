using System.Numerics;
using Rin.World.Mesh.Skinning;
using Rin.Core;
using Rin.Core.Shared.Math;

namespace SceneTest;

public class TestPoseSource : IPoseSource
{
    public required Skeleton Skeleton { get; init; }

    public SkeletalPose GetPose()
    {
        var rot = Quaternion.Identity.AddYaw(IApplication.Get().TimeSeconds * 20f);
        var pose = new SkeletalPose(Skeleton.Bones.Length);
        if (Skeleton.BoneNameToIndex.TryGetValue("root", out var rootIndex))
            pose.Set(rootIndex, new Transform { Orientation = rot });
        return pose;
    }
}