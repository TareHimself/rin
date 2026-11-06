using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class MeshDepthShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<DepthSceneInfo> Scene;
        public BufferRef<MeshInstance> Instances;
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
        var instance = Push.Instances[input.InstanceId];
        var scene = Push.Scene[0];
        var vertex = instance.Vertices[input.VertexId];
        var sceneLocation = Vector4.Transform(new Vector4(vertex.Location, 1f), instance.Transform);
        var viewLocation = Vector4.Transform(sceneLocation, scene.View);

        VertexOut output;
        output.Position = Vector4.Transform(viewLocation, scene.Projection);
        return output;
    }
}
