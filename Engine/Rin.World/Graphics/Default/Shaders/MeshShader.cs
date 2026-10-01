using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Shaders;

[NoReorder]
public struct MeshMaterialData
{
    public BufferRef<Vertex> Vertices;
    public Matrix4x4 Transform;
    public Vector3 BaseColor;
    public DeviceHandle BaseColorTexture;
    public DeviceHandle NormalTexture;
    public float Metallic;
    public DeviceHandle MetallicTexture;
    public float Specular;
    public DeviceHandle SpecularTexture;
    public float Roughness;
    public DeviceHandle RoughnessTexture;
    public float Emissive;
    public DeviceHandle EmissiveTexture;
}

[Shader("Shaders/Rin/World/Mesh/mesh.slang")]
public partial class MeshShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<WorldInfo> Scene;
        public BufferRef<MeshMaterialData> Data;
    }

    public struct VertexIn
    {
        [Semantic("SV_VulkanVertexID")] public int VertexId;
        [Semantic("SV_VulkanInstanceID")] public int InstanceId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("SCENE_LOCATION")] public Vector3 SceneLocation;
        [Semantic("SCENE_NORMAL")] public Vector3 SceneNormal;
        [Position] public Vector4 Position;
        [Semantic("INSTANCE_ID")] public int InstanceId;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("SCENE_LOCATION")] public Vector3 SceneLocation;
        [Semantic("SCENE_NORMAL")] public Vector3 SceneNormal;
        [Semantic("INSTANCE_ID")] public int InstanceId;
    }

    public struct GBufferOut
    {
        [Target(1)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer0;
        [Target(2)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer1;
        [Target(3)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer2;
        [Target(4)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer3;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Opaque;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var instance = Push.Data[input.InstanceId];
        var scene = Push.Scene[0];
        var vertex = instance.Vertices[input.VertexId];
        var sceneLocation = Vector4.Transform(new Vector4(vertex.Location, 1f), instance.Transform);

        VertexOut output;
        output.Uv = vertex.UV;
        output.SceneNormal = ShaderMath.TransformNormal(vertex.Normal, instance.Transform);
        output.SceneLocation = sceneLocation.xyz;
        var viewLocation = Vector4.Transform(new Vector4(sceneLocation.xyz, 1f), scene.View);
        output.Position = Vector4.Transform(viewLocation, scene.Projection);
        output.InstanceId = input.InstanceId;
        return output;
    }

    private Vector3 ColorOrTexture(DeviceHandle handle, Vector2 uv, Vector3 fallback)
    {
        return handle.Id > 0u
            ? Bindless.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).xyz
            : fallback;
    }

    private float ValueOrTexture(DeviceHandle handle, Vector2 uv, float fallback)
    {
        return handle.Id > 0u
            ? Bindless.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).X
            : fallback;
    }

    [Fragment, Depth]
    public GBufferOut Fragment(FragmentIn input)
    {
        var instance = Push.Data[input.InstanceId];
        var normal = Math.Normalize(input.SceneNormal);
        var color = ColorOrTexture(instance.BaseColorTexture, input.Uv, instance.BaseColor);
        var roughness = ValueOrTexture(instance.RoughnessTexture, input.Uv, instance.Roughness);
        var metallic = ValueOrTexture(instance.MetallicTexture, input.Uv, instance.Metallic);
        var specular = ValueOrTexture(instance.SpecularTexture, input.Uv, instance.Specular);
        var emissive = ValueOrTexture(instance.EmissiveTexture, input.Uv, instance.Emissive);

        GBufferOut output;
        output.GBuffer0 = new Vector4(color, roughness);
        output.GBuffer1 = new Vector4(input.SceneLocation, metallic);
        output.GBuffer2 = new Vector4(normal, specular);
        output.GBuffer3 = new Vector4(emissive, 0f, 0f, 0f);
        return output;
    }
}
