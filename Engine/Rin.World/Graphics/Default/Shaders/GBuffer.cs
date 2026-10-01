using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

public struct GBufferHandles
{
    public DeviceHandle GBuffer0;
    public DeviceHandle GBuffer1;
    public DeviceHandle GBuffer2;
    public DeviceHandle GBuffer3;
}

public struct GBufferSample
{
    public Vector3 Color;
    public Vector3 Location;
    public Vector3 Normal;
    public float Roughness;
    public float Metallic;
    public float Specular;
    public float Emissive;
}

public struct GBufferOutput
{
    [Target(1)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer0;
    [Target(2)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer1;
    [Target(3)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer2;
    [Target(4)] [Attachment(AttachmentFormat.RGBA32)] public Vector4 GBuffer3;
}

public static class GBufferExtensions
{
    public static GBufferSample SampleGBuffer(this BindlessData bindless, GBufferHandles gBuffer, Vector2 uv)
    {
        var sample0 = bindless.SampleTexture(gBuffer.GBuffer0, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample1 = bindless.SampleTexture(gBuffer.GBuffer1, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample2 = bindless.SampleTexture(gBuffer.GBuffer2, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample3 = bindless.SampleTexture(gBuffer.GBuffer3, uv, ImageTiling.ClampEdge, ImageFilter.Linear);

        GBufferSample result;
        result.Color = sample0.xyz;
        result.Location = sample1.xyz;
        result.Normal = sample2.xyz;
        result.Roughness = sample0.W;
        result.Metallic = sample1.W;
        result.Specular = sample2.W;
        result.Emissive = sample3.X;
        return result;
    }
}
