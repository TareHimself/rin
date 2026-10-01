using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;

namespace Sponza.Shaders;

[NoReorder]
public struct SponzaMeshData
{
    public BufferRef<Vertex> Vertices;
    public Matrix4x4 Transform;
    public Vector4 Color;
    public DeviceHandle ColorTexture;
    public DeviceHandle NormalTexture;
    public DeviceHandle MetallicRoughnessTexture;
}

[Shader("Shaders/Sponza/mesh.slang")]
public partial class SponzaMeshShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<WorldInfo> Scene;
        public BufferRef<SponzaMeshData> Data;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Opaque;

    [Vertex]
    public MeshShader.VertexOut Vertex(MeshShader.VertexIn input)
    {
        var instance = Push.Data[input.InstanceId];
        var scene = Push.Scene[0];
        var vertex = instance.Vertices[input.VertexId];
        var sceneLocation = Vector4.Transform(new Vector4(vertex.Location, 1f), instance.Transform);

        MeshShader.VertexOut output;
        output.Uv = vertex.UV;
        output.SceneNormal = ShaderMath.TransformNormal(vertex.Normal, instance.Transform);
        output.SceneLocation = sceneLocation.xyz;
        var viewLocation = Vector4.Transform(new Vector4(sceneLocation.xyz, 1f), scene.View);
        output.Position = Vector4.Transform(viewLocation, scene.Projection);
        output.InstanceId = input.InstanceId;
        return output;
    }

    [Fragment, Depth]
    public GBufferOutput Fragment(MeshShader.FragmentIn input)
    {
        var instance = Push.Data[input.InstanceId];
        var normal = Math.Normalize(input.SceneNormal);

        var color = instance.Color.xyz;
        if (instance.ColorTexture.Id > 0u)
            color = Bindless.SampleTexture(instance.ColorTexture, input.Uv, ImageTiling.Repeat, ImageFilter.Linear).xyz;

        // glTF metallic-roughness convention: G is roughness, B is metallic.
        var roughness = 1f;
        var metallic = 0f;
        if (instance.MetallicRoughnessTexture.Id > 0u)
        {
            var metallicRoughness = Bindless
                .SampleTexture(instance.MetallicRoughnessTexture, input.Uv, ImageTiling.Repeat, ImageFilter.Linear).xyz;
            roughness = metallicRoughness.Y;
            metallic = metallicRoughness.Z;
        }

        GBufferOutput output;
        output.GBuffer0 = new Vector4(color, roughness);
        output.GBuffer1 = new Vector4(input.SceneLocation, metallic);
        output.GBuffer2 = new Vector4(normal, 0f);
        output.GBuffer3 = new Vector4(0f);
        return output;
    }
}
