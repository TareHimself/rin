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
    [Position] public Vector4 Position;
    [Semantic("UV")] public Vector2 Uv;
    [Semantic("WORLD_POSITION")] public Vector3 WorldPosition;
    [Semantic("WORLD_NORMAL")] public Vector3 WorldNormal;
    [Semantic("INSTANCE_ID")] public int InstanceId;
}

/// <summary>
///     The engine half of every surface material's shader: vertex transform and clustered forward lighting around
///     <see cref="ISurfaceMaterial{TSelf}.Evaluate" />. A material's shader is
///     <c>[Shader] public partial class GoldShader : SurfaceShader&lt;Gold&gt;;</c>.
/// </summary>
[ShadeExport]
public abstract class SurfaceShader<TMaterial> : Shader where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    public struct PushConstants
    {
        public DeviceSpan<WorldInfo> Scene;
        public DeviceSpan<SurfaceDraw<TMaterial>> Draws;
    }

    [Push] protected PushConstants Push;

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

    [Fragment, Depth, Attachment(AttachmentFormat.RGBA16)]
    public Vector4 Fragment(SurfaceFragmentIn input)
    {
        var scene = Push.Scene[0];
        SurfaceInput surfaceInput;
        surfaceInput.Uv = input.Uv;
        surfaceInput.WorldPosition = input.WorldPosition;
        surfaceInput.WorldNormal = Normalize(input.WorldNormal);

        var surface = TMaterial.Evaluate(Push.Draws[input.InstanceId].Material, surfaceInput);
        var lighting = LightGrid.Light(surface, input.WorldPosition, scene.CameraPosition,
            new Vector2(input.Position.X, input.Position.Y), scene.Grid[0]);

        return scene.Channel switch
        {
            RenderChannel.Color => new Vector4(surface.BaseColor, 1f),
            RenderChannel.Location => new Vector4(input.WorldPosition, 1f),
            RenderChannel.Normal => new Vector4(surface.Normal, 1f),
            RenderChannel.RoughnessMetallicSpecular => new Vector4(surface.Roughness, surface.Metallic, surface.Specular, 1f),
            RenderChannel.Emissive => new Vector4(new Vector3(surface.Emissive), 1f),
            RenderChannel.Radiance => new Vector4(lighting.Radiance, 1f),
            RenderChannel.LightCount => new Vector4(LightGrid.Heatmap(lighting.LightCount, 32f), 1f),
            _ => new Vector4(LightMath.Lin2Rgb(lighting.Color), 1f)
        };
    }
}
