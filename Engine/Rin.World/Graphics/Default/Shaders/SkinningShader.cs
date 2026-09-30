using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics.Default.Shaders;

[Shader("Shaders/Rin/World/Mesh/Compute/skinning.slang")]
public partial class SkinningShader : Shader
{
    public struct PushConstants
    {
        public int TotalInvocations;
        public BufferRef<BufferRef<SkinnedVertex>> Meshes;
        public BufferRef<BufferRef<Matrix4x4>> Poses;
        public BufferRef<SkinningExecutionInfo> ExecutionInfo;
        public BufferRef<Vertex> Output;
    }

    [Push] [UsedImplicitly] protected PushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var index = input.ThreadId;
        if (index >= (uint)Push.TotalInvocations) return;

        var info = Push.ExecutionInfo[index];
        var mesh = Push.Meshes[info.MeshIndex];
        var inVertex = mesh[info.VertexIndex];
        var weights = inVertex.BoneWeights;
        var pose = Push.Poses[info.PoseIndex];
        var boneIds = inVertex.BoneIndices;

        var t1 = pose[boneIds.X];
        var t2 = pose[boneIds.Y];
        var t3 = pose[boneIds.Z];
        var t4 = pose[boneIds.W];

        var location4 = new Vector4(inVertex.Vertex.Location, 1f);
        var normal = inVertex.Vertex.Normal;

        var location = Vector4.Transform(location4, t1) * weights.X +
                       Vector4.Transform(location4, t2) * weights.Y +
                       Vector4.Transform(location4, t3) * weights.Z +
                       Vector4.Transform(location4, t4) * weights.W;

        normal = ShaderMath.TransformNormal(normal, t1) * weights.X +
                 ShaderMath.TransformNormal(normal, t2) * weights.Y +
                 ShaderMath.TransformNormal(normal, t3) * weights.Z +
                 ShaderMath.TransformNormal(normal, t4) * weights.W;
        normal = Shader.Math.Normalize(normal);

        var result = inVertex.Vertex;
        result.Location = location.xyz;
        result.Normal = normal;
        Push.Output[index] = result;
    }
}
