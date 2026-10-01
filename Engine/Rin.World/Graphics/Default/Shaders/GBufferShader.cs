using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

public abstract class GBufferShader : Shader
{
    protected static BindlessData Bindless;

    protected GBufferSample SampleGBuffer(GBufferHandles gBuffer, Vector2 uv)
    {
        var sample0 = Bindless.SampleTexture(gBuffer.GBuffer0, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample1 = Bindless.SampleTexture(gBuffer.GBuffer1, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample2 = Bindless.SampleTexture(gBuffer.GBuffer2, uv, ImageTiling.ClampEdge, ImageFilter.Linear);
        var sample3 = Bindless.SampleTexture(gBuffer.GBuffer3, uv, ImageTiling.ClampEdge, ImageFilter.Linear);

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
