using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Engine.World.Mesh.Skinning;

public class Skeleton
{
    [PublicAPI]
    public readonly Bone[] Bones;
    [PublicAPI]
    public Bone Root;
    
    public Pose BasePose;
    
    public Skeleton(Bone[] bones,Pose basePose)
    {
        Bones = bones;
        Root = bones.FirstOrDefault(c => c.Name == "root")  ?? throw new NullReferenceException();
        BasePose = basePose;
    }

    public IEnumerable<Matrix4x4> ResolvePose(in Pose pose)
    {
        var transforms = pose.Transforms; // Trying to avoid a copy
        return Bones.Select(c =>
        {
            var local = c.LocalTransform.ToMatrix();
            if (transforms.TryGetValue(c.Name, out var transform))
            {
                return local * transform.ToMatrix();
            }
            return local * c.WorldTransform.ToMatrix();
        });
    }
}