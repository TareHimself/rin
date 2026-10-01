using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Views.Shaders;

[NoReorder]
public struct ViewportPushData
{
    public Matrix4x4 Projection;
    public Matrix4x4 Transform;
    public Vector2 Size;
    public DeviceHandle OutputImage;
    public GBufferHandles GBuffer;
    public BufferRef<LightInfo> Lights;
    public int LightCount;
    public ViewportChannel Channel;
}

[Shader("Shaders/Rin/World/viewport.slang")]
public partial class ViewportShader : GBufferShader
{
    public struct PushConstants
    {
        public BufferRef<ViewportPushData> Data;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
    }

    [Push] protected PushConstants Push;

    protected override BlendState BlendState => BlendState.Alpha;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var data = Push.Data[0];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(data.Size.X, 0f), new Vector2(data.Size.X, data.Size.Y),
            new Vector2(0f, 0f), new Vector2(data.Size.X, data.Size.Y), new Vector2(0f, data.Size.Y)
        };

        var corner = corners[input.VertexId];
        var projected = Vector4.Transform(Vector4.Transform(new Vector4(corner, 0f, 1f), data.Transform), data.Projection);

        VertexOut output;
        output.Position = new Vector4(projected.X, projected.Y, 0f, 1f);
        output.Uv = corner / data.Size;
        return output;
    }

    private Vector4 SampleImage(DeviceHandle handle, Vector2 uv)
    {
        return Bindless.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear);
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public Vector4 Fragment(FragmentIn input)
    {
        var data = Push.Data[0];
        var uv = input.Uv;

        switch (data.Channel)
        {
            case ViewportChannel.Color:
                return new Vector4(SampleImage(data.GBuffer.GBuffer0, uv).xyz, 1f);

            case ViewportChannel.Location:
                return new Vector4(SampleImage(data.GBuffer.GBuffer1, uv).xyz, 1f);

            case ViewportChannel.Normal:
                return new Vector4(SampleImage(data.GBuffer.GBuffer2, uv).xyz, 1f);

            case ViewportChannel.RoughnessMetallicSpecular:
                return new Vector4(SampleImage(data.GBuffer.GBuffer0, uv).W, SampleImage(data.GBuffer.GBuffer1, uv).W,
                    SampleImage(data.GBuffer.GBuffer2, uv).W, 1f);

            case ViewportChannel.Emissive:
                return new Vector4(new Vector3(SampleImage(data.GBuffer.GBuffer3, uv).X), 1f);

            case ViewportChannel.Radiance:
            {
                var location = SampleImage(data.GBuffer.GBuffer1, uv).xyz;
                var normal = SampleImage(data.GBuffer.GBuffer2, uv).xyz;
                var radiance = new Vector3(0f);
                for (var i = 0; i < data.LightCount; i++)
                {
                    var light = data.Lights[i];
                    var toSurface = -LightMath.DirectionToLocation(light, location);
                    var noL = Math.Max(Math.Dot(normal, toSurface), 0f);
                    radiance += noL * light.Radiance * light.Color * LightMath.Attenuation(light, location);
                }

                return new Vector4(radiance, 1f);
            }

            default:
                return SampleImage(data.OutputImage, uv);
        }
    }
}
