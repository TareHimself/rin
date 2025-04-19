using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

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

    protected Matrix4x4 ResolvePoseTransform(Bone bone,FrozenDictionary<string, Transform> pose,
        Dictionary<string, Matrix4x4> cache)
    {
        {
            if (cache.TryGetValue(bone.Name, out var transform))
            {
                return transform;
            }
        }

        {
            if (bone.Parent is null)
            {
                var local = bone.LocalTransform.ToMatrix();
                
                // Apply pose
                if (pose.TryGetValue(bone.Name, out var transform))
                {
                    return local * transform.ToMatrix();
                }
                
                cache.Add(bone.Name, local);
                return local;
            }
            else
            {
                var parentTransform = ResolvePoseTransform(bone.Parent,pose,cache);
                
                var local = bone.LocalTransform.ToMatrix();
                
                // Apply pose
                if (pose.TryGetValue(bone.Name, out var transform))
                {
                    return local * transform.ToMatrix();
                }
                
                var absolute = local * parentTransform;
                cache.Add(bone.Name, absolute);
                return absolute;
            }
        }
    }
    public IEnumerable<Matrix4x4> ResolvePose(in Pose pose)
    {
        var transforms = pose.Transforms; // Trying to avoid a copy
        var computedTransforms = new Dictionary<string, Matrix4x4>();
        return Bones.Select(bone => ResolvePoseTransform(bone, transforms, computedTransforms));
    }
}