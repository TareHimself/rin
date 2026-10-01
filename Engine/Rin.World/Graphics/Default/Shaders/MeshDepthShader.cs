using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Shaders;

[NoReorder]
public struct DepthMaterialData
{
    public Matrix4x4 Transform;
    public BufferRef<Vertex> Vertices;
}

[Shader("Shaders/Rin/World/Mesh/mesh_depth.slang")]
public partial class MeshDepthShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<DepthSceneInfo> Scene;
        public BufferRef<DepthMaterialData> Data;
    }

    public struct VertexIn
    {
        [Semantic("SV_VulkanVertexID")] public int VertexId;
        [Semantic("SV_VulkanInstanceID")] public int InstanceId;
    }

    public struct VertexOut
    {
        [Position] public Vector4 Position;
    }

    [Push] protected PushConstants Push;

    [Vertex, Depth]
    public VertexOut Vertex(VertexIn input)
    {
        var instance = Push.Data[input.InstanceId];
        var scene = Push.Scene[0];
        var vertex = instance.Vertices[input.VertexId];
        var sceneLocation = Vector4.Transform(new Vector4(vertex.Location, 1f), instance.Transform);
        var viewLocation = Vector4.Transform(sceneLocation, scene.View);

        VertexOut output;
        output.Position = Vector4.Transform(viewLocation, scene.Projection);
        return output;
    }
}
