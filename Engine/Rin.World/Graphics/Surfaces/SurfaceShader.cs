using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Surfaces;

public struct SurfaceVertexIn
{
    [Semantic("SV_VulkanVertexID")] public int VertexId;
    [Semantic("SV_VulkanInstanceID")] public int InstanceId;
}

public struct SurfaceVertexOut
{
    [Semantic("UV")] public Vector2 Uv;
    [Semantic("WORLD_POSITION")] public Vector3 WorldPosition;
    [Semantic("WORLD_NORMAL")] public Vector3 WorldNormal;
    [Position] public Vector4 Position;
    [Semantic("INSTANCE_ID")] public int InstanceId;
}

public struct SurfaceFragmentIn
{
    [Semantic("UV")] public Vector2 Uv;
    [Semantic("WORLD_POSITION")] public Vector3 WorldPosition;
    [Semantic("WORLD_NORMAL")] public Vector3 WorldNormal;
    [Semantic("INSTANCE_ID")] public int InstanceId;
}

/// <summary>
///     The engine half of every surface material's shader: vertex transform and G-buffer packing around
///     <see cref="ISurfaceMaterial{TSelf}.Evaluate" />. A material's shader is
///     <c>[Shader] public partial class GoldShader : SurfaceShader&lt;Gold&gt;;</c>.
/// </summary>
[ShadeExport]
public abstract class SurfaceShader<TMaterial> : Shader where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    public struct PushConstants
    {
        public BufferRef<WorldInfo> Scene;
        public BufferRef<SurfaceDraw<TMaterial>> Draws;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Opaque;

    [Vertex]
    public SurfaceVertexOut Vertex(SurfaceVertexIn input)
    {
        var draw = Push.Draws[input.InstanceId];
        var scene = Push.Scene[0];
        var world = TMaterial.ProcessVertex(draw.Material, SurfaceVertices.Load(draw.Instance, input.VertexId),
            scene.Time);

        SurfaceVertexOut output;
        output.Uv = world.Uv;
        output.WorldPosition = world.Position;
        output.WorldNormal = world.Normal;
        output.Position = SurfaceVertices.Project(world.Position, scene.View, scene.Projection);
        output.InstanceId = input.InstanceId;
        return output;
    }

    [Fragment, Depth]
    public GBufferOutput Fragment(SurfaceFragmentIn input)
    {
        SurfaceInput surfaceInput;
        surfaceInput.Uv = input.Uv;
        surfaceInput.WorldPosition = input.WorldPosition;
        surfaceInput.WorldNormal = Normalize(input.WorldNormal);

        var surface = TMaterial.Evaluate(Push.Draws[input.InstanceId].Material, surfaceInput, Bindless);

        GBufferOutput output;
        output.GBuffer0 = new Vector4(surface.BaseColor, surface.Roughness);
        output.GBuffer1 = new Vector4(input.WorldPosition, surface.Metallic);
        output.GBuffer2 = new Vector4(surface.Normal, surface.Specular);
        output.GBuffer3 = new Vector4(surface.Emissive, 0f, 0f, 0f);
        return output;
    }
}
