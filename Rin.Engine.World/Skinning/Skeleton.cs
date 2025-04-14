using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Engine.World.Skinning;

public class Skeleton
{
    [PublicAPI]
    public readonly VertexBoneWeight[] Weights;
    [PublicAPI]
    public readonly Bone[] Bones;
    [PublicAPI]
    public Bone Root;

    /// <summary>
    /// First bone should be the root bone
    /// </summary>
    /// <param name="bones"></param>
    /// <param name="weights"></param>
    /// <exception cref="NullReferenceException"></exception>
    public Skeleton(Bone[] bones, VertexBoneWeight[] weights)
    {
        Bones = bones;
        Root = bones.FirstOrDefault()  ?? throw new NullReferenceException();
        Weights = weights;
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