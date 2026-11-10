using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class MeshDepthShader : Shader
{
    public struct PushConstants
    {
        public DeviceSpan<DepthSceneInfo> Scene;
        public DeviceSpan<MeshInstance> Instances;
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
        var scene = Push.Scene[0];
        var world = SurfaceVertices.Load(Push.Instances[input.InstanceId], input.VertexId);

        VertexOut output;
        output.Position = SurfaceVertices.Project(world.Position, scene.View, scene.Projection);
        return output;
    }
}
